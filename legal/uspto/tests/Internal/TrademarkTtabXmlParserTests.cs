using System.Xml;
using uspto;

namespace uspto.Tests;

public class TrademarkTtabXmlParserTests
{
    [Test]
    public void DocumentedV1ProceedingParsesPartiesPropertiesAddressesAndEvents()
    {
        string xml = """
        <t:ttab-proceedings xmlns:t="urn:uspto:ttab" archive="daily">
          <t:version><t:version-no>1.0</t:version-no><t:version-date>20040213</t:version-date></t:version>
          <t:action-key-code>DA</t:action-key-code>
          <t:transaction-date>20240915</t:transaction-date>
          <t:proceeding-information>
            <t:proceeding-entry>
              <t:number>91234567</t:number><t:type-code>OPP</t:type-code><t:filing-date>20240102</t:filing-date><t:employee-number>12345</t:employee-number>
              <t:interlocutory-attorney-name>Alex Attorney</t:interlocutory-attorney-name><t:location-code>INT</t:location-code><t:day-in-location>12</t:day-in-location>
              <t:charge-to-location-code>FIN</t:charge-to-location-code><t:charge-to-employee-name>Case Clerk</t:charge-to-employee-name><t:status-update-date>20240830</t:status-update-date><t:status-code>2</t:status-code>
              <t:party-information>
                <t:party>
                  <t:identifier>101</t:identifier><t:role-code>P</t:role-code><t:name>Acme Brands LLC</t:name><t:orgname>Acme Holdings</t:orgname>
                  <t:property-information><t:property><t:identifier>201</t:identifier><t:serial-number>90123456</t:serial-number><t:registration-number>7654321</t:registration-number><t:mark-text>ACME ORBIT</t:mark-text><t:future-property>kept</t:future-property></t:property></t:property-information>
                  <t:address-information><t:proceeding-address><t:identifier>301</t:identifier><t:type-code>C</t:type-code><t:name>Primary Counsel</t:name><t:orgname>Law Firm LLP</t:orgname><t:address-1>10 Board Walk</t:address-1><t:city>Alexandria</t:city><t:state>VA</t:state><t:country>US</t:country><t:postcode>22314</t:postcode><t:email>counsel@example.test</t:email></t:proceeding-address></t:address-information>
                  <t:future-party>retained</t:future-party>
                </t:party>
                <t:party><t:identifier>102</t:identifier><t:role-code>D</t:role-code><t:name>Orbit Goods Inc.</t:name></t:party>
              </t:party-information>
              <t:prosecution-history>
                <t:prosecution-entry><t:identifier>1</t:identifier><t:code>1000</t:code><t:type-code>T</t:type-code><t:due-date>20241001</t:due-date><t:date>20240901</t:date><t:history-text>NOTICE OF OPPOSITION FILED</t:history-text><t:future-event>visible</t:future-event></t:prosecution-entry>
                <t:prosecution-entry><t:identifier>2</t:identifier><t:code>102</t:code><t:type-code>N</t:type-code><t:due-date>date pending</t:due-date><t:date>2024-09-10</t:date><t:history-text>AMENDED ANSWER</t:history-text></t:prosecution-entry>
              </t:prosecution-history>
              <t:future-proceeding><t:value>raw</t:value></t:future-proceeding>
            </t:proceeding-entry>
          </t:proceeding-information>
        </t:ttab-proceedings>
        """;
        PatentDataProvenance provenance = CreateProvenance();
        TrademarkTtabXmlRecord result = TrademarkTtabXmlParser.ParseRecord(CreateRecord(xml, 1L), provenance);
        TrademarkTtabProceeding proceeding = result.Proceeding!;

        Assert.Multiple(() =>
        {
            Assert.That(result.IsTyped, Is.True);
            Assert.That(result.RootName, Is.EqualTo("ttab-proceedings"));
            Assert.That(result.SchemaVersion, Is.EqualTo("1.0"));
            Assert.That(result.ActionKeyCode, Is.EqualTo("DA"));
            Assert.That(result.TransactionDateText, Is.EqualTo("20240915"));
            Assert.That(result.HasTransactionDate, Is.True);
            Assert.That(result.TransactionDate, Is.EqualTo(new DateTimeOffset(2024, 9, 15, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.Bulk));
            Assert.That(result.Provenance.DatasetIdentifier, Is.EqualTo("TTABTDXF"));
            Assert.That(result.Provenance.IsAuthoritative, Is.True);
            Assert.That(result.Provenance, Is.Not.SameAs(provenance));
            Assert.That(proceeding.Number, Is.EqualTo("91234567"));
            Assert.That(proceeding.TypeCode, Is.EqualTo("OPP"));
            Assert.That(proceeding.StatusCode, Is.EqualTo("2"));
            Assert.That(proceeding.HasFilingDate, Is.True);
            Assert.That(proceeding.HasStatusUpdateDate, Is.True);
            Assert.That(proceeding.EmployeeNumber, Is.EqualTo("12345"));
            Assert.That(proceeding.InterlocutoryAttorneyName, Is.EqualTo("Alex Attorney"));
            Assert.That(proceeding.LocationCode, Is.EqualTo("INT"));
            Assert.That(proceeding.DayInLocation, Is.EqualTo("12"));
            Assert.That(proceeding.ChargeToLocationCode, Is.EqualTo("FIN"));
            Assert.That(proceeding.ChargeToEmployeeName, Is.EqualTo("Case Clerk"));
            Assert.That(proceeding.UnknownFields["future-proceeding"], Is.EqualTo("raw"));
        });

        Assert.Multiple(() =>
        {
            Assert.That(proceeding.Parties, Has.Length.EqualTo(2));
            Assert.That(proceeding.Parties[0].Identifier, Is.EqualTo("101"));
            Assert.That(proceeding.Parties[0].RoleCode, Is.EqualTo("P"));
            Assert.That(proceeding.Parties[0].Name, Is.EqualTo("Acme Brands LLC"));
            Assert.That(proceeding.Parties[0].OrganizationName, Is.EqualTo("Acme Holdings"));
            Assert.That(proceeding.Parties[0].UnknownFields["future-party"], Is.EqualTo("retained"));
            Assert.That(proceeding.Parties[1].RoleCode, Is.EqualTo("D"));
            Assert.That(proceeding.Parties[0].Properties.Single().SerialNumber, Is.EqualTo("90123456"));
            Assert.That(proceeding.Parties[0].Properties.Single().RegistrationNumber, Is.EqualTo("7654321"));
            Assert.That(proceeding.Parties[0].Properties.Single().MarkText, Is.EqualTo("ACME ORBIT"));
            Assert.That(proceeding.Parties[0].Properties.Single().UnknownFields["future-property"], Is.EqualTo("kept"));
            Assert.That(proceeding.Parties[0].Addresses.Single().Name, Is.EqualTo("Primary Counsel"));
            Assert.That(proceeding.Parties[0].Addresses.Single().OrganizationName, Is.EqualTo("Law Firm LLP"));
            Assert.That(proceeding.Parties[0].Addresses.Single().Address.Line1, Is.EqualTo("10 Board Walk"));
            Assert.That(proceeding.Parties[0].Addresses.Single().Address.CountryCode, Is.EqualTo("US"));
            Assert.That(proceeding.Parties[0].Addresses.Single().UnknownFields["email"], Is.EqualTo("counsel@example.test"));
        });

        Assert.Multiple(() =>
        {
            Assert.That(proceeding.Events, Has.Length.EqualTo(2));
            Assert.That(proceeding.Events[0].Identifier, Is.EqualTo("1"));
            Assert.That(proceeding.Events[0].Code, Is.EqualTo("1000"));
            Assert.That(proceeding.Events[0].TypeCode, Is.EqualTo("T"));
            Assert.That(proceeding.Events[0].Text, Is.EqualTo("NOTICE OF OPPOSITION FILED"));
            Assert.That(proceeding.Events[0].HasDueDate, Is.True);
            Assert.That(proceeding.Events[0].HasEventDate, Is.True);
            Assert.That(proceeding.Events[0].UnknownFields["future-event"], Is.EqualTo("visible"));
            Assert.That(proceeding.Events[1].DueDateText, Is.EqualTo("date pending"));
            Assert.That(proceeding.Events[1].HasDueDate, Is.False);
            Assert.That(proceeding.Events[1].HasEventDate, Is.True);
            Assert.That(result.RawValues["/ttab-proceedings/@archive"], Is.EqualTo("daily"));
            Assert.That(result.RawValues["/ttab-proceedings/proceeding-information/proceeding-entry/party-information/party[1]/name"], Is.EqualTo("Acme Brands LLC"));
            Assert.That(result.RawValues["/ttab-proceedings/proceeding-information/proceeding-entry/prosecution-history/prosecution-entry[2]/history-text"], Is.EqualTo("AMENDED ANSWER"));
        });
    }

    [Test]
    public void ModernProceedingParsesDecisionsAndDocumentReferences()
    {
        string xml = """
        <b:Proceeding xmlns:b="urn:future:board" schemaVersion="2.0">
          <b:ProceedingNumber>92076543</b:ProceedingNumber><b:ProceedingTypeCode>Cancellation</b:ProceedingTypeCode><b:FilingDate>date withheld</b:FilingDate><b:StatusCode>Terminated</b:StatusCode><b:StatusDate>2025-02-03</b:StatusDate>
          <b:PartyBag><b:ProceedingParty><b:PartyIdentifier>P-9</b:PartyIdentifier><b:Role>Petitioner</b:Role><b:PartyName>Modern Petitioner</b:PartyName><b:OrganizationName>Petitioner Group</b:OrganizationName><b:PropertyBag><b:TrademarkProperty><b:PropertyIdentifier>M-1</b:PropertyIdentifier><b:SerialNumber>88111222</b:SerialNumber><b:MarkText>MODERN MARK</b:MarkText></b:TrademarkProperty></b:PropertyBag><b:AddressBag><b:CorrespondenceAddress><b:AddressIdentifier>A-1</b:AddressIdentifier><b:AddressTypeCode>C</b:AddressTypeCode><b:ContactName>Modern Counsel</b:ContactName><b:AddressLineOne>1 Legal Plaza</b:AddressLineOne><b:AddressLineTwo>Suite 5</b:AddressLineTwo><b:City>Washington</b:City><b:Region>DC</b:Region><b:PostalCode>20001</b:PostalCode><b:CountryCode>US</b:CountryCode></b:CorrespondenceAddress></b:AddressBag></b:ProceedingParty></b:PartyBag>
          <b:EventHistory><b:HistoryEntry><b:EventIdentifier>E-1</b:EventIdentifier><b:EventCode>FILE</b:EventCode><b:EventTypeCode>incoming</b:EventTypeCode><b:EventDate>20250201</b:EventDate><b:Description>Motion filed</b:Description><b:DocumentReferences><b:DocumentReference><b:DocumentIdentifier>DOC-1</b:DocumentIdentifier><b:DocumentNumber>12</b:DocumentNumber><b:DocumentName>motion.pdf</b:DocumentName><b:DocumentTitle>Motion for judgment</b:DocumentTitle><b:DocumentCategory>Motion</b:DocumentCategory><b:MimeType>application/pdf</b:MimeType><b:DocumentUrl>https://example.test/ttab/doc-1.pdf</b:DocumentUrl><b:DocumentDate>20250201</b:DocumentDate></b:DocumentReference></b:DocumentReferences></b:HistoryEntry></b:EventHistory>
          <b:DecisionHistory><b:BoardDecision><b:DecisionIdentifier>DEC-1</b:DecisionIdentifier><b:DecisionTypeCode>Final</b:DecisionTypeCode><b:Disposition>Granted</b:Disposition><b:DecisionDate>20250304</b:DecisionDate><b:DecisionText>Petition granted.</b:DecisionText><b:Attachments><b:Attachment><b:DocumentId>DOC-2</b:DocumentId><b:SequenceNumber>13</b:SequenceNumber><b:FileName>decision.pdf</b:FileName><b:Title>Final decision</b:Title><b:ContentType>application/pdf</b:ContentType><b:DownloadUri>https://example.test/ttab/doc-2.pdf</b:DownloadUri><b:FilingDate>2025-03-04</b:FilingDate><b:checksum>abc</b:checksum></b:Attachment></b:Attachments><b:panel>Three judges</b:panel></b:BoardDecision></b:DecisionHistory>
          <b:DocketEntries><b:DocketEntry><b:DocumentIdentifier>DOC-3</b:DocumentIdentifier><b:EntryNumber>14</b:EntryNumber><b:Name>notice.pdf</b:Name><b:Category>Notice</b:Category><b:Uri>https://example.test/ttab/doc-3.pdf</b:Uri><b:Date>unknown</b:Date></b:DocketEntry></b:DocketEntries>
        </b:Proceeding>
        """;

        TrademarkTtabXmlRecord result = TrademarkTtabXmlParser.ParseRecord(CreateRecord(xml, 2L), CreateProvenance());
        TrademarkTtabProceeding proceeding = result.Proceeding!;
        TrademarkTtabDocumentReference eventDocument = proceeding.Events.Single().Documents.Single();
        TrademarkTtabDecision decision = proceeding.Decisions.Single();
        TrademarkTtabDocumentReference decisionDocument = decision.Documents.Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsTyped, Is.True);
            Assert.That(result.SchemaVersion, Is.EqualTo("2.0"));
            Assert.That(proceeding.Number, Is.EqualTo("92076543"));
            Assert.That(proceeding.TypeCode, Is.EqualTo("Cancellation"));
            Assert.That(proceeding.FilingDateText, Is.EqualTo("date withheld"));
            Assert.That(proceeding.HasFilingDate, Is.False);
            Assert.That(proceeding.HasStatusUpdateDate, Is.True);
            Assert.That(proceeding.Parties.Single().Properties.Single().Identifier, Is.EqualTo("M-1"));
            Assert.That(proceeding.Parties.Single().Addresses.Single().Address.Line2, Is.EqualTo("Suite 5"));
            Assert.That(proceeding.Events.Single().Code, Is.EqualTo("FILE"));
            Assert.That(eventDocument.Identifier, Is.EqualTo("DOC-1"));
            Assert.That(eventDocument.EntryNumber, Is.EqualTo(12));
            Assert.That(eventDocument.HasEntryNumber, Is.True);
            Assert.That(eventDocument.Title, Is.EqualTo("Motion for judgment"));
            Assert.That(eventDocument.MediaType, Is.EqualTo("application/pdf"));
            Assert.That(eventDocument.Uri, Is.EqualTo(new Uri("https://example.test/ttab/doc-1.pdf")));
            Assert.That(eventDocument.HasFilingDate, Is.True);
        });

        Assert.Multiple(() =>
        {
            Assert.That(decision.Identifier, Is.EqualTo("DEC-1"));
            Assert.That(decision.TypeCode, Is.EqualTo("Final"));
            Assert.That(decision.Outcome, Is.EqualTo("Granted"));
            Assert.That(decision.Text, Is.EqualTo("Petition granted."));
            Assert.That(decision.HasIssueDate, Is.True);
            Assert.That(decision.UnknownFields["panel"], Is.EqualTo("Three judges"));
            Assert.That(decisionDocument.Identifier, Is.EqualTo("DOC-2"));
            Assert.That(decisionDocument.EntryNumber, Is.EqualTo(13));
            Assert.That(decisionDocument.Name, Is.EqualTo("decision.pdf"));
            Assert.That(decisionDocument.UnknownFields["checksum"], Is.EqualTo("abc"));
            Assert.That(proceeding.Documents.Single().Identifier, Is.EqualTo("DOC-3"));
            Assert.That(proceeding.Documents.Single().FilingDateText, Is.EqualTo("unknown"));
            Assert.That(proceeding.Documents.Single().HasFilingDate, Is.False);
        });
    }

    [Test]
    public void CompactCaseGenerationSupportsDirectDocumentsAndRepeatedUnknownValues()
    {
        string xml = """
        <case schema-version="compact-1" transaction-date="20250305">
          <case-number>91239999</case-number><type>Opposition</type><future-value>first</future-value><future-value>second</future-value>
          <event><event-code>ANSWER</event-code><document-id>DOC-E</document-id><document-number>not-numbered</document-number><document-name>answer.pdf</document-name><document-url>relative/answer.pdf</document-url></event>
          <decision><decision-identifier>DEC-C</decision-identifier><result>Denied</result><document-id>DOC-D</document-id><document-number>15</document-number><document-name>order.pdf</document-name></decision>
        </case>
        """;

        TrademarkTtabXmlRecord result = TrademarkTtabXmlParser.ParseRecord(CreateRecord(xml, 6L), CreateProvenance());
        TrademarkTtabProceeding proceeding = result.Proceeding!;
        TrademarkTtabDocumentReference eventDocument = proceeding.Events.Single().Documents.Single();
        TrademarkTtabDocumentReference decisionDocument = proceeding.Decisions.Single().Documents.Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsTyped, Is.True);
            Assert.That(result.SchemaVersion, Is.EqualTo("compact-1"));
            Assert.That(result.TransactionDateText, Is.EqualTo("20250305"));
            Assert.That(result.HasTransactionDate, Is.True);
            Assert.That(proceeding.Number, Is.EqualTo("91239999"));
            Assert.That(proceeding.TypeCode, Is.EqualTo("Opposition"));
            Assert.That(proceeding.UnknownFields["future-value"], Is.EqualTo("first"));
            Assert.That(proceeding.UnknownFields["future-value[2]"], Is.EqualTo("second"));
            Assert.That(eventDocument.Identifier, Is.EqualTo("DOC-E"));
            Assert.That(eventDocument.HasEntryNumber, Is.False);
            Assert.That(eventDocument.UriText, Is.EqualTo("relative/answer.pdf"));
            Assert.That(eventDocument.Uri, Is.Null);
            Assert.That(decisionDocument.Identifier, Is.EqualTo("DOC-D"));
            Assert.That(decisionDocument.EntryNumber, Is.EqualTo(15));
            Assert.That(decisionDocument.HasEntryNumber, Is.True);
        });
    }

    [TestCase("<future-board generation='9'><matter><matter-id>X-1</matter-id><new-value>kept</new-value></matter></future-board>")]
    [TestCase("<ttab-proceedings><version><version-no>next</version-no></version><proceeding-information><data-available-code>N</data-available-code></proceeding-information></ttab-proceedings>")]
    public void UnknownOrEmptyGenerationFallsBackToRawRecord(string xml)
    {
        TrademarkTtabXmlRecord result = TrademarkTtabXmlParser.ParseRecord(CreateRecord(xml, 3L), CreateProvenance());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsTyped, Is.False);
            Assert.That(result.Proceeding, Is.Null);
            Assert.That(result.RawXml, Is.EqualTo(xml));
            Assert.That(result.RawValues, Is.Not.Empty);
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.Bulk));
            Assert.That(result.Provenance.IsAuthoritative, Is.True);
        });
    }

    [Test]
    public void FlattenRecordsIndexesTypedRecordsAndCountsGenericFallbacks()
    {
        XmlRecord[] records = new[]
        {
            CreateRecord("<proceeding-entry><number>91230001</number></proceeding-entry>", 1L),
            CreateRecord("<future-matter><id>raw</id></future-matter>", 2L),
            CreateRecord("<Proceeding><ProceedingNumber>92000002</ProceedingNumber></Proceeding>", 3L)
        };

        TrademarkTtabXmlBatch result = TrademarkTtabXmlParser.FlattenRecords(records, CreateProvenance());

        Assert.Multiple(() =>
        {
            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result.TypedCount, Is.EqualTo(2));
            Assert.That(result.GenericCount, Is.EqualTo(1));
            Assert.That(result.RecordsByProceedingNumber.Keys, Is.EquivalentTo(new[] { "91230001", "92000002" }));
            Assert.That(result.RecordsByProceedingNumber["92000002"].RecordNumber, Is.EqualTo(3L));
        });
    }

    [Test]
    public void FlattenRecordsRejectsDuplicateProceedingNumbers()
    {
        XmlRecord[] records = new[]
        {
            CreateRecord("<proceeding-entry><number>91230001</number></proceeding-entry>", 1L),
            CreateRecord("<Proceeding><ProceedingNumber>91230001</ProceedingNumber></Proceeding>", 2L)
        };

        ParseException failure = Assert.Throws<ParseException>(() => TrademarkTtabXmlParser.FlattenRecords(records, CreateProvenance()));

        Assert.Multiple(() =>
        {
            Assert.That(failure.Message, Does.Contain("duplicate proceeding number"));
            Assert.That(failure.SourceName, Is.EqualTo("trademark TTAB XML"));
            Assert.That(failure.LineNumber, Is.EqualTo(12));
        });
    }

    [Test]
    public void RecognizedProceedingWithoutNumberReportsTypedContext()
    {
        ParseException failure = Assert.Throws<ParseException>(() => TrademarkTtabXmlParser.ParseRecord(CreateRecord("<proceeding-entry><type-code>OPP</type-code></proceeding-entry>", 4L), CreateProvenance()));

        Assert.Multiple(() =>
        {
            Assert.That(failure.Message, Does.Contain("proceeding number is missing"));
            Assert.That(failure.Format, Is.EqualTo(DocumentFormat.Xml));
            Assert.That(failure.SourceName, Is.EqualTo("trademark TTAB XML"));
            Assert.That(failure.DataPath, Does.Contain("/number"));
            Assert.That(failure.HasLineNumber, Is.True);
        });
    }

    [Test]
    public void MissingRawInputAndDtdEntityExpansionRemainRejected()
    {
        XmlRecord missing = new XmlRecord();
        ValidationException raw = Assert.Throws<ValidationException>(() => TrademarkTtabXmlParser.ParseRecord(missing, CreateProvenance()));
        XmlRecord dtd = CreateRecord("<!DOCTYPE proceeding-entry [<!ENTITY hidden '91234567'>]><proceeding-entry><number>&hidden;</number></proceeding-entry>", 5L);
        XmlException dtdFailure = Assert.Catch<XmlException>(() => TrademarkTtabXmlParser.ParseRecord(dtd, CreateProvenance()));

        Assert.Multiple(() =>
        {
            Assert.That(raw.FieldName, Is.EqualTo("RawXml"));
            Assert.That(dtdFailure.Message, Does.Contain("DTD").IgnoreCase);
        });
    }

    [Test]
    public void TruncatedTrademarkTtabXmlIsRejected()
    {
        XmlException failure = Assert.Throws<XmlException>(() => TrademarkTtabXmlParser.ParseRecord(CreateRecord("<proceeding><number>91234567", 7L), CreateProvenance()));

        Assert.That(failure.Message, Does.Contain("unexpected end").IgnoreCase);
    }

    private static XmlRecord CreateRecord(string xml, long number)
    {
        XmlRecord result = new XmlRecord();
        result.RecordNumber = number;
        result.Path = "/ttab-records/record[" + number + "]";
        result.StartLineNumber = 12;
        result.HasLineInfo = true;
        result.RawXml = xml;
        return result;
    }

    private static PatentDataProvenance CreateProvenance()
    {
        PatentDataProvenance result = new PatentDataProvenance();
        result.Source = DataProvenance.Bulk;
        result.DatasetIdentifier = "TTABTDXF";
        result.DatasetVersion = "2024-09-15";
        result.SourceUri = new Uri("https://example.test/ttab.zip");
        return result;
    }
}
