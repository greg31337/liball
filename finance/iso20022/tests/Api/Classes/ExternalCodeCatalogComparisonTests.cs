using iso20022;

namespace iso20022.Tests;

public class ExternalCodeCatalogComparisonTests
{
    [Test]
    public void EquivalentComparableContentRetainsCoverageDiagnosticsWithoutChoosingAFormat()
    {
        ExternalCodeCatalogComparisonRequest request = CreateEquivalentRequest();
        IExternalCodeSetImporter importer = new ExternalCodeSetImporter();

        ExternalCodeCatalogComparisonResult result = importer.CompareCatalogs(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Equivalent, Is.True);
            Assert.That(result.Edition, Is.EqualTo("2026-Q1"));
            Assert.That(result.ComparedCodeSetCount, Is.EqualTo(1));
            Assert.That(result.ComparedCodeValueCount, Is.EqualTo(1));
            Assert.That(result.Disagreements, Is.Empty);
            Assert.That(result.DiagnosticCodes, Is.EqualTo(new[]
            {
                "external-code.compare.coverage.json_lifecycle",
                "external-code.compare.coverage.xlsx_facets",
                "external-code.compare.coverage.mappings"
            }));
        });
    }

    [Test]
    public void ComparisonReportsPresenceMultiplicityFacetAndFieldDisagreementsPerFormat()
    {
        ExternalCodeCatalogComparisonRequest request = CreateEquivalentRequest();
        ExternalCodeSet jsonSet = request.JsonCatalog!.CodeSets[0];
        ExternalCodeSet xsdSet = request.XsdCatalog!.CodeSets[0];
        ExternalCodeSet xlsxSet = request.XlsxCatalog!.CodeSets[0];
        xsdSet.Definition = "Different definition";
        xsdSet.MinimumLength = 2;
        xlsxSet.RegistrationStatus = "Obsolete";
        xlsxSet.Values[0].Requester = "Different requester";
        xsdSet.Values.Add(new ExternalCodeValue
        {
            Value = "ACTV",
            Name = "HistoricalActive",
            Definition = "Historical definition",
            Requester = "ISO requester",
            RegistrationStatus = "Obsolete",
            LastUpdatedDate = CreateDate("2020-01-01"),
            CreationDate = CreateDate("2010-01-01")
        });
        ExternalCodeSet jsonOnly = new ExternalCodeSet
        {
            Identifier = "ExternalJsonOnly1Code",
            Name = "ExternalJsonOnly1Code",
            Definition = "JSON only."
        };
        jsonOnly.Values.Add(new ExternalCodeValue { Value = "ONLY", Definition = "Only JSON." });
        request.JsonCatalog.CodeSets.Add(jsonOnly);
        IExternalCodeSetImporter importer = new ExternalCodeSetImporter();

        ExternalCodeCatalogComparisonResult result = importer.CompareCatalogs(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Equivalent, Is.False);
            Assert.That(result.Disagreements.Any(item =>
                item.Kind == ExternalCodeDisagreementKind.CodeSetPresence &&
                item.CodeSetIdentifier == "ExternalJsonOnly1Code" &&
                item.JsonPresent && !item.XsdPresent && !item.XlsxPresent), Is.True);
            Assert.That(result.Disagreements.Any(item =>
                item.Kind == ExternalCodeDisagreementKind.CodeValueMultiplicity &&
                item.CodeSetIdentifier == "ExternalStatus1Code" &&
                item.CodeValue == "ACTV" && item.JsonValue == "1" &&
                item.XsdValue == "2" && item.XlsxValue == "1"), Is.True);
            Assert.That(result.Disagreements.Any(item =>
                item.FieldName == "MinimumLength" && item.JsonValue == "1" &&
                item.XsdValue == "2" && !item.XlsxPresent), Is.True);
            Assert.That(result.Disagreements.Any(item =>
                item.FieldName == "Definition" &&
                item.CodeSetIdentifier == "ExternalStatus1Code"), Is.True);
            Assert.That(result.Disagreements.Any(item =>
                item.FieldName == "RegistrationStatus" && !item.JsonPresent &&
                item.XsdValue == "Registered" && item.XlsxValue == "Obsolete"), Is.True);
            Assert.That(result.Disagreements.Any(item =>
                item.FieldName == "Requester" && item.CodeValue == "ACTV"), Is.True);
        });
    }

    [Test]
    public void ComparisonReportsEditionMismatchTruncatesAndHonorsInvalidInputAndCancellation()
    {
        IExternalCodeSetImporter importer = new ExternalCodeSetImporter();
        ExternalCodeCatalogComparisonRequest editionRequest = CreateEquivalentRequest();
        editionRequest.XlsxCatalog!.Edition = "2026-Q2";
        ExternalCodeCatalogComparisonResult edition = importer.CompareCatalogs(editionRequest);

        ExternalCodeCatalogComparisonRequest truncatedRequest = CreateEquivalentRequest();
        truncatedRequest.MaxDisagreementCount = 1;
        truncatedRequest.XsdCatalog!.CodeSets[0].Definition = "different";
        truncatedRequest.XsdCatalog.CodeSets[0].MinimumLength = 2;
        truncatedRequest.XlsxCatalog!.CodeSets[0].RegistrationStatus = "Obsolete";
        ExternalCodeCatalogComparisonResult truncated = importer.CompareCatalogs(truncatedRequest);

        ExternalCodeCatalogComparisonRequest formatRequest = CreateEquivalentRequest();
        formatRequest.JsonCatalog!.SourceFormat = "XSD";
        ExternalCodeCatalogComparisonResult format = importer.CompareCatalogs(formatRequest);

        ExternalCodeCatalogComparisonRequest cancellationRequest = CreateEquivalentRequest();
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        cancellationRequest.CancellationToken = cancellation.Token;

        Assert.Multiple(() =>
        {
            Assert.That(edition.Success, Is.True);
            Assert.That(edition.Equivalent, Is.False);
            Assert.That(edition.Edition, Is.Empty);
            Assert.That(edition.Disagreements, Has.Count.EqualTo(1));
            Assert.That(edition.Disagreements[0].Kind,
                Is.EqualTo(ExternalCodeDisagreementKind.Edition));
            Assert.That(edition.Disagreements[0].JsonValue, Is.EqualTo("2026-Q1"));
            Assert.That(edition.Disagreements[0].XlsxValue, Is.EqualTo("2026-Q2"));
            Assert.That(truncated.Success, Is.True);
            Assert.That(truncated.Equivalent, Is.False);
            Assert.That(truncated.Truncated, Is.True);
            Assert.That(truncated.Disagreements, Has.Count.EqualTo(1));
            Assert.That(format.Success, Is.False);
            Assert.That(format.ErrorCode, Is.EqualTo("external-code.compare.format"));
            Assert.That(
                () => importer.CompareCatalogs(cancellationRequest),
                Throws.InstanceOf<OperationCanceledException>());
        });
    }

    [Test]
    public void BundledOfficialEditionReportsMaterialFormatDisagreementsDeterministically()
    {
        string specificationRoot = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "../../../../specs/iso20022"));
        IExternalCodeSetImporter importer = new ExternalCodeSetImporter();
        ExternalCodeCatalog json = ImportOfficialJson(importer, specificationRoot);
        ExternalCodeCatalog xsd = ImportOfficialXsd(importer, specificationRoot);
        ExternalCodeCatalog xlsx = ImportOfficialXlsx(importer, specificationRoot);
        ExternalCodeCatalogComparisonRequest request = new ExternalCodeCatalogComparisonRequest
        {
            JsonCatalog = json,
            XsdCatalog = xsd,
            XlsxCatalog = xlsx,
            MaxDisagreementCount = 1000
        };

        ExternalCodeCatalogComparisonResult first = importer.CompareCatalogs(request);
        ExternalCodeCatalogComparisonResult second = importer.CompareCatalogs(request);
        ExternalCodeDisagreement duplicate = first.Disagreements.Single(item =>
            item.Kind == ExternalCodeDisagreementKind.CodeValueMultiplicity &&
            item.CodeSetIdentifier == "ExternalLocalInstrument1Code" &&
            item.CodeValue == "CTP");

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, first.ErrorMessage);
            Assert.That(first.Equivalent, Is.False);
            Assert.That(first.Edition, Is.EqualTo("2026-Q1"));
            Assert.That(first.ComparedCodeSetCount, Is.EqualTo(158));
            Assert.That(first.ComparedCodeValueCount, Is.GreaterThan(3200));
            Assert.That(first.Truncated, Is.False);
            Assert.That(duplicate.JsonValue, Is.EqualTo("1"));
            Assert.That(duplicate.XsdValue, Is.EqualTo("2"));
            Assert.That(duplicate.XlsxValue, Is.EqualTo("2"));
            Assert.That(first.Disagreements.Select(DescribeDisagreement),
                Is.EqualTo(second.Disagreements.Select(DescribeDisagreement)));
        });
    }

    private static ExternalCodeCatalogComparisonRequest CreateEquivalentRequest()
    {
        ExternalCodeSet jsonSet = CreateSet(false);
        ExternalCodeSet xsdSet = CreateSet(true);
        ExternalCodeSet xlsxSet = CreateSet(true);
        xlsxSet.HasMinimumLength = false;
        xlsxSet.MinimumLength = 0;
        xlsxSet.HasMaximumLength = false;
        xlsxSet.MaximumLength = 0;
        xlsxSet.Pattern = string.Empty;
        xlsxSet.Values[0].MappingStandards.Add("ISO 15022");
        xlsxSet.Values[0].MappingValues.Add("A1");
        ExternalCodeCatalog json = CreateCatalog("JSON", jsonSet);
        ExternalCodeCatalog xsd = CreateCatalog("XSD", xsdSet);
        ExternalCodeCatalog xlsx = CreateCatalog("XLSX", xlsxSet);
        return new ExternalCodeCatalogComparisonRequest
        {
            JsonCatalog = json,
            XsdCatalog = xsd,
            XlsxCatalog = xlsx,
            MaxDisagreementCount = 100
        };
    }

    private static ExternalCodeSet CreateSet(bool lifecycle)
    {
        ExternalCodeSet set = new ExternalCodeSet
        {
            Identifier = "ExternalStatus1Code",
            Name = "ExternalStatus1Code",
            Definition = "Specifies status.",
            RegistrationStatus = lifecycle ? "Registered" : string.Empty,
            HasMinimumLength = true,
            MinimumLength = 1,
            HasMaximumLength = true,
            MaximumLength = 4,
            Pattern = "[A-Z]{1,4}"
        };
        ExternalCodeValue value = new ExternalCodeValue
        {
            Value = "ACTV",
            Name = lifecycle ? "Active" : string.Empty,
            Definition = "The code is active.",
            Requester = lifecycle ? "ISO requester" : string.Empty,
            RegistrationStatus = lifecycle ? "Registered" : string.Empty,
            LastUpdatedDate = lifecycle ? CreateDate("2026-05-11") : null,
            CreationDate = lifecycle ? CreateDate("2026-05-01") : null,
            AdditionalInformation = lifecycle ? "Published note." : string.Empty
        };
        set.Values.Add(value);
        return set;
    }

    private static ExternalCodeCatalog CreateCatalog(string format, ExternalCodeSet set)
    {
        ExternalCodeCatalog catalog = new ExternalCodeCatalog
        {
            Edition = "2026-Q1",
            SourceFormat = format
        };
        catalog.CodeSets.Add(set);
        return catalog;
    }

    private static IsoDateValue CreateDate(string lexical)
    {
        string[] parts = lexical.Split('-');
        return new IsoDateValue
        {
            Year = int.Parse(parts[0]),
            Month = int.Parse(parts[1]),
            Day = int.Parse(parts[2]),
            LexicalValue = lexical
        };
    }

    private static ExternalCodeCatalog ImportOfficialJson(
        IExternalCodeSetImporter importer,
        string specificationRoot)
    {
        string path = Path.Combine(specificationRoot, "ExternalCodeSets_JSON_2026-05.zip");
        ExternalCodeImportResult result = importer.ImportJsonStream(
            new MemoryStream(File.ReadAllBytes(path)),
            CreateImportRequest(path));
        return result.Catalog ?? throw new AssertionException(result.ErrorMessage);
    }

    private static ExternalCodeCatalog ImportOfficialXsd(
        IExternalCodeSetImporter importer,
        string specificationRoot)
    {
        string path = Path.Combine(specificationRoot, "ExternalCodeSets_XSD_2026-05.zip");
        ExternalCodeImportResult result = importer.ImportXsdStream(
            new MemoryStream(File.ReadAllBytes(path)),
            CreateImportRequest(path));
        return result.Catalog ?? throw new AssertionException(result.ErrorMessage);
    }

    private static ExternalCodeCatalog ImportOfficialXlsx(
        IExternalCodeSetImporter importer,
        string specificationRoot)
    {
        string path = Path.Combine(specificationRoot, "ExternalCodeSets_XLSX_2026-05.zip");
        ExternalCodeImportResult result = importer.ImportXlsxStream(
            new MemoryStream(File.ReadAllBytes(path)),
            CreateImportRequest(path));
        return result.Catalog ?? throw new AssertionException(result.ErrorMessage);
    }

    private static ExternalCodeImportRequest CreateImportRequest(string path)
    {
        return new ExternalCodeImportRequest
        {
            SourceName = path,
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

    private static string DescribeDisagreement(ExternalCodeDisagreement item)
    {
        return item.Code + ":" + item.CodeSetIdentifier + ":" + item.CodeValue + ":" +
            item.FieldName + ":" + item.JsonValue + ":" + item.XsdValue + ":" +
            item.XlsxValue;
    }
}
