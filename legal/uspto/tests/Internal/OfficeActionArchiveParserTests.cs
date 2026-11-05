using System.Text;
using System.Text.Json;
using uspto;

namespace uspto.Tests;

public class OfficeActionArchiveParserTests
{
    [Test]
    public void JsonLinesDetectAndParseAllFourArchiveRecordGenerations()
    {
        string jsonLines = """
        {"id":"A-1","application_number":"18/123456","body_text":["Complete action"],"submission_date":"2024-01-02","sections":{"summary_text":["Summary"],"section_103_rejection_text":"Claims rejected"},"futureAction":{"kept":true}}
        {"id":"C-1","patent_application_number":"18123456","reference_identifier":"US 2024/0123456 A1","legal_section_code":"103","office_action_citation_reference_indicator":true,"create_date_time":"2024-01-03T10:11:12Z","futureCitation":7}
        {"id":"R-1","patent_application_number":"18123456","claim_number_array_document":["1","2"],"has_rej_103":true,"cite_103_max":"2","allowed_claim_indicator":false,"submission_date":"20240104"}
        {"id":"E-1","patent_application_number":"18123456","cited_document_identifier":"US20240123456A1","publication_number":"20240123456","kind_code":"A1","country_code":"US","related_claim_number_text":"1, 2","passage_location_text":["p. 3","Fig. 1"],"npl_indicator":false,"office_action_date":"2024-01-05","quality_summary_text":"machine reviewed"}
        """;
        JsonRecord[] records = ReadJsonLines(jsonLines);

        OfficeActionArchiveRecord action = OfficeActionArchiveParser.ParseJsonRecord(records[0], null, CreateProvenance());
        OfficeActionArchiveRecord citation = OfficeActionArchiveParser.ParseJsonRecord(records[1], null, CreateProvenance());
        OfficeActionArchiveRecord rejection = OfficeActionArchiveParser.ParseJsonRecord(records[2], null, CreateProvenance());
        OfficeActionArchiveRecord enriched = OfficeActionArchiveParser.ParseJsonRecord(records[3], null, CreateProvenance());

        Assert.Multiple(() =>
        {
            Assert.That(action.Generation, Is.EqualTo("action-text"));
            Assert.That(action.DatasetIdentifier, Is.EqualTo("oa_actions"));
            Assert.That(action.ActionText?.ApplicationNumbers, Is.EqualTo(new string[] { "18/123456" }));
            Assert.That(action.ActionText?.BodyText, Is.EqualTo(new string[] { "Complete action" }));
            Assert.That(action.ActionText?.Sections.SummaryText, Is.EqualTo(new string[] { "Summary" }));
            Assert.That(action.ActionText?.Sections.Section103RejectionText, Is.EqualTo(new string[] { "Claims rejected" }));
            Assert.That(action.ActionText?.UnknownFields["futureAction"], Is.EqualTo("{\"kept\":true}"));
            Assert.That(action.RawFields["futureAction.kept"], Is.EqualTo("true"));
            Assert.That(action.RawSource, Does.Contain("futureAction"));
            Assert.That(action.Provenance.Source, Is.EqualTo(DataProvenance.Bulk));
            Assert.That(action.Provenance.IsAuthoritative, Is.True);

            Assert.That(citation.Generation, Is.EqualTo("citation"));
            Assert.That(citation.Citation?.ApplicationNumber, Is.EqualTo("18123456"));
            Assert.That(citation.Citation?.LegalSectionCode, Is.EqualTo("103"));
            Assert.That(citation.Citation?.OfficeActionCitationReferenceIndicator, Is.True);
            Assert.That(citation.Citation?.HasOfficeActionCitationReferenceIndicator, Is.True);
            Assert.That(citation.Citation?.CitedIdentifier.CanonicalPatentIdentifier, Is.EqualTo("US20240123456A1"));
            Assert.That(citation.Citation?.HasCreatedAt, Is.True);
            Assert.That(citation.Citation?.UnknownFields["futureCitation"], Is.EqualTo("7"));

            Assert.That(rejection.Generation, Is.EqualTo("rejection"));
            Assert.That(rejection.Rejection?.ApplicationNumber, Is.EqualTo("18123456"));
            Assert.That(rejection.Rejection?.ClaimNumbers, Is.EqualTo(new string[] { "1", "2" }));
            Assert.That(rejection.Rejection?.HasSection103Rejection, Is.True);
            Assert.That(rejection.Rejection?.MaximumSection103CitationCount, Is.EqualTo(2));
            Assert.That(rejection.Rejection?.HasAllowedClaimIndicator, Is.True);
            Assert.That(rejection.Rejection?.AllowedClaimIndicator, Is.False);

            Assert.That(enriched.Generation, Is.EqualTo("enriched-citation"));
            Assert.That(enriched.EnrichedCitation?.ApplicationNumber, Is.EqualTo("18123456"));
            Assert.That(enriched.EnrichedCitation?.CitedDocumentIdentifier, Is.EqualTo("US20240123456A1"));
            Assert.That(enriched.EnrichedCitation?.PublicationNumber, Is.EqualTo("20240123456"));
            Assert.That(enriched.EnrichedCitation?.KindCode, Is.EqualTo("A1"));
            Assert.That(enriched.EnrichedCitation?.CountryCode, Is.EqualTo("US"));
            Assert.That(enriched.EnrichedCitation?.RelatedClaimNumbers, Is.EqualTo(new string[] { "1", "2" }));
            Assert.That(enriched.EnrichedCitation?.PassageLocations, Is.EqualTo(new string[] { "p. 3", "Fig. 1" }));
            Assert.That(enriched.EnrichedCitation?.QualitySummaryText, Is.EqualTo("machine reviewed"));
            Assert.That(enriched.EnrichedCitation?.NonPatentLiteratureIndicator, Is.False);
            Assert.That(enriched.EnrichedCitation?.HasNonPatentLiteratureIndicator, Is.True);
            Assert.That(enriched.EnrichedCitation?.OfficeActionDate, Is.EqualTo(new DateTimeOffset(2024, 1, 5, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(enriched.EnrichedCitation?.HasOfficeActionDate, Is.True);
            Assert.That(enriched.EnrichedCitation?.CitedIdentifier.CanonicalPatentIdentifier, Is.EqualTo("US20240123456A1"));
        });
    }

    [Test]
    public void WrapperDatasetMarkerAndNestedSectionsSelectHistoricalActionShape()
    {
        string json = """
        {
          "dataset_name":"office-action-text-v0",
          "record":{
            "ID":"OLD-1",
            "applicationNumberText":"17/000001",
            "officeActionBodyText":"Historical body",
            "section101Text":"Historical eligibility rejection",
            "application_status_number":"150",
            "last_modified_timestamp":"2023-02-01T04:05:06Z"
          }
        }
        """;
        JsonRecord record = CreateJsonRecord(json, 9L, 22);

        OfficeActionArchiveRecord result = OfficeActionArchiveParser.ParseJsonRecord(record, null, CreateProvenance());

        Assert.Multiple(() =>
        {
            Assert.That(result.RecordNumber, Is.EqualTo(9L));
            Assert.That(result.StartLineNumber, Is.EqualTo(22L));
            Assert.That(result.Format, Is.EqualTo(DocumentFormat.Json));
            Assert.That(result.DatasetIdentifier, Is.EqualTo("oa_actions"));
            Assert.That(result.ActionText?.Identifier, Is.EqualTo("OLD-1"));
            Assert.That(result.ActionText?.ApplicationNumbers.Single(), Is.EqualTo("17/000001"));
            Assert.That(result.ActionText?.BodyText.Single(), Is.EqualTo("Historical body"));
            Assert.That(result.ActionText?.Sections.Section101RejectionText.Single(), Is.EqualTo("Historical eligibility rejection"));
            Assert.That(result.ActionText?.ApplicationStatusNumber, Is.EqualTo(150));
            Assert.That(result.ActionText?.HasLastModifiedAt, Is.True);
            Assert.That(result.NormalizedJson, Does.Contain("\"sections.section101RejectionText\""));
            Assert.That(result.RawFields["section101Text"], Is.EqualTo("\"Historical eligibility rejection\""));
        });
    }

    [Test]
    public void NestedDatasetMarkerSelectsCitationGenerationAndRetainsUnknownFields()
    {
        string json = """
        {
          "dataset":"oa_actions",
          "document":{
            "record_type":"office-action-citations-v2",
            "id":"NESTED-CITATION",
            "reference_identifier":"US 7,654,321 B2",
            "futureObject":{"confidence":0.75}
          }
        }
        """;

        OfficeActionArchiveRecord result = OfficeActionArchiveParser.ParseJsonRecord(CreateJsonRecord(json, 10L, 24), null, CreateProvenance());

        Assert.Multiple(() =>
        {
            Assert.That(result.Generation, Is.EqualTo("citation"));
            Assert.That(result.DatasetIdentifier, Is.EqualTo("oa_citations"));
            Assert.That(result.Citation?.Identifier, Is.EqualTo("NESTED-CITATION"));
            Assert.That(result.Citation?.CitedIdentifier.CanonicalPatentIdentifier, Is.EqualTo("US7654321B2"));
            Assert.That(result.Citation?.UnknownFields["futureObject"], Is.EqualTo("{\"confidence\":0.75}"));
            Assert.That(result.RawFields["futureObject.confidence"], Is.EqualTo("0.75"));
        });
    }

    [Test]
    public void NonObjectAndMalformedJsonArchiveVariantsAreRejected()
    {
        ParseException shape = Assert.Throws<ParseException>(() => OfficeActionArchiveParser.ParseJsonRecord(CreateJsonRecord("[]", 11L, 30), null, CreateProvenance()));
        JsonRecord malformed = new JsonRecord();
        malformed.RecordNumber = 12L;
        malformed.StartLineNumber = 31;
        malformed.RawJson = "{\"body_text\":";
        JsonException syntax = Assert.Catch<JsonException>(() => OfficeActionArchiveParser.ParseJsonRecord(malformed, null, CreateProvenance()));

        Assert.Multiple(() =>
        {
            Assert.That(shape.Format, Is.EqualTo(DocumentFormat.Json));
            Assert.That(shape.DataPath, Is.EqualTo("$[11]"));
            Assert.That(shape.LineNumber, Is.EqualTo(30));
            Assert.That(syntax.Message, Does.Contain("JSON object or array"));
        });
    }

    [Test]
    public void DelimitedRowsNormalizeDatasetSpecificFlagsAndRetainSourceColumns()
    {
        DelimitedRow actionRow = ReadCsvRow("id,application_number,body_text,sections_summary_text,application_status_number,future_column\nA-CSV,18111111,Action body,CSV summary,151,raw");
        DelimitedRow citationRow = ReadCsvRow("id,reference_identifier,examiner_cited_reference_indicator,paragraph_number\nC-CSV,US7654321B2,Y,17");
        DelimitedRow rejectionRow = ReadCsvRow("id,claim_number_array_document,has_rej_102,closing_missing\nR-CSV,4,Y,1");
        DelimitedRow enrichedRow = ReadCsvRow("id,cited_document_identifier,publication_number,kind_code,country_code,npl_indicator,passage_location_text\nE-CSV,US20240111111A1,20240111111,A1,US,N,paragraph 8");

        OfficeActionArchiveRecord action = OfficeActionArchiveParser.ParseDelimitedRow(actionRow, "oa_actions_2024.csv", CreateProvenance());
        OfficeActionArchiveRecord citation = OfficeActionArchiveParser.ParseDelimitedRow(citationRow, "oa-citations-v1.csv", CreateProvenance());
        OfficeActionArchiveRecord rejection = OfficeActionArchiveParser.ParseDelimitedRow(rejectionRow, "rejections-2019.tsv", CreateProvenance());
        OfficeActionArchiveRecord enriched = OfficeActionArchiveParser.ParseDelimitedRow(enrichedRow, "enriched-cited-reference-metadata", CreateProvenance());

        Assert.Multiple(() =>
        {
            Assert.That(action.Format, Is.EqualTo(DocumentFormat.Csv));
            Assert.That(action.ActionText?.ApplicationNumbers.Single(), Is.EqualTo("18111111"));
            Assert.That(action.ActionText?.BodyText.Single(), Is.EqualTo("Action body"));
            Assert.That(action.ActionText?.Sections.SummaryText.Single(), Is.EqualTo("CSV summary"));
            Assert.That(action.ActionText?.ApplicationStatusNumber, Is.EqualTo(151));
            Assert.That(action.ActionText?.UnknownFields["future_column"], Is.EqualTo("\"raw\""));
            Assert.That(action.RawFields["future_column"], Is.EqualTo("raw"));

            Assert.That(citation.Citation?.ExaminerCitedReferenceIndicator, Is.True);
            Assert.That(citation.Citation?.HasExaminerCitedReferenceIndicator, Is.True);
            Assert.That(citation.Citation?.ParagraphNumber, Is.EqualTo("17"));

            Assert.That(rejection.Rejection?.ClaimNumbers, Is.EqualTo(new string[] { "4" }));
            Assert.That(rejection.Rejection?.HasSection102Rejection, Is.True);
            Assert.That(rejection.Rejection?.ClosingMissingIndicator, Is.EqualTo(1));

            Assert.That(enriched.EnrichedCitation?.NonPatentLiteratureIndicator, Is.False);
            Assert.That(enriched.EnrichedCitation?.HasNonPatentLiteratureIndicator, Is.True);
            Assert.That(enriched.EnrichedCitation?.PassageLocations, Is.EqualTo(new string[] { "paragraph 8" }));
        });
    }

    [Test]
    public void JsonArrayTextAndClaimColumnsRemainArraysAfterDelimitedNormalization()
    {
        DelimitedRow actionRow = CreateDelimitedRow(
            new string[] { "dataset", "id", "body_text", "sections_summary_text" },
            new string[] { "oa_actions", "A-ARRAY", "[\"first\",\"second\"]", "[\"summary one\",\"summary two\"]" });
        DelimitedRow rejectionRow = CreateDelimitedRow(
            new string[] { "id", "claim_number_array_document", "has_rej_101" },
            new string[] { "R-ARRAY", "[\"1\",\"3\",\"5\"]", "N" });

        OfficeActionArchiveRecord action = OfficeActionArchiveParser.ParseDelimitedRow(actionRow, null, CreateProvenance());
        OfficeActionArchiveRecord rejection = OfficeActionArchiveParser.ParseDelimitedRow(rejectionRow, "oa_rejections", CreateProvenance());

        Assert.Multiple(() =>
        {
            Assert.That(action.ActionText?.BodyText, Is.EqualTo(new string[] { "first", "second" }));
            Assert.That(action.ActionText?.Sections.SummaryText, Is.EqualTo(new string[] { "summary one", "summary two" }));
            Assert.That(rejection.Rejection?.ClaimNumbers, Is.EqualTo(new string[] { "1", "3", "5" }));
            Assert.That(rejection.Rejection?.HasSection101RejectionState, Is.True);
            Assert.That(rejection.Rejection?.HasSection101Rejection, Is.False);
        });
    }

    [Test]
    public void AliasCollisionsAndUnknownGenerationsFailWithoutGuessing()
    {
        JsonRecord collision = CreateJsonRecord("{\"application_number\":\"1\",\"patentApplicationNumber\":\"2\",\"bodyText\":\"action\"}", 2L, 5);
        ParseException duplicate = Assert.Throws<ParseException>(() => OfficeActionArchiveParser.ParseJsonRecord(collision, null, CreateProvenance()));
        JsonRecord unknown = CreateJsonRecord("{\"id\":\"UNKNOWN\",\"unrelated\":true}", 3L, 8);
        ParseException generation = Assert.Throws<ParseException>(() => OfficeActionArchiveParser.ParseJsonRecord(unknown, null, CreateProvenance()));

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.Format, Is.EqualTo(DocumentFormat.Json));
            Assert.That(duplicate.Message, Does.Contain("duplicate aliases"));
            Assert.That(duplicate.DataPath, Is.EqualTo("$.patentApplicationNumber"));
            Assert.That(generation.SourceName, Is.EqualTo("Office Action weekly archive"));
            Assert.That(generation.DataPath, Is.EqualTo("$[3]"));
            Assert.That(generation.LineNumber, Is.EqualTo(8));
        });
    }

