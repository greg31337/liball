using System.Text;
using System.Text.Json;
using uspto;

namespace uspto.Tests;

public class BulkParserRouterTests
{
    [Test]
    public void SelectionUsesProductFilenameAndContentSignaturesWithArchiveAwareness()
    {
        BulkParserContext product = new BulkParserContext();
        product.ProductIdentifier = "TRTDXFAP";
        product.MediaType = "application/xml";
        BulkParserRoute productRoute = BulkParserRouter.SelectRoute(product);

        BulkParserContext file = new BulkParserContext();
        file.FileName = "tt250529.xml";
        BulkParserRoute fileRoute = BulkParserRouter.SelectRoute(file);

        BulkParserContext signature = new BulkParserContext();
        signature.FileName = "mislabelled.xml";
        signature.MediaType = "application/xml";
        signature.Prefix = Encoding.UTF8.GetBytes("\uFEFF  {\"future\":true}");
        BulkParserRoute signatureRoute = BulkParserRouter.SelectRoute(signature);

        BulkParserContext archive = new BulkParserContext();
        archive.FileName = "tt250529.xml.zip";
        archive.Prefix = new byte[] { 0x50, 0x4b, 0x03, 0x04, 0x00, 0x00 };
        BulkParserRoute archiveRoute = BulkParserRouter.SelectRoute(archive);

        Assert.Multiple(() =>
        {
            Assert.That(productRoute.Family, Is.EqualTo(DatasetFamily.TrademarkApplicationAndStatus));
            Assert.That(productRoute.Format, Is.EqualTo(DocumentFormat.Xml));
            Assert.That(productRoute.SelectionReason, Is.EqualTo("product-identifier"));
            Assert.That(productRoute.ParserName, Is.EqualTo("trademark-daily"));
            Assert.That(productRoute.IsTyped, Is.True);

            Assert.That(fileRoute.Family, Is.EqualTo(DatasetFamily.Ttab));
            Assert.That(fileRoute.Format, Is.EqualTo(DocumentFormat.Xml));
            Assert.That(fileRoute.SelectionReason, Is.EqualTo("filename"));
            Assert.That(fileRoute.ParserName, Is.EqualTo("trademark-ttab"));

            Assert.That(signatureRoute.Format, Is.EqualTo(DocumentFormat.Json));
            Assert.That(signatureRoute.FormatDetectedBySignature, Is.True);
            Assert.That(signatureRoute.ParserName, Is.EqualTo("generic-json"));

            Assert.That(archiveRoute.Compression, Is.EqualTo(CompressionFormat.Zip));
            Assert.That(archiveRoute.Format, Is.EqualTo(DocumentFormat.Zip));
            Assert.That(archiveRoute.FormatDetectedBySignature, Is.True);
            Assert.That(archiveRoute.ParserName, Is.EqualTo("archive"));
            Assert.That(archiveRoute.IsTyped, Is.False);
        });
    }

