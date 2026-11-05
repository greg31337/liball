using System.Text;
using System.Xml;
using uspto;

namespace uspto.Tests;

public class TrademarkAssignmentXmlParserTests
{
    [Test]
    public void NamespacedAssignmentParsesTransactionPartiesPropertiesCorrespondentAndDates()
    {
        string xml = """
        <t:trademark-assignments xmlns:t="urn:uspto:trademark-assignment">
          <t:trademark-assignment source-version="2013">
            <t:assignment-record>
              <t:reel-no>006789</t:reel-no><t:frame-no>0123</t:frame-no><t:last-update-date>20240206</t:last-update-date><t:recorded-date>20240203</t:recorded-date>
              <t:received-date>2024-02-01</t:received-date><t:mailed-date>02/05/2024</t:mailed-date><t:page-count>8</t:page-count><t:conveyance-text>ASSIGNMENT OF OWNERSHIP</t:conveyance-text><t:purge-indicator>N</t:purge-indicator><t:future-record>kept</t:future-record>
            </t:assignment-record>
            <t:correspondent><t:name>Trademark Counsel</t:name><t:address-1>10 Counsel Way</t:address-1><t:city>Alexandria</t:city><t:state>VA</t:state><t:postcode>22314</t:postcode><t:country>US</t:country><t:reference>TM-100</t:reference></t:correspondent>
            <t:assignors>
              <t:assignor><t:name>Original Owner LLC</t:name><t:execution-date>20240130</t:execution-date><t:entity-type>16</t:entity-type><t:nationality>US</t:nationality><t:capacity>owner</t:capacity></t:assignor>
              <t:assignor><t:name>Second Seller Inc.</t:name><t:execution-date>2024-01-31</t:execution-date></t:assignor>
            </t:assignors>
            <t:assignees><t:assignee><t:name>New Owner Corp.</t:name><t:entity-type>03</t:entity-type><t:nationality>DE</t:nationality><t:address><t:address-1>20 Market Street</t:address-1><t:address-2>Suite 4</t:address-2><t:city>Wilmington</t:city><t:state>DE</t:state><t:postcode>19801</t:postcode><t:country-name>United States</t:country-name></t:address></t:assignee></t:assignees>
            <t:properties>
              <t:property><t:property-type>application</t:property-type><t:serial-number>90123456</t:serial-number><t:mark-identification>ACME ORBIT</t:mark-identification></t:property>
              <t:property><t:property-identifier>REG-7654321</t:property-identifier><t:property-type>registration</t:property-type><t:registration-number>7654321</t:registration-number><t:international-registration-number>1234567</t:international-registration-number><t:mark>SECOND MARK</t:mark><t:future-property>retained</t:future-property></t:property>
            </t:properties>
          </t:trademark-assignment>
        </t:trademark-assignments>
        """;
        XmlRecord generic = ReadRecords(xml).Single();
        PatentDataProvenance provenance = CreateProvenance();

        TrademarkAssignmentXmlRecord result = TrademarkAssignmentXmlParser.ParseRecord(generic, provenance);
        TrademarkAssignment assignment = result.Assignment;

        Assert.Multiple(() =>
        {
            Assert.That(result.ReelAndFrameKey, Is.EqualTo("006789/0123"));
            Assert.That(result.RecordNumber, Is.EqualTo(1L));
            Assert.That(result.SourcePath, Is.EqualTo("/t:trademark-assignments/t:trademark-assignment"));
            Assert.That(result.RawXml, Does.Contain("future-record"));
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.Bulk));
            Assert.That(result.Provenance.DatasetIdentifier, Is.EqualTo("TRTDXFAG"));
            Assert.That(result.Provenance.IsAuthoritative, Is.True);
            Assert.That(result.Provenance, Is.Not.SameAs(provenance));
            Assert.That(assignment.ReelNumber, Is.EqualTo("006789"));
            Assert.That(assignment.FrameNumber, Is.EqualTo("0123"));
            Assert.That(assignment.ReelAndFrameNumber, Is.EqualTo("006789/0123"));
            Assert.That(assignment.PageCount, Is.EqualTo(8));
            Assert.That(assignment.HasPageCount, Is.True);
            Assert.That(assignment.Conveyance, Is.EqualTo("ASSIGNMENT OF OWNERSHIP"));
            Assert.That(assignment.PurgeIndicator, Is.EqualTo("N"));
            Assert.That(assignment.UnknownFields["future-record"], Is.EqualTo("kept"));
        });

        Assert.Multiple(() =>
        {
            Assert.That(assignment.LastUpdatedDateText, Is.EqualTo("20240206"));
            Assert.That(assignment.HasLastUpdatedDate, Is.True);
            Assert.That(assignment.LastUpdatedDate, Is.EqualTo(new DateTimeOffset(2024, 2, 6, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(assignment.HasRecordedDate, Is.True);
            Assert.That(assignment.RecordedDate, Is.EqualTo(new DateTimeOffset(2024, 2, 3, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(assignment.HasReceivedDate, Is.True);
            Assert.That(assignment.HasMailedDate, Is.True);
            Assert.That(assignment.MailedDate, Is.EqualTo(new DateTimeOffset(2024, 2, 5, 0, 0, 0, TimeSpan.Zero)));
        });

        Assert.Multiple(() =>
        {
            Assert.That(assignment.Assignors, Has.Length.EqualTo(2));
            Assert.That(assignment.Assignors[0].Name, Is.EqualTo("Original Owner LLC"));
            Assert.That(assignment.Assignors[0].EntityType, Is.EqualTo("16"));
            Assert.That(assignment.Assignors[0].Nationality, Is.EqualTo("US"));
            Assert.That(assignment.Assignors[0].HasExecutionDate, Is.True);
            Assert.That(assignment.Assignors[0].UnknownFields["capacity"], Is.EqualTo("owner"));
            Assert.That(assignment.Assignors[1].Name, Is.EqualTo("Second Seller Inc."));
            Assert.That(assignment.Assignees.Single().Name, Is.EqualTo("New Owner Corp."));
            Assert.That(assignment.Assignees.Single().Address.Line1, Is.EqualTo("20 Market Street"));
            Assert.That(assignment.Assignees.Single().Address.Line2, Is.EqualTo("Suite 4"));
            Assert.That(assignment.Assignees.Single().Address.City, Is.EqualTo("Wilmington"));
            Assert.That(assignment.Assignees.Single().Address.CountryName, Is.EqualTo("United States"));
            Assert.That(assignment.Correspondent?.Name, Is.EqualTo("Trademark Counsel"));
            Assert.That(assignment.Correspondent?.Address.CountryCode, Is.EqualTo("US"));
            Assert.That(assignment.Correspondent?.UnknownFields["reference"], Is.EqualTo("TM-100"));
        });

        Assert.Multiple(() =>
        {
            Assert.That(assignment.Properties, Has.Length.EqualTo(2));
            Assert.That(assignment.Properties[0].PropertyIdentifier, Is.EqualTo("90123456"));
            Assert.That(assignment.Properties[0].SerialNumber, Is.EqualTo("90123456"));
            Assert.That(assignment.Properties[0].MarkText, Is.EqualTo("ACME ORBIT"));
            Assert.That(assignment.Properties[1].PropertyIdentifier, Is.EqualTo("REG-7654321"));
            Assert.That(assignment.Properties[1].RegistrationNumber, Is.EqualTo("7654321"));
            Assert.That(assignment.Properties[1].InternationalRegistrationNumber, Is.EqualTo("1234567"));
            Assert.That(assignment.Properties[1].MarkText, Is.EqualTo("SECOND MARK"));
            Assert.That(assignment.Properties[1].UnknownFields["future-property"], Is.EqualTo("retained"));
            Assert.That(result.RawValues["/trademark-assignment/@source-version"], Is.EqualTo("2013"));
            Assert.That(result.RawValues["/trademark-assignment/assignors/assignor[1]/name"], Is.EqualTo("Original Owner LLC"));
            Assert.That(result.RawValues["/trademark-assignment/assignors/assignor[2]/name"], Is.EqualTo("Second Seller Inc."));
            Assert.That(result.RawValues["/trademark-assignment/properties/property[2]/future-property"], Is.EqualTo("retained"));
        });
    }

    [Test]
    public void AlternateCamelCaseShapePreservesUnrecognizedLegacyDates()
    {
        string xml = """
        <TrademarkAssignment>
          <AssignmentRecord><ReelNumber>100</ReelNumber><FrameNumber>20</FrameNumber><ReelAndFrameNumber>source-value</ReelAndFrameNumber><LastUpdateDate>00000000</LastUpdateDate><RecordedDate>date pending</RecordedDate><PageCount>0</PageCount><Conveyance>MERGER</Conveyance></AssignmentRecord>
          <AssignorBag><AssignorData><PartyName>Seller</PartyName><ExecutionDate>unknown</ExecutionDate></AssignorData></AssignorBag>
          <AssigneeBag><AssigneeData><PartyName>Buyer</PartyName><Address1>One Road</Address1><CountryCode>US</CountryCode></AssigneeData></AssigneeBag>
          <PropertyBag><TrademarkProperty><TrademarkSerialNumber>88111222</TrademarkSerialNumber><TrademarkRegistrationNumber>6123456</TrademarkRegistrationNumber><WordMark>MODERN MARK</WordMark></TrademarkProperty></PropertyBag>
          <CorrespondenceAddress><CorrespondentName>Modern Counsel</CorrespondentName><PostalCode>20001</PostalCode></CorrespondenceAddress>
        </TrademarkAssignment>
        """;

        TrademarkAssignmentXmlRecord result = TrademarkAssignmentXmlParser.ParseRecord(CreateRecord(xml), CreateProvenance());

        Assert.Multiple(() =>
        {
            Assert.That(result.ReelAndFrameKey, Is.EqualTo("100/20"));
            Assert.That(result.Assignment.ReelAndFrameNumber, Is.EqualTo("source-value"));
            Assert.That(result.Assignment.PageCount, Is.Zero);
            Assert.That(result.Assignment.HasPageCount, Is.True);
            Assert.That(result.Assignment.LastUpdatedDateText, Is.EqualTo("00000000"));
            Assert.That(result.Assignment.HasLastUpdatedDate, Is.False);
            Assert.That(result.Assignment.RecordedDateText, Is.EqualTo("date pending"));
            Assert.That(result.Assignment.HasRecordedDate, Is.False);
            Assert.That(result.Assignment.Assignors.Single().ExecutionDateText, Is.EqualTo("unknown"));
            Assert.That(result.Assignment.Assignors.Single().HasExecutionDate, Is.False);
            Assert.That(result.Assignment.Assignees.Single().Address.Line1, Is.EqualTo("One Road"));
            Assert.That(result.Assignment.Properties.Single().SerialNumber, Is.EqualTo("88111222"));
            Assert.That(result.Assignment.Properties.Single().RegistrationNumber, Is.EqualTo("6123456"));
            Assert.That(result.Assignment.Properties.Single().MarkText, Is.EqualTo("MODERN MARK"));
            Assert.That(result.Assignment.Correspondent?.Name, Is.EqualTo("Modern Counsel"));
        });
    }

    [Test]
    public void FlatLegacyGenerationSupportsDirectPartiesPropertiesAndRepeatedUnknownValues()
    {
        string xml = """
        <assignment>
          <reel>200</reel><frame>30</frame><conveyance-text>CHANGE OF NAME</conveyance-text><future-value>first</future-value><future-value>second</future-value>
          <assignor><party-name>Old Name LLC</party-name><country>US</country></assignor>
          <assignee><party-name>New Name LLC</party-name><country>United States</country></assignee>
          <trademark-property><registration-number>7654321</registration-number><mark-text>RENAMED MARK</mark-text><future-property>kept</future-property></trademark-property>
        </assignment>
        """;

        TrademarkAssignmentXmlRecord result = TrademarkAssignmentXmlParser.ParseRecord(CreateRecord(xml), CreateProvenance());

        Assert.Multiple(() =>
        {
            Assert.That(result.ReelAndFrameKey, Is.EqualTo("200/30"));
            Assert.That(result.Assignment.Conveyance, Is.EqualTo("CHANGE OF NAME"));
            Assert.That(result.Assignment.Assignors.Single().Name, Is.EqualTo("Old Name LLC"));
            Assert.That(result.Assignment.Assignors.Single().Address.CountryCode, Is.EqualTo("US"));
            Assert.That(result.Assignment.Assignees.Single().Address.CountryName, Is.EqualTo("United States"));
            Assert.That(result.Assignment.Properties.Single().PropertyIdentifier, Is.EqualTo("7654321"));
            Assert.That(result.Assignment.Properties.Single().MarkText, Is.EqualTo("RENAMED MARK"));
            Assert.That(result.Assignment.Properties.Single().UnknownFields["future-property"], Is.EqualTo("kept"));
            Assert.That(result.Assignment.UnknownFields["future-value"], Is.EqualTo("first"));
            Assert.That(result.Assignment.UnknownFields["future-value[2]"], Is.EqualTo("second"));
        });
    }

    [Test]
    public void FlattenRecordsIndexesTransactionsAndRejectsDuplicateKeys()
    {
        string uniqueXml = "<trademark-assignments><trademark-assignment><assignment-record><reel-no>1</reel-no><frame-no>2</frame-no></assignment-record></trademark-assignment><trademark-assignment><assignment-record><reel-no>3</reel-no><frame-no>4</frame-no></assignment-record></trademark-assignment></trademark-assignments>";
        TrademarkAssignmentXmlBatch unique = TrademarkAssignmentXmlParser.FlattenRecords(ReadRecords(uniqueXml), CreateProvenance());
        string duplicateXml = "<trademark-assignments><trademark-assignment><assignment-record><reel-no>1</reel-no><frame-no>2</frame-no></assignment-record></trademark-assignment><trademark-assignment><assignment-record><reel-no>1</reel-no><frame-no>2</frame-no></assignment-record></trademark-assignment></trademark-assignments>";

        ParseException duplicate = Assert.Throws<ParseException>(() => TrademarkAssignmentXmlParser.FlattenRecords(ReadRecords(duplicateXml), CreateProvenance()));

        Assert.Multiple(() =>
        {
            Assert.That(unique.Count, Is.EqualTo(2));
            Assert.That(unique.RecordsByReelAndFrame.Keys, Is.EquivalentTo(new string[] { "1/2", "3/4" }));
            Assert.That(unique.RecordsByReelAndFrame["3/4"].Assignment.FrameNumber, Is.EqualTo("4"));
            Assert.That(duplicate.Message, Does.Contain("duplicate reel/frame"));
            Assert.That(duplicate.DataPath, Is.EqualTo("/trademark-assignments/trademark-assignment[2]"));
        });
    }

    [TestCase("<trademark-assignment><assignment-record><frame-no>2</frame-no></assignment-record></trademark-assignment>", "reel number")]
    [TestCase("<trademark-assignment><assignment-record><reel-no>1</reel-no></assignment-record></trademark-assignment>", "frame number")]
    [TestCase("<trademark-assignment><assignment-record><reel-no>1</reel-no><frame-no>2</frame-no><page-count>-1</page-count></assignment-record></trademark-assignment>", "page count")]
    [TestCase("<patent-assignment><assignment-record><reel-no>1</reel-no><frame-no>2</frame-no></assignment-record></patent-assignment>", "not a trademark assignment")]
    public void InvalidIdentityTypedValueAndRootReportAssignmentContext(string xml, string expectedMessage)
    {
        ParseException failure = Assert.Throws<ParseException>(() => TrademarkAssignmentXmlParser.ParseRecord(CreateRecord(xml), CreateProvenance()));

        Assert.Multiple(() =>
        {
            Assert.That(failure.Format, Is.EqualTo(DocumentFormat.Xml));
            Assert.That(failure.SourceName, Is.EqualTo("trademark assignment XML"));
            Assert.That(failure.Message, Does.Contain(expectedMessage));
            Assert.That(failure.LineNumber, Is.EqualTo(14));
        });
    }

    [Test]
    public void MissingRawInputAndDtdEntityExpansionRemainRejected()
    {
        XmlRecord missing = new XmlRecord();
        ValidationException raw = Assert.Throws<ValidationException>(() => TrademarkAssignmentXmlParser.ParseRecord(missing, CreateProvenance()));
        XmlRecord dtd = CreateRecord("<!DOCTYPE trademark-assignment [<!ENTITY hidden '2'>]><trademark-assignment><assignment-record><reel-no>1</reel-no><frame-no>&hidden;</frame-no></assignment-record></trademark-assignment>");
        XmlException dtdFailure = Assert.Catch<XmlException>(() => TrademarkAssignmentXmlParser.ParseRecord(dtd, CreateProvenance()));

        Assert.Multiple(() =>
        {
            Assert.That(raw.FieldName, Is.EqualTo("RawXml"));
            Assert.That(dtdFailure.Message, Does.Contain("DTD").IgnoreCase);
        });
    }

    [Test]
    public void TruncatedTrademarkAssignmentXmlIsRejected()
    {
        XmlException failure = Assert.Throws<XmlException>(() => TrademarkAssignmentXmlParser.ParseRecord(CreateRecord("<trademark-assignment><reel-no>1"), CreateProvenance()));

        Assert.That(failure.Message, Does.Contain("unexpected end").IgnoreCase);
    }

    private static XmlRecord[] ReadRecords(string xml)
    {
        using MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        XmlRecordReader reader = new XmlRecordReader(input, "trademark-assignments", null, "trademark-assignment", null, "tm-assign.xml", CreateLimits(), CancellationToken.None);
        return reader.ReadBatch(10);
    }

    private static XmlRecord CreateRecord(string xml)
    {
        XmlRecord result = new XmlRecord();
        result.RecordNumber = 5L;
        result.Path = "/trademark-assignments/trademark-assignment";
        result.StartLineNumber = 14;
        result.HasLineInfo = true;
        result.RawXml = xml;
        return result;
    }

    private static PatentDataProvenance CreateProvenance()
    {
        PatentDataProvenance result = new PatentDataProvenance();
        result.Source = DataProvenance.Bulk;
        result.DatasetIdentifier = "TRTDXFAG";
        result.DatasetVersion = "2013-09-05";
        return result;
    }

    private static UsptoLimits CreateLimits()
    {
        UsptoLimits result = new UsptoLimits();
        result.MaximumResults = 10;
        result.MaximumRawResponseBytes = 100000;
        result.MaximumXmlDepth = 32;
        result.MaximumStringCharacters = 10000;
        return result;
    }
}