    [Test]
    public void MalformedDelimitedShapeAndMissingRawJsonAreRejected()
    {
        DelimitedRow malformed = CreateDelimitedRow(new string[] { "id", "bodyText" }, new string[] { "A" });
        malformed.HasMissingColumns = true;
        malformed.MissingColumnCount = 1;
        ParseException rowFailure = Assert.Throws<ParseException>(() => OfficeActionArchiveParser.ParseDelimitedRow(malformed, "oa_actions", CreateProvenance()));
        JsonRecord missing = new JsonRecord();
        ValidationException jsonFailure = Assert.Throws<ValidationException>(() => OfficeActionArchiveParser.ParseJsonRecord(missing, null, CreateProvenance()));

        Assert.Multiple(() =>
        {
            Assert.That(rowFailure.Format, Is.EqualTo(DocumentFormat.Csv));
            Assert.That(rowFailure.DataPath, Is.EqualTo("row[1]"));
            Assert.That(jsonFailure.FieldName, Is.EqualTo("RawJson"));
        });
    }

    private static JsonRecord[] ReadJsonLines(string json)
    {
        using MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes(json));
        JsonRecordReader reader = new JsonRecordReader(input, true, "oact.jsonl", CreateLimits(), CancellationToken.None);
        return reader.ReadBatch(10);
    }

    private static JsonRecord CreateJsonRecord(string json, long recordNumber, int lineNumber)
    {
        JsonRecord result = new JsonRecord();
        result.RecordNumber = recordNumber;
        result.StartLineNumber = lineNumber;
        result.RawJson = json;
        return result;
    }

    private static DelimitedRow ReadCsvRow(string csv)
    {
        using MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        DelimitedReader reader = new DelimitedReader(input, ',', true, "oact.csv", CreateLimits(), CancellationToken.None);
        return reader.ReadRow() ?? throw new AssertionException("Expected a CSV row.");
    }

    private static DelimitedRow CreateDelimitedRow(string[] headers, string[] values)
    {
        DelimitedRow result = new DelimitedRow();
        result.RowNumber = 1L;
        result.StartLineNumber = 2L;
        result.EndLineNumber = 2L;
        result.Headers = headers;
        result.Values = values;
        result.ExpectedColumnCount = headers.Length;
        return result;
    }

    private static PatentDataProvenance CreateProvenance()
    {
        PatentDataProvenance result = new PatentDataProvenance();
        result.Source = DataProvenance.Bulk;
        result.DatasetIdentifier = "OACT";
        result.DatasetVersion = "2024-W01";
        return result;
    }

    private static UsptoLimits CreateLimits()
    {
        UsptoLimits result = new UsptoLimits();
        result.MaximumResults = 10;
        result.MaximumRawResponseBytes = 100000;
        result.MaximumJsonDepth = 32;
        result.MaximumStringCharacters = 10000;
        result.MaximumDelimitedFieldCharacters = 10000;
        result.MaximumDelimitedColumns = 100;
        return result;
    }
}
