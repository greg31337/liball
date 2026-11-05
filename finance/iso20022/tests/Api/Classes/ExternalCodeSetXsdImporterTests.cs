using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using iso20022;

namespace iso20022.Tests;

public class ExternalCodeSetXsdImporterTests
{
    [Test]
    public void RawXsdNormalizesFacetsDocumentationAndPublishedLifecycleMetadata()
    {
        byte[] xsd = Encoding.UTF8.GetBytes(CreateXsdFixture());
        ExternalCodeImportRequest request = CreateRequest("2026-Q1", "fixture.xsd");
        request.Artifact.PublicationDate = new IsoPartialDateValue
        {
            Kind = PartialDateKind.YearMonth,
            Year = 2026,
            Month = 5,
            LexicalValue = "2026-05"
        };
        IExternalCodeSetImporter importer = new ExternalCodeSetImporter();

        ExternalCodeImportResult result = importer.ImportXsdStream(
            new MemoryStream(xsd),
            request);
        ExternalCodeCatalog catalog = result.Catalog ?? throw new AssertionException(result.ErrorMessage);
        ExternalCodeSet codeSet = catalog.CodeSets.Single();
        ExternalCodeValue first = codeSet.Values[0];
        ExternalCodeValue second = codeSet.Values[1];

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.SourceFormat, Is.EqualTo("XSD"));
            Assert.That(result.SourceEntryName, Is.EqualTo("fixture.xsd"));
            Assert.That(result.Artifact.MediaType, Is.EqualTo(Iso20022MediaTypes.XmlSchema));
            Assert.That(result.Artifact.Hash.Value, Is.EqualTo(SHA256.HashData(xsd)));
            Assert.That(catalog.Edition, Is.EqualTo("2026-Q1"));
            Assert.That(catalog.PublicationDate?.LexicalValue, Is.EqualTo("2026-05"));
            Assert.That(catalog.SchemaIdentifier,
                Is.EqualTo("urn:iso:std:iso:20022:tech:xsd:externalcodeset"));
            Assert.That(catalog.SchemaVersion, Is.EqualTo("fixture-v1"));
            Assert.That(catalog.SourceFormat, Is.EqualTo("XSD"));
            Assert.That(codeSet.Identifier, Is.EqualTo("ExternalStatus1Code"));
            Assert.That(codeSet.Name, Is.EqualTo("External Status"));
            Assert.That(codeSet.Definition, Is.EqualTo("Specifies whether a code is active."));
            Assert.That(codeSet.RegistrationStatus, Is.EqualTo("Registered"));
            Assert.That(codeSet.MinimumLength, Is.EqualTo(4));
            Assert.That(codeSet.MaximumLength, Is.EqualTo(4));
            Assert.That(codeSet.Pattern, Is.EqualTo("[A-Z]{4}"));
            Assert.That(codeSet.Values.Select(value => value.Value),
                Is.EqualTo(new[] { "ACTV", "INAC" }));
            Assert.That(first.Name, Is.EqualTo("Active"));
            Assert.That(first.Definition, Is.EqualTo("The code is active."));
            Assert.That(first.Requester, Is.EqualTo("ISO test requester"));
            Assert.That(first.RegistrationStatus, Is.EqualTo("Registered"));
            Assert.That(first.LastUpdatedDate?.LexicalValue, Is.EqualTo("2026-05-11"));
            Assert.That(first.CreationDate?.LexicalValue, Is.EqualTo("2026-05-01"));
            Assert.That(first.ActivationDate, Is.Null);
            Assert.That(first.DeactivationDate, Is.Null);
            Assert.That(second.ReplacedBy, Is.EqualTo("ACTV"));
            Assert.That(second.AdditionalInformation, Is.EqualTo("Do not use."));
            Assert.That(result.DiagnosticCodes,
                Does.Contain("external-code.xsd.validity_unavailable"));
        });
    }

    [Test]
    public void ZipXsdSelectsSafeEntryInfersQuarterAndRetainsOuterArtifactHash()
    {
        byte[] xsd = Encoding.UTF8.GetBytes(CreateXsdFixture());
        byte[] archive = CreateZip(new[]
        {
            ("1Q2026_externalcodesets_v1.xsd", xsd),
            ("readme.txt", Encoding.UTF8.GetBytes("fixture"))
        });
        ExternalCodeImportRequest request = CreateRequest(string.Empty, "external-codes.zip");
        request.Artifact.Coordinate.Edition = string.Empty;
        request.Artifact.HashAlgorithm = "SHA-256";
        request.Artifact.Hash.Value = SHA256.HashData(archive);
        IExternalCodeSetImporter importer = new ExternalCodeSetImporter();

        ExternalCodeImportResult result = importer.ImportXsdStream(
            new MemoryStream(archive),
            request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Catalog?.Edition, Is.EqualTo("2026-Q1"));
            Assert.That(result.SourceEntryName, Is.EqualTo("1Q2026_externalcodesets_v1.xsd"));
            Assert.That(result.Artifact.MediaType, Is.EqualTo(Iso20022MediaTypes.Zip));
            Assert.That(result.Artifact.Hash.Value, Is.EqualTo(SHA256.HashData(archive)));
            Assert.That(result.Catalog?.Artifact, Is.SameAs(result.Artifact));
        });
    }

    [Test]
    public void XsdImportReportsAmbiguousArchivesInvalidNamespacesAndInvalidFacets()
    {
        byte[] xsd = Encoding.UTF8.GetBytes(CreateXsdFixture());
        byte[] ambiguousArchive = CreateZip(new[]
        {
            ("first.xsd", xsd),
            ("second.xsd", xsd)
        });
        byte[] invalidNamespace = Encoding.UTF8.GetBytes(CreateXsdFixture().Replace(
            "urn:iso:std:iso:20022:tech:xsd:externalcodeset",
            "urn:example:wrong",
            StringComparison.Ordinal));
        byte[] invalidFacet = Encoding.UTF8.GetBytes(CreateXsdFixture().Replace(
            "<xs:maxLength value=\"4\"/>",
            "<xs:maxLength value=\"-1\"/>",
            StringComparison.Ordinal));
        IExternalCodeSetImporter importer = new ExternalCodeSetImporter();

        ExternalCodeImportResult ambiguous = importer.ImportXsdStream(
            new MemoryStream(ambiguousArchive),
            CreateRequest("2026-Q1", "ambiguous.zip"));
        ExternalCodeImportResult wrongNamespace = importer.ImportXsdStream(
            new MemoryStream(invalidNamespace),
            CreateRequest("2026-Q1", "invalid.xsd"));
        ExternalCodeImportResult facet = importer.ImportXsdStream(
            new MemoryStream(invalidFacet),
            CreateRequest("2026-Q1", "invalid-facet.xsd"));

        Assert.Multiple(() =>
        {
            Assert.That(ambiguous.ErrorCode, Is.EqualTo("external-code.archive.entry_ambiguous"));
            Assert.That(wrongNamespace.ErrorCode, Is.EqualTo("external-code.xsd.namespace"));
            Assert.That(facet.ErrorCode, Is.EqualTo("external-code.xsd.maximum_length"));
            Assert.That(ambiguous.Catalog, Is.Null);
            Assert.That(wrongNamespace.Catalog, Is.Null);
            Assert.That(facet.Catalog, Is.Null);
        });
    }

    [Test]
    public void XsdImportHonorsBoundsCancellationAndSecureXmlDefaults()
    {
        byte[] xsd = Encoding.UTF8.GetBytes(CreateXsdFixture());
        IExternalCodeSetImporter importer = new ExternalCodeSetImporter();
        ExternalCodeImportRequest sizeRequest = CreateRequest("2026-Q1", "size.xsd");
        sizeRequest.MaxArtifactBytes = xsd.Length - 1;
        ExternalCodeImportRequest setLimitRequest = CreateRequest("2026-Q1", "limit.xsd");
        setLimitRequest.Limits.MaxSchemaCount = 1;
        byte[] twoSets = Encoding.UTF8.GetBytes(CreateXsdFixture().Replace(
            "</xs:schema>",
            CreateSecondSimpleType() + "</xs:schema>",
            StringComparison.Ordinal));
        ExternalCodeImportRequest cancellationRequest = CreateRequest("2026-Q1", "cancelled.xsd");
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        cancellationRequest.CancellationToken = cancellation.Token;
        const string dtd = """
            <!DOCTYPE xs:schema [<!ENTITY external SYSTEM "file:///etc/passwd">]>
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
              xmlns="urn:iso:std:iso:20022:tech:xsd:externalcodeset"
              targetNamespace="urn:iso:std:iso:20022:tech:xsd:externalcodeset"/>
            """;

        ExternalCodeImportResult size = importer.ImportXsdStream(
            new MemoryStream(xsd),
            sizeRequest);
        ExternalCodeImportResult setLimit = importer.ImportXsdStream(
            new MemoryStream(twoSets),
            setLimitRequest);

        Assert.Multiple(() =>
        {
            Assert.That(size.ErrorCode, Is.EqualTo("external-code.artifact.size"));
            Assert.That(setLimit.ErrorCode, Is.EqualTo("external-code.xsd.schema_limit"));
            Assert.That(
                () => importer.ImportXsdStream(new MemoryStream(xsd), cancellationRequest),
                Throws.InstanceOf<OperationCanceledException>());
            Assert.That(
                () => importer.ImportXsdStream(
                    new MemoryStream(Encoding.UTF8.GetBytes(dtd)),
                    CreateRequest("2026-Q1", "dtd.xsd")),
                Throws.InstanceOf<XmlException>());
        });
    }

    [Test]
    public void BundledOfficialXsdImportsAllPublishedDefinitionsValuesAndMetadata()
    {
        string archivePath = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "../../../../specs/iso20022/ExternalCodeSets_XSD_2026-05.zip"));
        byte[] archive = File.ReadAllBytes(archivePath);
        ExternalCodeImportRequest request = CreateRequest("2026-Q1", archivePath);
        request.Artifact.HashAlgorithm = "SHA-256";
        request.Artifact.Hash.Value = Convert.FromHexString(
            "d4f874c459f0d8796b844d8d3c55802b03eb258186d83ee539ec9ba1b6ded6b0");
        IExternalCodeSetImporter importer = new ExternalCodeSetImporter();

        ExternalCodeImportResult result = importer.ImportXsdStream(
            new MemoryStream(archive),
            request);
        ExternalCodeCatalog catalog = result.Catalog ?? throw new AssertionException(result.ErrorMessage);
        ExternalCodeSet accepted = catalog.CodeSets.Single(
            set => set.Identifier == "ExternalAcceptedReason1Code");
        ExternalCodeSet encrypted = catalog.CodeSets.Single(
            set => set.Identifier == "ExternalEncryptedElementIdentification1Code");
        ExternalCodeSet cashClearing = catalog.CodeSets.Single(
            set => set.Identifier == "ExternalCashClearingSystem1Code");
        ExternalCodeValue canadianLvts = cashClearing.Values.Single(value => value.Value == "CAD");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.SourceEntryName, Is.EqualTo("1Q2026_externalcodesets_v1.xsd"));
            Assert.That(result.Artifact.Hash.LexicalValue, Is.EqualTo(
                "D4F874C459F0D8796B844D8D3C55802B03EB258186D83EE539EC9BA1B6DED6B0"));
            Assert.That(catalog.CodeSets, Has.Count.EqualTo(158));
            Assert.That(catalog.CodeSets.Sum(set => set.Values.Count), Is.EqualTo(3246));
            Assert.That(accepted.RegistrationStatus, Is.EqualTo("Registered"));
            Assert.That(accepted.Values.Select(value => value.Value),
                Is.EqualTo(new[] { "ADEA", "NSTP", "SMPG" }));
            Assert.That(accepted.Values[0].Name, Is.EqualTo("AccountServicerDeadlineMissed"));
            Assert.That(accepted.Values[0].CreationDate?.LexicalValue, Is.EqualTo("2016-05-01"));
            Assert.That(accepted.Values[0].LastUpdatedDate?.LexicalValue, Is.EqualTo("2016-05-11"));
            Assert.That(encrypted.Pattern, Is.EqualTo("([0-9A-F][0-9A-F]){1,3}"));
            Assert.That(encrypted.Values, Has.Count.EqualTo(62));
            Assert.That(canadianLvts.ReplacedBy, Is.EqualTo("LVT"));
            Assert.That(canadianLvts.AdditionalInformation, Is.EqualTo("Do not use."));
            Assert.That(result.DiagnosticCodes,
                Does.Contain("external-code.xsd.validity_unavailable"));
        });
    }

    private static ExternalCodeImportRequest CreateRequest(string edition, string sourceName)
    {
        return new ExternalCodeImportRequest
        {
            SourceName = sourceName,
            Edition = edition,
            Artifact = new ArtifactManifest
            {
                Coordinate = new ArtifactCoordinate
                {
                    Kind = ArtifactKind.ExternalCodeSet,
                    Edition = edition,
                    Identifier = "external-code-sets",
                    Version = "coordinate-v1"
                },
                OriginUri = "https://example.test/external-code-sets",
                IsApproved = true
            }
        };
    }

    private static string CreateXsdFixture()
    {
        return """
            <?xml version="1.0" encoding="UTF-8"?>
            <xs:schema xmlns="urn:iso:std:iso:20022:tech:xsd:externalcodeset"
              xmlns:xs="http://www.w3.org/2001/XMLSchema"
              targetNamespace="urn:iso:std:iso:20022:tech:xsd:externalcodeset"
              elementFormDefault="qualified" version="fixture-v1">
              <xs:simpleType name="ExternalStatus1Code">
                <xs:annotation>
                  <xs:appinfo><RegistrationStatus>Registered</RegistrationStatus></xs:appinfo>
                  <xs:documentation source="Name" xml:lang="EN">External Status</xs:documentation>
                  <xs:documentation source="Definition" xml:lang="EN">Specifies whether a code is active.</xs:documentation>
                </xs:annotation>
                <xs:restriction base="xs:string">
                  <xs:minLength value="4"/>
                  <xs:maxLength value="4"/>
                  <xs:pattern value="[A-Z]{4}"/>
                  <xs:enumeration value="ACTV">
                    <xs:annotation>
                      <xs:appinfo><ISO20022>
                        <RegistrationStatus>Registered</RegistrationStatus>
                        <Requestor>ISO test requester</Requestor>
                        <LastUpdatedDate>2026-05-11</LastUpdatedDate>
                        <CreationDate>2026-05-01</CreationDate>
                      </ISO20022></xs:appinfo>
                      <xs:documentation source="Name" xml:lang="EN">Active</xs:documentation>
                      <xs:documentation source="Definition" xml:lang="EN">The code is active.</xs:documentation>
                    </xs:annotation>
                  </xs:enumeration>
                  <xs:enumeration value="INAC">
                    <xs:annotation>
                      <xs:appinfo><ISO20022>
                        <RegistrationStatus>Obsolete</RegistrationStatus>
                        <ReplacedBy>ACTV</ReplacedBy>
                        <AdditionalInformation>Do not use.</AdditionalInformation>
                      </ISO20022></xs:appinfo>
                      <xs:documentation source="Name" xml:lang="EN">Inactive</xs:documentation>
                      <xs:documentation source="Definition" xml:lang="EN">The code is inactive.</xs:documentation>
                    </xs:annotation>
                  </xs:enumeration>
                </xs:restriction>
              </xs:simpleType>
            </xs:schema>
            """;
    }

    private static string CreateSecondSimpleType()
    {
        return """
              <xs:simpleType name="ExternalSecond1Code">
                <xs:restriction base="xs:string">
                  <xs:enumeration value="ONE"/>
                </xs:restriction>
              </xs:simpleType>
            """;
    }

    private static byte[] CreateZip((string Name, byte[] Content)[] entries)
    {
        using MemoryStream output = new MemoryStream();
        using (ZipArchive archive = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            foreach ((string name, byte[] content) in entries)
            {
                ZipArchiveEntry entry = archive.CreateEntry(name, CompressionLevel.NoCompression);
                using Stream stream = entry.Open();
                stream.Write(content);
            }
        }

        return output.ToArray();
    }
}
