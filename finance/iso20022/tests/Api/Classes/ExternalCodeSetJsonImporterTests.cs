using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using iso20022;

namespace iso20022.Tests;

public class ExternalCodeSetJsonImporterTests
{
    [Test]
    public void RawJsonNormalizesSchemaFacetsDefinitionsEditionAndAbsentLifecycle()
    {
        byte[] json = Encoding.UTF8.GetBytes(CreateJsonFixture());
        ExternalCodeImportRequest request = CreateRequest("2026-Q1", "fixture.json");
        request.Artifact.PublicationDate = new IsoPartialDateValue
        {
            Kind = PartialDateKind.YearMonth,
            Year = 2026,
            Month = 5,
            LexicalValue = "2026-05"
        };
        IExternalCodeSetImporter importer = new ExternalCodeSetImporter();

        ExternalCodeImportResult result = importer.ImportJsonStream(
            new MemoryStream(json),
            request);
        ExternalCodeCatalog catalog = result.Catalog ?? throw new AssertionException(result.ErrorMessage);
        ExternalCodeSet statusSet = catalog.CodeSets.Single(
            set => set.Identifier == "ExternalStatus1Code");
        ExternalCodeSet patternSet = catalog.CodeSets.Single(
            set => set.Identifier == "ExternalHex1Code");
        ExternalCodeValue active = statusSet.Values.Single(value => value.Value == "ACTV");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.SourceFormat, Is.EqualTo("JSON"));
            Assert.That(result.SourceEntryName, Is.EqualTo("fixture.json"));
            Assert.That(result.Artifact.HashAlgorithm, Is.EqualTo("SHA-256"));
            Assert.That(result.Artifact.Hash.Value, Is.EqualTo(SHA256.HashData(json)));
            Assert.That(result.Artifact.MediaType, Is.EqualTo(Iso20022MediaTypes.Json));
            Assert.That(catalog.Edition, Is.EqualTo("2026-Q1"));
            Assert.That(catalog.PublicationDate?.LexicalValue, Is.EqualTo("2026-05"));
            Assert.That(catalog.SchemaIdentifier, Is.EqualTo("urn:iso:std:iso:20022:tech:json:"));
            Assert.That(catalog.SchemaVersion, Is.EqualTo("draft-04"));
            Assert.That(catalog.SourceFormat, Is.EqualTo("JSON"));
            Assert.That(catalog.CodeSets, Has.Count.EqualTo(3));
            Assert.That(statusSet.Name, Is.EqualTo("ExternalStatus1Code"));
            Assert.That(statusSet.Definition, Does.StartWith("Specifies whether a code is active."));
            Assert.That(statusSet.HasMinimumLength, Is.True);
            Assert.That(statusSet.MinimumLength, Is.EqualTo(4));
            Assert.That(statusSet.HasMaximumLength, Is.True);
            Assert.That(statusSet.MaximumLength, Is.EqualTo(4));
            Assert.That(statusSet.Values.Select(value => value.Value),
                Is.EqualTo(new[] { "ACTV", "INAC" }));
            Assert.That(active.Definition, Is.EqualTo("The code is active."));
            Assert.That(active.Name, Is.Empty);
            Assert.That(active.RegistrationStatus, Is.Empty);
            Assert.That(active.ActivationDate, Is.Null);
            Assert.That(active.DeactivationDate, Is.Null);
            Assert.That(patternSet.Pattern, Is.EqualTo("^[0-9A-F]{2}$"));
            Assert.That(result.DiagnosticCodes,
                Does.Contain("external-code.json.lifecycle_unavailable"));
            Assert.That(result.DiagnosticCodes,
                Does.Contain("external-code.json.values_unavailable"));
        });
    }

    [Test]
    public void ZipJsonSelectsSafeEntryInfersQuarterAndRetainsOuterArtifactHash()
    {
        byte[] json = Encoding.UTF8.GetBytes(CreateJsonFixture());
        byte[] archive = CreateZip(new[]
        {
            ("1Q2026_externalcodesets_v1.json", json),
            ("readme.txt", Encoding.UTF8.GetBytes("fixture"))
        });
        ExternalCodeImportRequest request = CreateRequest(string.Empty, "external-codes.zip");
        request.Artifact.Coordinate.Edition = string.Empty;
        request.Artifact.HashAlgorithm = "SHA-256";
        request.Artifact.Hash.Value = SHA256.HashData(archive);
        IExternalCodeSetImporter importer = new ExternalCodeSetImporter();

        ExternalCodeImportResult result = importer.ImportJsonStream(
            new MemoryStream(archive),
            request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Catalog?.Edition, Is.EqualTo("2026-Q1"));
            Assert.That(result.SourceEntryName, Is.EqualTo("1Q2026_externalcodesets_v1.json"));
            Assert.That(result.Artifact.MediaType, Is.EqualTo(Iso20022MediaTypes.Zip));
            Assert.That(result.Artifact.Hash.Value, Is.EqualTo(SHA256.HashData(archive)));
            Assert.That(result.Catalog?.Artifact, Is.SameAs(result.Artifact));
        });
    }

    [Test]
    public void JsonImportReportsAmbiguousArchivesInvalidSchemaAndDuplicateCodes()
    {
        byte[] json = Encoding.UTF8.GetBytes(CreateJsonFixture());
        byte[] ambiguousArchive = CreateZip(new[]
        {
            ("first.json", json),
            ("second.json", json)
        });
        const string invalidSchema = """
            {"$schema":"http://json-schema.org/draft-04/schema#","type":"array","definitions":{}}
            """;
        const string duplicateCode = """
            {
              "$schema":"http://json-schema.org/draft-04/schema#",
              "type":"object",
              "definitions":{
                "ExternalDuplicate1Code":{
                  "type":"string",
                  "description":"Duplicate values.\n*`DUPL`-Duplicated.",
                  "enum":["DUPL","DUPL"]
                }
              }
            }
            """;
        IExternalCodeSetImporter importer = new ExternalCodeSetImporter();

        ExternalCodeImportResult ambiguous = importer.ImportJsonStream(
            new MemoryStream(ambiguousArchive),
            CreateRequest("2026-Q1", "ambiguous.zip"));
        ExternalCodeImportResult wrongRoot = importer.ImportJsonStream(
            new MemoryStream(Encoding.UTF8.GetBytes(invalidSchema)),
            CreateRequest("2026-Q1", "invalid.json"));
        ExternalCodeImportResult duplicate = importer.ImportJsonStream(
            new MemoryStream(Encoding.UTF8.GetBytes(duplicateCode)),
            CreateRequest("2026-Q1", "duplicate.json"));

        Assert.Multiple(() =>
        {
            Assert.That(ambiguous.ErrorCode, Is.EqualTo("external-code.archive.entry_ambiguous"));
            Assert.That(wrongRoot.ErrorCode, Is.EqualTo("external-code.json.root_type"));
            Assert.That(duplicate.ErrorCode, Is.EqualTo("external-code.json.code"));
            Assert.That(ambiguous.Catalog, Is.Null);
            Assert.That(wrongRoot.Catalog, Is.Null);
            Assert.That(duplicate.Catalog, Is.Null);
        });
    }

    [Test]
    public void JsonImportHonorsSizeDefinitionDiagnosticAndCancellationLimits()
    {
        byte[] json = Encoding.UTF8.GetBytes(CreateJsonFixture());
        IExternalCodeSetImporter importer = new ExternalCodeSetImporter();
        ExternalCodeImportRequest sizeRequest = CreateRequest("2026-Q1", "size.json");
        sizeRequest.MaxArtifactBytes = json.Length - 1;
        ExternalCodeImportRequest definitionRequest = CreateRequest("2026-Q1", "definitions.json");
        definitionRequest.Limits.MaxSchemaCount = 2;
        ExternalCodeImportRequest diagnosticRequest = CreateRequest("2026-Q1", "diagnostics.json");
        diagnosticRequest.Limits.MaxDiagnosticCount = 1;
        ExternalCodeImportRequest cancellationRequest = CreateRequest("2026-Q1", "cancelled.json");
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        cancellationRequest.CancellationToken = cancellation.Token;

        ExternalCodeImportResult size = importer.ImportJsonStream(
            new MemoryStream(json),
            sizeRequest);
        ExternalCodeImportResult definitions = importer.ImportJsonStream(
            new MemoryStream(json),
            definitionRequest);
        ExternalCodeImportResult diagnostics = importer.ImportJsonStream(
            new MemoryStream(json),
            diagnosticRequest);

        Assert.Multiple(() =>
        {
            Assert.That(size.ErrorCode, Is.EqualTo("external-code.artifact.size"));
            Assert.That(definitions.ErrorCode, Is.EqualTo("external-code.json.definition_limit"));
            Assert.That(diagnostics.Success, Is.True);
            Assert.That(diagnostics.DiagnosticCodes, Has.Count.EqualTo(1));
            Assert.That(
                () => importer.ImportJsonStream(new MemoryStream(json), cancellationRequest),
                Throws.InstanceOf<OperationCanceledException>());
        });
    }

    [Test]
    public void MalformedJsonUsesThePlatformJsonException()
    {
        IExternalCodeSetImporter importer = new ExternalCodeSetImporter();
        byte[] malformed = Encoding.UTF8.GetBytes(
            "{\"$schema\":\"http://json-schema.org/draft-04/schema#\",\"definitions\":");

        Assert.That(
            () => importer.ImportJsonStream(
                new MemoryStream(malformed),
                CreateRequest("2026-Q1", "malformed.json")),
            Throws.InstanceOf<JsonException>());
    }

    [Test]
    public void BundledOfficialJsonImportsAllPublishedDefinitionsAndValues()
    {
        string archivePath = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "../../../../specs/iso20022/ExternalCodeSets_JSON_2026-05.zip"));
        byte[] archive = File.ReadAllBytes(archivePath);
        ExternalCodeImportRequest request = CreateRequest("2026-Q1", archivePath);
        request.Artifact.HashAlgorithm = "SHA-256";
        request.Artifact.Hash.Value = Convert.FromHexString(
            "96b283eecf00edf93dc9cb95926200ec613de46fd2375babb09a9ff1c53707f2");
        IExternalCodeSetImporter importer = new ExternalCodeSetImporter();

        ExternalCodeImportResult result = importer.ImportJsonStream(
            new MemoryStream(archive),
            request);
        ExternalCodeCatalog catalog = result.Catalog ?? throw new AssertionException(result.ErrorMessage);
        ExternalCodeSet accepted = catalog.CodeSets.Single(
            set => set.Identifier == "ExternalAcceptedReason1Code");
        ExternalCodeSet encrypted = catalog.CodeSets.Single(
            set => set.Identifier == "ExternalEncryptedElementIdentification1Code");
        ExternalCodeSet scenario = catalog.CodeSets.Single(
            set => set.Identifier == "ExternalPaymentScenario1Code");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.SourceEntryName, Is.EqualTo("1Q2026_externalcodesets_v1.json"));
            Assert.That(result.Artifact.Hash.LexicalValue, Is.EqualTo(
                "96B283EECF00EDF93DC9CB95926200EC613DE46FD2375BABB09A9FF1C53707F2"));
            Assert.That(catalog.CodeSets, Has.Count.EqualTo(158));
            Assert.That(catalog.CodeSets.Sum(set => set.Values.Count), Is.EqualTo(3213));
            Assert.That(accepted.Values.Select(value => value.Value),
                Is.EqualTo(new[] { "ADEA", "NSTP", "SMPG" }));
            Assert.That(accepted.Values[0].Definition,
                Is.EqualTo("Received after the servicer's deadline. Processed on best effort basis"));
            Assert.That(encrypted.Pattern, Is.EqualTo("^([0-9A-F][0-9A-F]){1,3}$"));
            Assert.That(encrypted.Values, Has.Count.EqualTo(62));
            Assert.That(scenario.Values, Is.Empty);
            Assert.That(result.DiagnosticCodes.Count(code =>
                code == "external-code.json.values_unavailable"), Is.EqualTo(24));
            Assert.That(result.DiagnosticCodes,
                Does.Contain("external-code.json.lifecycle_unavailable"));
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
                    Version = "1"
                },
                OriginUri = "https://example.test/external-code-sets",
                IsApproved = true
            }
        };
    }

    private static string CreateJsonFixture()
    {
        return """
            {
              "$schema":"http://json-schema.org/draft-04/schema#",
              "type":"object",
              "additionalProperties":false,
              "properties":{
                "$id":{"default":"urn:iso:std:iso:20022:tech:json:"}
              },
              "definitions":{
                "ExternalStatus1Code":{
                  "type":"string",
                  "minLength":4,
                  "maxLength":4,
                  "description":"Specifies whether a code is active.\r\nExternal code sets are published separately.\n*`ACTV`-The code is active.\n*`INAC`-The code is inactive.",
                  "enum":["ACTV","INAC"]
                },
                "ExternalHex1Code":{
                  "type":"string",
                  "pattern":"^[0-9A-F]{2}$",
                  "description":"Specifies a hexadecimal code.\n*`AF`-A hexadecimal value.",
                  "enum":["AF"]
                },
                "ExternalUnavailable1Code":{
                  "type":"string",
                  "description":"Values are maintained separately."
                }
              }
            }
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
                stream.Write(content, 0, content.Length);
            }
        }

        return output.ToArray();
    }
}