    [Test]
    public void ContentSchemaOverridesConflictingCatalogMetadataAndRoutesTtab()
    {
        string xml = "<ttab-proceedings><proceeding-information><proceeding-entry><number>91234567</number><type-code>OPP</type-code><prosecution-history><prosecution-entry><code>1000</code><history-text>Filed</history-text></prosecution-entry></prosecution-history></proceeding-entry></proceeding-information></ttab-proceedings>";
        XmlRecord record = CreateXmlRecord(xml);
        BulkParserContext context = new BulkParserContext();
        context.ProductIdentifier = "TRTDXFAG";
        context.FileName = "asb240101.xml";
        context.MediaType = "application/xml";

        BulkParserRoute result = BulkParserRouter.RouteXmlRecord(record, context, CreateProvenance());
        TrademarkTtabXmlRecord parsed = (TrademarkTtabXmlRecord)result.Record!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Family, Is.EqualTo(DatasetFamily.Ttab));
            Assert.That(result.SelectionReason, Is.EqualTo("content-schema"));
            Assert.That(result.FamilyDetectedBySchema, Is.True);
            Assert.That(result.ParserName, Is.EqualTo("trademark-ttab"));
            Assert.That(result.IsTyped, Is.True);
            Assert.That(parsed.Proceeding?.Number, Is.EqualTo("91234567"));
            Assert.That(parsed.Proceeding?.Events.Single().Code, Is.EqualTo("1000"));
        });
    }

    [Test]
    public void ProductIdentifierRoutesGenericAssignmentRootToPatentParser()
    {
        XmlRecord record = CreateXmlRecord("<assignment><assignment-record><reel-no>100</reel-no><frame-no>20</frame-no><conveyance-text>ASSIGNMENT</conveyance-text></assignment-record></assignment>");
        BulkParserContext context = new BulkParserContext();
        context.ProductIdentifier = "PASDL";
        context.MediaType = "application/xml";

        BulkParserRoute result = BulkParserRouter.RouteXmlRecord(record, context, CreateProvenance());
        PatentAssignmentFlatRecord parsed = (PatentAssignmentFlatRecord)result.Record!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Family, Is.EqualTo(DatasetFamily.PatentAssignment));
            Assert.That(result.SelectionReason, Is.EqualTo("product-identifier"));
            Assert.That(result.ParserName, Is.EqualTo("patent-assignment"));
            Assert.That(parsed.ReelAndFrameKey, Is.EqualTo("100/20"));
            Assert.That(parsed.Transaction.Conveyance, Is.EqualTo("ASSIGNMENT"));
        });
    }

    [Test]
    public void FilenameRoutesGenericAssignmentRootToTrademarkParser()
    {
        XmlRecord record = CreateXmlRecord("<assignment><assignment-record><reel-no>200</reel-no><frame-no>30</frame-no><conveyance>MERGER</conveyance></assignment-record></assignment>");
        BulkParserContext context = new BulkParserContext();
        context.FileName = "asb260903.xml";

        BulkParserRoute result = BulkParserRouter.RouteXmlRecord(record, context, CreateProvenance());
        TrademarkAssignmentXmlRecord parsed = (TrademarkAssignmentXmlRecord)result.Record!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Family, Is.EqualTo(DatasetFamily.TrademarkAssignment));
            Assert.That(result.SelectionReason, Is.EqualTo("filename"));
            Assert.That(result.ParserName, Is.EqualTo("trademark-assignment"));
            Assert.That(parsed.ReelAndFrameKey, Is.EqualTo("200/30"));
            Assert.That(parsed.Assignment.Conveyance, Is.EqualTo("MERGER"));
        });
    }

    [Test]
    public void ProductIdentifierRoutesJsonFileWrapperRecord()
    {
        JsonRecord record = CreateJsonRecord("{\"applicationNumberText\":\"14/104,993\",\"applicationMetaData\":{\"inventionTitle\":\"Routed wrapper\"}}");
        BulkParserContext context = new BulkParserContext();
        context.ProductIdentifier = "PTFWPRE";
        context.FileName = "weekly.data";

        BulkParserRoute result = BulkParserRouter.RouteJsonRecord(record, context, CreateProvenance());
        PatentFileWrapperBulkRecord parsed = (PatentFileWrapperBulkRecord)result.Record!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Family, Is.EqualTo(DatasetFamily.PatentFileWrapperComplete));
            Assert.That(result.SelectionReason, Is.EqualTo("product-identifier"));
            Assert.That(result.ParserName, Is.EqualTo("patent-file-wrapper"));
            Assert.That(parsed.ApplicationNumber, Is.EqualTo("14104993"));
            Assert.That(parsed.Official.Application?.Metadata.InventionTitle, Is.EqualTo("Routed wrapper"));
        });
    }

    [Test]
    public void SchemaMarkerAndFilenameRouteOfficeActionJsonAndDelimitedRows()
    {
        JsonRecord json = CreateJsonRecord("{\"id\":\"A-1\",\"application_number\":\"18/123456\",\"body_text\":[\"Action body\"]}");
        BulkParserContext jsonContext = new BulkParserContext();
        jsonContext.SchemaMarker = "oa_actions";
        BulkParserRoute jsonResult = BulkParserRouter.RouteJsonRecord(json, jsonContext, CreateProvenance());

        DelimitedRow row = CreateDelimitedRow(new[] { "id", "application_number", "body_text" }, new[] { "A-2", "18111111", "Delimited body" });
        BulkParserContext rowContext = new BulkParserContext();
        rowContext.FileName = "oa_actions_2026.csv";
        BulkParserRoute rowResult = BulkParserRouter.RouteDelimitedRow(row, ',', rowContext, CreateProvenance());

        OfficeActionArchiveRecord jsonParsed = (OfficeActionArchiveRecord)jsonResult.Record!;
        OfficeActionArchiveRecord rowParsed = (OfficeActionArchiveRecord)rowResult.Record!;
        Assert.Multiple(() =>
        {
            Assert.That(jsonResult.SelectionReason, Is.EqualTo("schema-marker"));
            Assert.That(jsonResult.FamilyDetectedBySchema, Is.True);
            Assert.That(jsonParsed.ActionText?.BodyText.Single(), Is.EqualTo("Action body"));
            Assert.That(rowResult.SelectionReason, Is.EqualTo("filename"));
            Assert.That(rowResult.Format, Is.EqualTo(DocumentFormat.Csv));
            Assert.That(rowParsed.ActionText?.ApplicationNumbers.Single(), Is.EqualTo("18111111"));
            Assert.That(rowParsed.ActionText?.BodyText.Single(), Is.EqualTo("Delimited body"));
        });
    }

    [Test]
    public void PatentFullTextSchemaRoutesDespiteApplicationProductHint()
    {
        string xml = "<us-patent-grant id='G-1' country='US' date-produced='20260102'><us-bibliographic-data-grant><publication-reference><document-id><country>US</country><doc-number>123</doc-number><kind>B2</kind></document-id></publication-reference><invention-title>Router patent</invention-title></us-bibliographic-data-grant></us-patent-grant>";
        XmlRecord record = CreateXmlRecord(xml);
        BulkParserContext context = new BulkParserContext();
        context.ProductIdentifier = "APPXML";
        context.FileName = "wrong-application.xml";
        context.Prefix = Encoding.UTF8.GetBytes(xml);

        BulkParserRoute result = BulkParserRouter.RouteXmlRecord(record, context, CreateProvenance());
        PatentFullText parsed = (PatentFullText)result.Record!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Family, Is.EqualTo(DatasetFamily.PatentGrantFullText));
            Assert.That(result.SelectionReason, Is.EqualTo("content-schema"));
            Assert.That(result.ParserName, Is.EqualTo("patent-full-text"));
            Assert.That(parsed.DocumentCategory, Is.EqualTo("grant"));
            Assert.That(parsed.BibliographicData.InventionTitle, Is.EqualTo("Router patent"));
        });
    }

    [Test]
    public void UnknownAndUnsupportedRepresentationsReturnOriginalGenericRecords()
    {
        JsonRecord unknownJson = CreateJsonRecord("{\"future\":true}");
        BulkParserRoute unknown = BulkParserRouter.RouteJsonRecord(unknownJson, new BulkParserContext(), CreateProvenance());

        JsonRecord assignmentJson = CreateJsonRecord("{\"reel\":\"1\",\"frame\":\"2\"}");
        BulkParserContext assignmentContext = new BulkParserContext();
        assignmentContext.ProductIdentifier = "PASDL";
        BulkParserRoute unsupported = BulkParserRouter.RouteJsonRecord(assignmentJson, assignmentContext, CreateProvenance());

        XmlRecord futureTtab = CreateXmlRecord("<future-board><matter-id>X-1</matter-id></future-board>");
        BulkParserContext ttabContext = new BulkParserContext();
        ttabContext.ProductIdentifier = "TTABTDXF";
        BulkParserRoute future = BulkParserRouter.RouteXmlRecord(futureTtab, ttabContext, CreateProvenance());

        Assert.Multiple(() =>
        {
            Assert.That(unknown.Family, Is.EqualTo(DatasetFamily.Unknown));
            Assert.That(unknown.IsTyped, Is.False);
            Assert.That(unknown.ParserName, Is.EqualTo("generic-json"));
            Assert.That(unknown.Record, Is.SameAs(unknownJson));
            Assert.That(unsupported.Family, Is.EqualTo(DatasetFamily.PatentAssignment));
            Assert.That(unsupported.IsTyped, Is.False);
            Assert.That(unsupported.Record, Is.SameAs(assignmentJson));
            Assert.That(future.Family, Is.EqualTo(DatasetFamily.Ttab));
            Assert.That(future.IsTyped, Is.False);
            Assert.That(future.ParserName, Is.EqualTo("generic-xml"));
            Assert.That(future.Record, Is.SameAs(futureTtab));
        });
    }

    [Test]
    public void RouterValidatesContextProvenanceAndDelimiter()
    {
        ValidationException context = Assert.Throws<ValidationException>(() => BulkParserRouter.SelectRoute(null!));
        ValidationException provenance = Assert.Throws<ValidationException>(() => BulkParserRouter.RouteJsonRecord(CreateJsonRecord("{}"), new BulkParserContext(), null!));
        ValidationException delimiter = Assert.Throws<ValidationException>(() => BulkParserRouter.RouteDelimitedRow(CreateDelimitedRow(new[] { "id" }, new[] { "1" }), '|', new BulkParserContext(), CreateProvenance()));

        Assert.Multiple(() =>
        {
            Assert.That(context.FieldName, Is.EqualTo("context"));
            Assert.That(provenance.FieldName, Is.EqualTo("provenance"));
            Assert.That(delimiter.FieldName, Is.EqualTo("delimiter"));
        });
    }

    private static XmlRecord CreateXmlRecord(string xml)
    {
        XmlRecord result = new XmlRecord();
        result.RecordNumber = 1L;
        result.Path = "/records/record[1]";
        result.StartLineNumber = 4;
        result.HasLineInfo = true;
        result.RawXml = xml;
        return result;
    }

    private static JsonRecord CreateJsonRecord(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        JsonRecord result = new JsonRecord();
        result.RecordNumber = 1L;
        result.StartLineNumber = 1;
        result.RawJson = json;
        result.Element = document.RootElement.Clone();
        return result;
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
        result.DatasetIdentifier = "router-test";
        return result;
    }
}
