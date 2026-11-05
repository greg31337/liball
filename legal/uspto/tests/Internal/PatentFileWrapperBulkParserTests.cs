using System.Text;
using System.Text.Json;
using System.Xml;
using uspto;

namespace uspto.Tests;

public class PatentFileWrapperBulkParserTests
{
    [Test]
    public void JsonBulkRecordPopulatesEveryTypedFileWrapperSection()
    {
        string json = CreateCompleteJsonRecord("JSON invention");
        using MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes("[" + json + "]"));
        JsonRecordReader reader = new JsonRecordReader(input, false, "pfw.json", CreateLimits(), CancellationToken.None);
        JsonRecord generic = RequireJsonRecord(reader.ReadRecord());

        PatentFileWrapperBulkRecord result = PatentFileWrapperBulkParser.ParseJsonRecord(generic, CreateProvenance());

        AssertCompleteRecord(result, "JSON invention");
        Assert.Multiple(() =>
        {
            Assert.That(result.Format, Is.EqualTo(DocumentFormat.Json));
            Assert.That(result.RawSource, Is.EqualTo(json));
            Assert.That(result.NormalizedJson, Is.EqualTo(json));
            Assert.That(result.Official.Application?.Metadata.UnknownFields["futureMetadata"], Is.EqualTo("{\"enabled\":true}"));
            Assert.That(result.Official.Application?.UnknownSections["futureSection"], Is.EqualTo("{\"retained\":true}"));
            Assert.That(result.Official.Transactions?.Events[0].UnknownFields["futureEvent"], Is.EqualTo("9"));
        });
    }

    [Test]
    public void XmlBulkRecordSupportsNamespacesAndKebabCaseBagContainers()
    {
        string xml = CreateCompleteXmlRecord();
        using MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes("<b:records xmlns:b=\"urn:bulk\">" + xml + "</b:records>"));
        XmlRecordReader reader = new XmlRecordReader(input, "records", "urn:bulk", "patent-file-wrapper", "urn:bulk", "pfw.xml", CreateLimits(), CancellationToken.None);
        XmlRecord generic = RequireXmlRecord(reader.ReadRecord());

        PatentFileWrapperBulkRecord result = PatentFileWrapperBulkParser.ParseXmlRecord(generic, CreateProvenance());

        AssertCompleteRecord(result, "XML invention");
        Assert.Multiple(() =>
        {
            Assert.That(result.Format, Is.EqualTo(DocumentFormat.Xml));
            Assert.That(result.RawSource, Does.Contain("patent-file-wrapper"));
            Assert.That(result.NormalizedJson, Does.Contain("\"applicationNumberText\":\"14/104,993\""));
            Assert.That(result.NormalizedJson, Does.Contain("\"eventDataBag\":[{"));
            Assert.That(result.Official.Transactions?.Events[0].EventCode, Is.EqualTo("XML-EVENT"));
            Assert.That(result.Official.Documents?.Documents[0].Code, Is.EqualTo("XMLDOC"));
            Assert.That(result.Official.Attorneys?.Attorneys[0].Person.LastName, Is.EqualTo("XmlAttorney"));
        });
    }

    [Test]
    public void OneRecordJsonEnvelopeAndAbsentOptionalSectionsAreSupported()
    {
        string envelope = "{\"count\":1,\"patentFileWrapperDataBag\":[{\"applicationNumberText\":\"14/104,993\",\"applicationMetaData\":{\"inventionTitle\":\"Envelope\"}}]}";
        JsonRecord generic = new JsonRecord();
        generic.RecordNumber = 7L;
        generic.RawJson = envelope;

        PatentFileWrapperBulkRecord result = PatentFileWrapperBulkParser.ParseJsonRecord(generic, CreateProvenance());

        Assert.Multiple(() =>
        {
            Assert.That(result.ApplicationNumber, Is.EqualTo("14104993"));
            Assert.That(result.Official.Application?.Metadata.InventionTitle, Is.EqualTo("Envelope"));
            Assert.That(result.Official.Transactions?.Events, Is.Empty);
            Assert.That(result.Official.Documents?.Documents, Is.Empty);
            Assert.That(result.Official.AssociatedDocuments?.Count, Is.Zero);
            Assert.That(result.Official.Continuity?.Count, Is.Zero);
            Assert.That(result.Official.ForeignPriorities?.Count, Is.Zero);
            Assert.That(result.Official.Assignments?.Count, Is.Zero);
            Assert.That(result.Official.Attorneys?.Count, Is.Zero);
            Assert.That(result.Official.TermAdjustment?.Count, Is.Zero);
        });
    }

    [Test]
    public void XmlNilValuesBecomeJsonNullWithoutLosingTheOriginalSubtree()
    {
        string xml = "<patent-file-wrapper xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"><application-number-text>14/104,993</application-number-text><application-meta-data><invention-title xsi:nil=\"true\"/><future-value>kept</future-value></application-meta-data></patent-file-wrapper>";
        XmlRecord generic = new XmlRecord();
        generic.Path = "/records/patent-file-wrapper";
        generic.RawXml = xml;

        PatentFileWrapperBulkRecord result = PatentFileWrapperBulkParser.ParseXmlRecord(generic, CreateProvenance());

        Assert.Multiple(() =>
        {
            Assert.That(result.Official.Application?.Metadata.InventionTitle, Is.Null);
            Assert.That(result.Official.Application?.Metadata.UnknownFields["futureValue"], Is.EqualTo("\"kept\""));
            Assert.That(result.RawSource, Is.EqualTo(xml));
            Assert.That(result.NormalizedJson, Does.Contain("\"inventionTitle\":null"));
        });
    }

    [Test]
    public void LegacyXmlAttributesRepeatedFieldsAndDirectBagContentsAreRetained()
    {
        string xml = """
        <PATENT-FILE-WRAPPER export-version="legacy-1">
          <APPLICATION-NUMBER-TEXT>14/104,993</APPLICATION-NUMBER-TEXT>
          <APPLICATION-META-DATA><INVENTION-TITLE>Legacy XML</INVENTION-TITLE></APPLICATION-META-DATA>
          <DOCUMENT-BAG><APPLICATION-NUMBER-TEXT>14/104,993</APPLICATION-NUMBER-TEXT><DOCUMENT-IDENTIFIER>LEGACY-DOC</DOCUMENT-IDENTIFIER><DOCUMENT-CODE>IFW</DOCUMENT-CODE></DOCUMENT-BAG>
          <FUTURE-FIELD>first</FUTURE-FIELD><FUTURE-FIELD>second</FUTURE-FIELD>
        </PATENT-FILE-WRAPPER>
        """;
        XmlRecord generic = new XmlRecord();
        generic.Path = "/records/PATENT-FILE-WRAPPER";
        generic.RawXml = xml;

        PatentFileWrapperBulkRecord result = PatentFileWrapperBulkParser.ParseXmlRecord(generic, CreateProvenance());

        Assert.Multiple(() =>
        {
            Assert.That(result.ApplicationNumber, Is.EqualTo("14104993"));
            Assert.That(result.Official.Application?.Metadata.InventionTitle, Is.EqualTo("Legacy XML"));
            Assert.That(result.Official.Documents?.Documents.Single().DocumentIdentifier, Is.EqualTo("LEGACY-DOC"));
            Assert.That(result.Official.Application?.UnknownSections["exportVersion"], Is.EqualTo("\"legacy-1\""));
            Assert.That(result.Official.Application?.UnknownSections["futureField"], Is.EqualTo("[\"first\",\"second\"]"));
            Assert.That(result.NormalizedJson, Does.Contain("\"documentBag\":[{"));
        });
    }

    [Test]
    public void MalformedJsonAndXmlVariantsAreRejectedBeforeTypedDataIsReturned()
    {
        JsonRecord array = new JsonRecord();
        array.RecordNumber = 8L;
        array.RawJson = "[]";
        ParseException arrayFailure = Assert.Throws<ParseException>(() => PatentFileWrapperBulkParser.ParseJsonRecord(array, CreateProvenance()));

        JsonRecord malformedJson = new JsonRecord();
        malformedJson.RawJson = "{\"applicationNumberText\":";
        JsonException jsonFailure = Assert.Catch<JsonException>(() => PatentFileWrapperBulkParser.ParseJsonRecord(malformedJson, CreateProvenance()));

        XmlRecord invalidNumber = new XmlRecord();
        invalidNumber.Path = "/records/patent-file-wrapper";
        invalidNumber.RawXml = "<patent-file-wrapper><application-number-text>invalid</application-number-text></patent-file-wrapper>";
        ParseException numberFailure = Assert.Throws<ParseException>(() => PatentFileWrapperBulkParser.ParseXmlRecord(invalidNumber, CreateProvenance()));

        XmlRecord malformedXml = new XmlRecord();
        malformedXml.RawXml = "<patent-file-wrapper>";
        XmlException xmlFailure = Assert.Throws<XmlException>(() => PatentFileWrapperBulkParser.ParseXmlRecord(malformedXml, CreateProvenance()));

        Assert.Multiple(() =>
        {
            Assert.That(arrayFailure.DataPath, Is.EqualTo("$[8]"));
            Assert.That(jsonFailure.Message, Does.Contain("JSON object or array"));
            Assert.That(numberFailure.Format, Is.EqualTo(DocumentFormat.Xml));
            Assert.That(numberFailure.DataPath, Is.EqualTo("/records/patent-file-wrapper.applicationNumberText"));
            Assert.That(xmlFailure.Message, Does.Contain("unexpected end").IgnoreCase);
        });
    }

    [Test]
    public void InvalidRecordsReportStablePathsAndRequiredRawSourceValidation()
    {
        JsonRecord invalidNumber = new JsonRecord();
        invalidNumber.RecordNumber = 3L;
        invalidNumber.RawJson = "{\"applicationNumberText\":\"bad\",\"applicationMetaData\":{}}";
        ParseException number = Assert.Throws<ParseException>(() => PatentFileWrapperBulkParser.ParseJsonRecord(invalidNumber, CreateProvenance()));

        JsonRecord invalidEnvelope = new JsonRecord();
        invalidEnvelope.RawJson = "{\"patentFileWrapperDataBag\":[]}";
        ParseException envelope = Assert.Throws<ParseException>(() => PatentFileWrapperBulkParser.ParseJsonRecord(invalidEnvelope, CreateProvenance()));

        XmlRecord missingRaw = new XmlRecord();
        ValidationException raw = Assert.Throws<ValidationException>(() => PatentFileWrapperBulkParser.ParseXmlRecord(missingRaw, CreateProvenance()));

        Assert.Multiple(() =>
        {
            Assert.That(number.Format, Is.EqualTo(DocumentFormat.Json));
            Assert.That(number.DataPath, Is.EqualTo("$[3].applicationNumberText"));
            Assert.That(envelope.DataPath, Is.EqualTo("$.patentFileWrapperDataBag"));
            Assert.That(raw.FieldName, Is.EqualTo("RawXml"));
        });
    }

    private static void AssertCompleteRecord(PatentFileWrapperBulkRecord result, string expectedTitle)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.ApplicationNumber, Is.EqualTo("14104993"));
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.Bulk));
            Assert.That(result.Provenance.DatasetIdentifier, Is.EqualTo("PFW-COMPLETE"));
            Assert.That(result.Provenance.IsAuthoritative, Is.True);
            Assert.That(result.Official.Application?.Metadata.InventionTitle, Is.EqualTo(expectedTitle));
            Assert.That(result.Official.Application?.Metadata.HasFilingDate, Is.True);
            Assert.That(result.Official.Transactions?.Events.Single().EventCode, Does.Contain("EVENT"));
            Assert.That(result.Official.Documents?.Documents.Single().DocumentIdentifier, Is.EqualTo("DOC-1"));
            Assert.That(result.Official.Documents?.Documents.Single().DownloadAlternatives.Single().Format, Is.EqualTo(DocumentFormat.Pdf));
            Assert.That(result.Official.AssociatedDocuments?.PublicationFiles.Single().ProductIdentifier, Is.EqualTo("PGPUB"));
            Assert.That(result.Official.AssociatedDocuments?.GrantFiles.Single().ProductIdentifier, Is.EqualTo("GRANT"));
            Assert.That(result.Official.Continuity?.Parents.Single().ParentApplicationNumber, Is.EqualTo("13123456"));
            Assert.That(result.Official.Continuity?.Children.Single().ChildApplicationNumber, Is.EqualTo("15123456"));
            Assert.That(result.Official.ForeignPriorities?.Claims.Single().CountryCode, Is.EqualTo("EP"));
            Assert.That(result.Official.Assignments?.Assignments.Single().ReelAndFrameNumber, Is.EqualTo("01234/0567"));
            Assert.That(result.Official.Attorneys?.Attorneys.Single().RegistrationNumber, Is.EqualTo("54321"));
            Assert.That(result.Official.Attorneys?.Attorneys.Single().IsActive, Is.True);
            Assert.That(result.Official.TermAdjustment?.AdjustmentTotalDays, Is.EqualTo(42));
            Assert.That(result.Official.TermAdjustment?.History.Single().Code, Is.EqualTo("A"));
        });
    }

    private static string CreateCompleteJsonRecord(string title)
    {
        string json = """
        {
          "applicationNumberText":"14/104,993",
          "applicationMetaData":{"inventionTitle":"__TITLE__","filingDate":"2012-12-19","futureMetadata":{"enabled":true}},
          "eventDataBag":[{"eventSequenceNumber":"1","eventDate":"2024-01-02","eventCode":"JSON-EVENT","eventDescriptionText":"Filed","futureEvent":9}],
          "documentBag":[{"applicationNumberText":"14/104,993","documentIdentifier":"DOC-1","documentCode":"JSONDOC","documentCodeDescriptionText":"Document","downloadOptionBag":[{"mimeTypeIdentifier":"application/pdf","documentURI":"https://example.test/doc.pdf","pageTotalQuantity":"2"}]}],
          "parentContinuityBag":[{"parentApplicationNumberText":"13/123,456","childApplicationNumberText":"14/104,993","claimParentageTypeCode":"CON"}],
          "childContinuityBag":[{"parentApplicationNumberText":"14/104,993","childApplicationNumberText":"15/123,456","claimParentageTypeCode":"DIV"}],
          "foreignPriorityBag":[{"ipOfficeCode":"EP","applicationNumberText":"EP123","filingDate":"2011-01-02","priorityClaimSequenceNumber":"1"}],
          "assignmentBag":[{"reelNumber":"01234","frameNumber":"0567","conveyanceText":"ASSIGNMENT"}],
          "recordAttorney":{"attorneyBag":[{"firstName":"Ada","lastName":"JsonAttorney","registrationNumber":"54321","activeIndicator":"ACTIVE"}]},
          "patentTermAdjustmentData":{"adjustmentTotalQuantity":"42","aDelayQuantity":"10","patentTermAdjustmentHistoryDataBag":[{"eventDate":"2024-02-03","eventDescriptionText":"Delay","ptaPTECode":"A"}]},
          "pgpubDocumentMetaData":{"productIdentifier":"PGPUB","publicationNumber":"US20140123456A1","zipFileName":"pg.zip","fileMediaType":"application/zip"},
          "grantDocumentMetaData":{"productIdentifier":"GRANT","patentNumber":"9999999","xmlFileName":"grant.xml","fileMediaType":"application/xml"},
          "futureSection":{"retained":true}
        }
        """;
        return json.Replace("__TITLE__", title, StringComparison.Ordinal);
    }

    private static string CreateCompleteXmlRecord()
    {
        return """
        <b:patent-file-wrapper xmlns:b="urn:bulk">
          <b:application-number-text>14/104,993</b:application-number-text>
          <b:application-meta-data><b:invention-title>XML invention</b:invention-title><b:filing-date>2012-12-19</b:filing-date></b:application-meta-data>
          <b:event-data-bag><b:event-data><b:event-sequence-number>1</b:event-sequence-number><b:event-code>XML-EVENT</b:event-code></b:event-data></b:event-data-bag>
          <b:document-bag><b:document><b:application-number-text>14/104,993</b:application-number-text><b:document-identifier>DOC-1</b:document-identifier><b:document-code>XMLDOC</b:document-code><b:download-option-bag><b:download-option><b:mime-type-identifier>application/pdf</b:mime-type-identifier><b:document-u-r-i>https://example.test/xml.pdf</b:document-u-r-i></b:download-option></b:download-option-bag></b:document></b:document-bag>
          <b:parent-continuity-bag><b:parent-continuity><b:parent-application-number-text>13/123,456</b:parent-application-number-text></b:parent-continuity></b:parent-continuity-bag>
          <b:child-continuity-bag><b:child-continuity><b:child-application-number-text>15/123,456</b:child-application-number-text></b:child-continuity></b:child-continuity-bag>
          <b:foreign-priority-bag><b:foreign-priority><b:ip-office-code>EP</b:ip-office-code><b:application-number-text>EP123</b:application-number-text></b:foreign-priority></b:foreign-priority-bag>
          <b:assignment-bag><b:assignment><b:reel-number>01234</b:reel-number><b:frame-number>0567</b:frame-number></b:assignment></b:assignment-bag>
          <b:record-attorney><b:attorney-bag><b:attorney><b:last-name>XmlAttorney</b:last-name><b:registration-number>54321</b:registration-number><b:active-indicator>Y</b:active-indicator></b:attorney></b:attorney-bag></b:record-attorney>
          <b:patent-term-adjustment-data><b:adjustment-total-quantity>42</b:adjustment-total-quantity><b:patent-term-adjustment-history-data-bag><b:patent-term-adjustment-history-data><b:pta-p-t-e-code>A</b:pta-p-t-e-code></b:patent-term-adjustment-history-data></b:patent-term-adjustment-history-data-bag></b:patent-term-adjustment-data>
          <b:pgpub-document-meta-data><b:product-identifier>PGPUB</b:product-identifier><b:publication-number>US20140123456A1</b:publication-number></b:pgpub-document-meta-data>
          <b:grant-document-meta-data><b:product-identifier>GRANT</b:product-identifier><b:patent-number>9999999</b:patent-number></b:grant-document-meta-data>
        </b:patent-file-wrapper>
        """;
    }

    private static JsonRecord RequireJsonRecord(JsonRecord? record)
    {
        return record ?? throw new AssertionException("Expected a JSON record.");
    }

    private static XmlRecord RequireXmlRecord(XmlRecord? record)
    {
        return record ?? throw new AssertionException("Expected an XML record.");
    }

    private static PatentDataProvenance CreateProvenance()
    {
        PatentDataProvenance provenance = new PatentDataProvenance();
        provenance.Source = DataProvenance.Bulk;
        provenance.DatasetIdentifier = "PFW-COMPLETE";
        provenance.DatasetVersion = "2026-09-01";
        return provenance;
    }

    private static UsptoLimits CreateLimits()
    {
        UsptoLimits limits = new UsptoLimits();
        limits.MaximumResults = 10;
        limits.MaximumRawResponseBytes = 100000;
        limits.MaximumJsonDepth = 32;
        limits.MaximumXmlDepth = 32;
        limits.MaximumStringCharacters = 10000;
        return limits;
    }
}
