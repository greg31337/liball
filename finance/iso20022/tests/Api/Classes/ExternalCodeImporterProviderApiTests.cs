using iso20022;

namespace iso20022.Tests;

public class ExternalCodeImporterProviderApiTests
{
    private ExternalCodeCatalog jsonCatalog = null!;
    private ExternalCodeCatalog xsdCatalog = null!;
    private ExternalCodeCatalog xlsxCatalog = null!;

    [OneTimeSetUp]
    public void ImportOfficialCatalogs()
    {
        string specificationRoot = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "../../../../specs/iso20022"));
        IExternalCodeSetImporter importer = new ExternalCodeSetImporter();
        jsonCatalog = ImportOfficialCatalog(
            importer,
            Path.Combine(specificationRoot, "ExternalCodeSets_JSON_2026-05.zip"),
            "JSON");
        xsdCatalog = ImportOfficialCatalog(
            importer,
            Path.Combine(specificationRoot, "ExternalCodeSets_XSD_2026-05.zip"),
            "XSD");
        xlsxCatalog = ImportOfficialCatalog(
            importer,
            Path.Combine(specificationRoot, "ExternalCodeSets_XLSX_2026-05.zip"),
            "XLSX");
    }

    [Test]
    public void OfficialFormatsShareEditionLookupIdentityAndExactProvenance()
    {
        CodeLookupResult json = LookupSingleCatalog(
            jsonCatalog,
            "ExternalAcceptedReason1Code",
            "ADEA",
            false);
        CodeLookupResult xsd = LookupSingleCatalog(
            xsdCatalog,
            "ExternalAcceptedReason1Code",
            "ADEA",
            true);
        CodeLookupResult xlsx = LookupSingleCatalog(
            xlsxCatalog,
            "ExternalAcceptedReason1Code",
            "ADEA",
            false);
        IExternalCodeSetImporter importer = new ExternalCodeSetImporter();
        ExternalCodeCatalogComparisonResult comparison = importer.CompareCatalogs(
            new ExternalCodeCatalogComparisonRequest
            {
                JsonCatalog = jsonCatalog,
                XsdCatalog = xsdCatalog,
                XlsxCatalog = xlsxCatalog,
                MaxDisagreementCount = 1000
            });

        ICodeSetProvider combinedProvider = new CodeSetProvider(
            new List<RepositorySnapshot>(),
            new List<ExternalCodeCatalog> { jsonCatalog, xsdCatalog, xlsxCatalog });
        CodeLookupResult ambiguousEdition = combinedProvider.LookupCode(new CodeLookupRequest
        {
            Kind = CodeSetKind.External,
            CodeSetIdentifier = "ExternalAcceptedReason1Code",
            Value = "ADEA",
            SelectLatestEdition = true
        });

        Assert.Multiple(() =>
        {
            Assert.That(
                new[] { jsonCatalog.Edition, xsdCatalog.Edition, xlsxCatalog.Edition },
                Is.EqualTo(new[] { "2026-Q1", "2026-Q1", "2026-Q1" }));
            Assert.That(
                new[] { jsonCatalog.SourceFormat, xsdCatalog.SourceFormat, xlsxCatalog.SourceFormat },
                Is.EqualTo(new[] { "JSON", "XSD", "XLSX" }));
            Assert.That(json.Success, Is.True, json.ErrorMessage);
            Assert.That(json.Found, Is.True);
            Assert.That(json.ExternalCodeValue?.Value, Is.EqualTo("ADEA"));
            Assert.That(json.Edition, Is.EqualTo("2026-Q1"));
            Assert.That(json.Artifact, Is.SameAs(jsonCatalog.Artifact));
            Assert.That(xsd.Success, Is.True, xsd.ErrorMessage);
            Assert.That(xsd.Found, Is.True);
            Assert.That(xsd.ExternalCodeValue?.Value, Is.EqualTo("ADEA"));
            Assert.That(xsd.Artifact, Is.SameAs(xsdCatalog.Artifact));
            Assert.That(xlsx.Success, Is.True, xlsx.ErrorMessage);
            Assert.That(xlsx.Found, Is.True);
            Assert.That(xlsx.ExternalCodeValue?.Value, Is.EqualTo("ADEA"));
            Assert.That(xlsx.Artifact, Is.SameAs(xlsxCatalog.Artifact));
            Assert.That(comparison.Success, Is.True, comparison.ErrorMessage);
            Assert.That(comparison.Edition, Is.EqualTo("2026-Q1"));
            Assert.That(ambiguousEdition.Success, Is.False);
            Assert.That(ambiguousEdition.ErrorCode,
                Is.EqualTo("code.lookup.edition_ambiguous"));
        });
    }

    [Test]
    public void OfficialLifecycleDatesInactiveValuesAndDuplicatesFollowLookupPolicy()
    {
        byte[] workbook = ExternalCodeSetXlsxImporterTests.CreateXlsxPackage(false, false, false);
        IExternalCodeSetImporter importer = new ExternalCodeSetImporter();
        ExternalCodeImportResult imported = importer.ImportXlsxStream(
            new MemoryStream(workbook),
            CreateImportRequest("lifecycle.xlsx"));
        ExternalCodeCatalog lifecycleCatalog = imported.Catalog ??
            throw new AssertionException(imported.ErrorMessage);
        ExternalCodeSet datedSet = lifecycleCatalog.CodeSets.Single();
        ExternalCodeValue obsolete = datedSet.Values.First(value => value.RemovalDate != null);
        IsoDateValue removalDate = obsolete.RemovalDate ??
            throw new AssertionException("Expected an official removal date.");
        IsoDateValue beforeRemoval = PreviousDay(removalDate);
        ICodeSetProvider xlsxProvider = new CodeSetProvider(
            new List<RepositorySnapshot>(),
            new List<ExternalCodeCatalog> { lifecycleCatalog });

        CodeLookupResult inactiveExcluded = xlsxProvider.LookupCode(
            CreateLookup(datedSet.Identifier, obsolete.Value, false, null));
        CodeLookupResult inactiveIncluded = xlsxProvider.LookupCode(
            CreateLookup(datedSet.Identifier, obsolete.Value, true, null));
        CodeLookupResult beforeEnd = xlsxProvider.LookupCode(
            CreateLookup(datedSet.Identifier, obsolete.Value, true, beforeRemoval));
        CodeLookupResult atEnd = xlsxProvider.LookupCode(
            CreateLookup(datedSet.Identifier, obsolete.Value, true, removalDate));

        ICodeSetProvider xsdProvider = new CodeSetProvider(
            new List<RepositorySnapshot>(),
            new List<ExternalCodeCatalog> { xsdCatalog });
        CodeLookupResult activeDuplicate = xsdProvider.LookupCode(
            CreateLookup("ExternalLocalInstrument1Code", "CTP", false, null));
        CodeLookupResult duplicate = xsdProvider.LookupCode(
            CreateLookup("ExternalLocalInstrument1Code", "CTP", true, null));

        Assert.Multiple(() =>
        {
            Assert.That(imported.Success, Is.True, imported.ErrorMessage);
            Assert.That(obsolete.RegistrationStatus, Does.StartWith("Obsolete"));
            Assert.That(inactiveExcluded.Success, Is.True, inactiveExcluded.ErrorMessage);
            Assert.That(inactiveExcluded.Found, Is.False);
            Assert.That(inactiveIncluded.Success, Is.True, inactiveIncluded.ErrorMessage);
            Assert.That(inactiveIncluded.Found, Is.True);
            Assert.That(inactiveIncluded.ExternalCodeValue, Is.SameAs(obsolete));
            Assert.That(beforeEnd.Success, Is.True, beforeEnd.ErrorMessage);
            Assert.That(beforeEnd.Found, Is.True);
            Assert.That(atEnd.Success, Is.True, atEnd.ErrorMessage);
            Assert.That(atEnd.Found, Is.False);
            Assert.That(activeDuplicate.Success, Is.True, activeDuplicate.ErrorMessage);
            Assert.That(activeDuplicate.Found, Is.True);
            Assert.That(activeDuplicate.ExternalCodeValue?.RegistrationStatus,
                Is.EqualTo("Registered"));
            Assert.That(duplicate.Success, Is.False);
            Assert.That(duplicate.ErrorCode, Is.EqualTo("code.lookup.value_ambiguous"));
        });
    }

    [Test]
    public void MalformedInputsRemainFormatSpecificAndNeverProduceCatalogs()
    {
        IExternalCodeSetImporter importer = new ExternalCodeSetImporter();
        ExternalCodeImportRequest jsonRequest = CreateImportRequest("malformed.json");
        ExternalCodeImportRequest xsdRequest = CreateImportRequest("malformed.xsd");
        ExternalCodeImportRequest xlsxRequest = CreateImportRequest("malformed.xlsx");
        const string wrongNamespace = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
              targetNamespace="urn:example:wrong"/>
            """;
        byte[] emptyWorkbook = CreateEmptyZip();

        ExternalCodeImportResult xsd = importer.ImportXsdStream(
            new MemoryStream(System.Text.Encoding.UTF8.GetBytes(wrongNamespace)),
            xsdRequest);
        ExternalCodeImportResult xlsx = importer.ImportXlsxStream(
            new MemoryStream(emptyWorkbook),
            xlsxRequest);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => importer.ImportJsonStream(
                    new MemoryStream("{"u8.ToArray()),
                    jsonRequest),
                Throws.InstanceOf<System.Text.Json.JsonException>());
            Assert.That(xsd.Success, Is.False);
            Assert.That(xsd.ErrorCode, Is.EqualTo("external-code.xsd.namespace"));
            Assert.That(xsd.Catalog, Is.Null);
            Assert.That(xlsx.Success, Is.False);
            Assert.That(xlsx.ErrorCode, Is.EqualTo("external-code.archive.entry_not_found"));
            Assert.That(xlsx.Catalog, Is.Null);
        });
    }

    private static ExternalCodeCatalog ImportOfficialCatalog(
        IExternalCodeSetImporter importer,
        string path,
        string format)
    {
        ExternalCodeImportRequest request = CreateImportRequest(path);
        ExternalCodeImportResult result;
        using (MemoryStream input = new MemoryStream(File.ReadAllBytes(path)))
        {
            if (format == "JSON")
            {
                result = importer.ImportJsonStream(input, request);
            }
            else if (format == "XSD")
            {
                result = importer.ImportXsdStream(input, request);
            }
            else
            {
                result = importer.ImportXlsxStream(input, request);
            }
        }

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.That(result.SourceFormat, Is.EqualTo(format));
        return result.Catalog ?? throw new AssertionException(result.ErrorMessage);
    }

    private static ExternalCodeImportRequest CreateImportRequest(string sourceName)
    {
        return new ExternalCodeImportRequest
        {
            SourceName = sourceName,
            Edition = "2026-Q1",
            Artifact = new ArtifactManifest
            {
                Coordinate = new ArtifactCoordinate
                {
                    Kind = ArtifactKind.ExternalCodeSet,
                    Edition = "2026-Q1",
                    Identifier = "external-code-sets",
                    Version = "1"
                }
            }
        };
    }

    private static CodeLookupResult LookupSingleCatalog(
        ExternalCodeCatalog catalog,
        string codeSet,
        string value,
        bool latest)
    {
        ICodeSetProvider provider = new CodeSetProvider(
            new List<RepositorySnapshot>(),
            new List<ExternalCodeCatalog> { catalog });
        CodeLookupRequest request = new CodeLookupRequest
        {
            Kind = CodeSetKind.External,
            CodeSetIdentifier = codeSet,
            Value = value,
            SelectLatestEdition = latest,
            Edition = latest ? string.Empty : "2026-Q1"
        };
        return provider.LookupCode(request);
    }

    private static CodeLookupRequest CreateLookup(
        string codeSet,
        string value,
        bool includeInactive,
        IsoDateValue? activeOn)
    {
        return new CodeLookupRequest
        {
            Kind = CodeSetKind.External,
            CodeSetIdentifier = codeSet,
            Value = value,
            Edition = "2026-Q1",
            IncludeInactive = includeInactive,
            ActiveOn = activeOn
        };
    }

    private static IsoDateValue PreviousDay(IsoDateValue value)
    {
        DateTime date = new DateTime(value.Year, value.Month, value.Day).AddDays(-1);
        return new IsoDateValue
        {
            Year = date.Year,
            Month = date.Month,
            Day = date.Day,
            LexicalValue = date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
        };
    }

    private static byte[] CreateEmptyZip()
    {
        using MemoryStream output = new MemoryStream();
        using (System.IO.Compression.ZipArchive archive = new System.IO.Compression.ZipArchive(
            output,
            System.IO.Compression.ZipArchiveMode.Create,
            true))
        {
        }

        return output.ToArray();
    }
}
