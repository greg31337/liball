using System.Text;
using System.Xml;
using uspto;

namespace uspto.Tests;

public class PatentAssignmentXmlParserTests
{
    [Test]
    public void HistoricalNamespacedRecordParsesTransactionPartiesPropertiesAndRawValues()
    {
        string assignmentXml = """
          <a:patent-assignment source-version="2026.1">
            <a:assignment-record>
              <a:reel-no>012345</a:reel-no><a:frame-no>0678</a:frame-no>
              <a:recorded-date>20240203</a:recorded-date><a:received-date>02/01/2024</a:received-date>
              <a:mailed-date>2024-02-05</a:mailed-date><a:page-count>7</a:page-count>
              <a:conveyance-text>ASSIGNMENT OF ASSIGNORS INTEREST</a:conveyance-text>
              <a:image-available-indicator>Y</a:image-available-indicator>
              <a:assignment-document-location-uri>https://example.test/assignment.pdf</a:assignment-document-location-uri>
              <a:purge-indicator>N</a:purge-indicator>
            </a:assignment-record>
            <a:correspondent><a:name>Patent Counsel</a:name><a:address-1>One Main St.</a:address-1><a:city>Alexandria</a:city><a:state>VA</a:state><a:postcode>22314</a:postcode><a:country>US</a:country></a:correspondent>
            <a:assignors>
              <a:assignor><a:name>Ada Inventor</a:name><a:execution-date>20240129</a:execution-date><a:capacity>inventor</a:capacity></a:assignor>
              <a:assignor><a:name>Grace Inventor</a:name><a:execution-date>2024-01-30</a:execution-date></a:assignor>
            </a:assignors>
            <a:assignees><a:assignee><a:name>Example Owner LLC</a:name><a:address><a:address-1>Two Market St.</a:address-1><a:city>San Francisco</a:city><a:state>CA</a:state><a:postcode>94105</a:postcode><a:country-name>United States</a:country-name></a:address></a:assignee></a:assignees>
            <a:domestic-representative><a:name>Domestic Agent</a:name><a:address-1>Three King St.</a:address-1></a:domestic-representative>
            <a:properties>
              <a:property><a:document-id><a:country>US</a:country><a:doc-number>18123456</a:doc-number><a:kind>A1</a:kind><a:date>20240104</a:date></a:document-id><a:title>First invention</a:title></a:property>
              <a:property><a:property-identifier>PAT-999</a:property-identifier><a:property-type>patent</a:property-type><a:patent-number>9999999</a:patent-number><a:title>Second invention</a:title><a:future-property>kept</a:future-property></a:property>
            </a:properties>
          </a:patent-assignment>
        """;
        XmlRecord generic = ReadRecords("<a:patent-assignments xmlns:a=\"urn:uspto:assignment\">" + assignmentXml + "</a:patent-assignments>").Single();

        PatentDataProvenance provenance = CreateProvenance();
        PatentAssignmentFlatRecord result = PatentAssignmentXmlParser.ParseRecord(generic, provenance);
        PatentAssignment assignment = result.Transaction;

        Assert.Multiple(() =>
        {
            Assert.That(result.ReelAndFrameKey, Is.EqualTo("012345/0678"));
            Assert.That(result.RecordNumber, Is.EqualTo(1L));
            Assert.That(result.SourcePath, Is.EqualTo("/a:patent-assignments/a:patent-assignment"));
            Assert.That(result.RawXml, Does.Contain("purge-indicator"));
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.Bulk));
            Assert.That(result.Provenance, Is.Not.SameAs(provenance));
            Assert.That(assignment.ReelNumber, Is.EqualTo("012345"));
            Assert.That(assignment.FrameNumber, Is.EqualTo("0678"));
            Assert.That(assignment.ReelAndFrameNumber, Is.EqualTo("012345/0678"));
            Assert.That(assignment.Conveyance, Is.EqualTo("ASSIGNMENT OF ASSIGNORS INTEREST"));
            Assert.That(assignment.PageCount, Is.EqualTo(7));
            Assert.That(assignment.HasPageCount, Is.True);
            Assert.That(assignment.IsImageAvailable, Is.True);
            Assert.That(assignment.HasIsImageAvailable, Is.True);
            Assert.That(assignment.DocumentUri?.AbsoluteUri, Is.EqualTo("https://example.test/assignment.pdf"));
            Assert.That(assignment.UnknownFields["purge-indicator"], Is.EqualTo("N"));
        });

        Assert.Multiple(() =>
        {
            Assert.That(assignment.HasReceivedDate, Is.True);
            Assert.That(assignment.ReceivedDate, Is.EqualTo(new DateTimeOffset(2024, 2, 1, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(assignment.HasRecordedDate, Is.True);
            Assert.That(assignment.RecordedDate, Is.EqualTo(new DateTimeOffset(2024, 2, 3, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(assignment.HasMailedDate, Is.True);
            Assert.That(assignment.MailedDate, Is.EqualTo(new DateTimeOffset(2024, 2, 5, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(assignment.Assignors, Has.Length.EqualTo(2));
            Assert.That(assignment.Assignors[0].Name, Is.EqualTo("Ada Inventor"));
            Assert.That(assignment.Assignors[0].HasExecutionDate, Is.True);
            Assert.That(assignment.Assignors[0].UnknownFields["capacity"], Is.EqualTo("inventor"));
            Assert.That(assignment.Assignors[1].Name, Is.EqualTo("Grace Inventor"));
            Assert.That(assignment.Assignees.Single().Name, Is.EqualTo("Example Owner LLC"));
            Assert.That(assignment.Assignees.Single().Address.City, Is.EqualTo("San Francisco"));
            Assert.That(assignment.Assignees.Single().Address.GeographicRegionCode, Is.EqualTo("CA"));
            Assert.That(assignment.Assignees.Single().Address.CountryName, Is.EqualTo("United States"));
            Assert.That(assignment.Correspondents.Single().Name, Is.EqualTo("Patent Counsel"));
            Assert.That(assignment.Correspondents.Single().Address.CountryCode, Is.EqualTo("US"));
            Assert.That(assignment.DomesticRepresentative?.Name, Is.EqualTo("Domestic Agent"));
        });

        Assert.Multiple(() =>
        {
            Assert.That(assignment.Properties, Has.Length.EqualTo(2));
            Assert.That(assignment.Properties[0].PropertyIdentifier, Is.EqualTo("18123456"));
            Assert.That(assignment.Properties[0].PropertyType, Is.EqualTo("A1"));
            Assert.That(assignment.Properties[0].Title, Is.EqualTo("First invention"));
            Assert.That(assignment.Properties[0].UnknownFields["document-id.country"], Is.EqualTo("US"));
            Assert.That(assignment.Properties[0].UnknownFields["document-id.date"], Is.EqualTo("20240104"));
            Assert.That(assignment.Properties[1].PatentNumber, Is.EqualTo("9999999"));
            Assert.That(assignment.Properties[1].UnknownFields["future-property"], Is.EqualTo("kept"));
            Assert.That(result.RawValues["/patent-assignment/@source-version"], Is.EqualTo("2026.1"));
            Assert.That(result.RawValues["/patent-assignment/assignors/assignor[1]/name"], Is.EqualTo("Ada Inventor"));
            Assert.That(result.RawValues["/patent-assignment/assignors/assignor[2]/name"], Is.EqualTo("Grace Inventor"));
            Assert.That(result.RawValues["/patent-assignment/assignment-record/purge-indicator"], Is.EqualTo("N"));
        });
    }

    [Test]
    public void FlattenRecordsIndexesCompleteTransactionsByExactReelAndFrame()
    {
        string xml = """
        <PatentAssignments>
          <PatentAssignment><AssignmentRecord><ReelNumber>000001</ReelNumber><FrameNumber>0001</FrameNumber><ReelAndFrameNumber>source-combined</ReelAndFrameNumber><PageTotalQuantity>1</PageTotalQuantity></AssignmentRecord><AssignorBag><Assignor><AssignorNameText>First Seller</AssignorNameText></Assignor></AssignorBag><PropertyBag><Property><ApplicationNumberText>18111111</ApplicationNumberText></Property></PropertyBag></PatentAssignment>
          <PatentAssignment><AssignmentRecord><ReelNumber>000002</ReelNumber><FrameNumber>0002</FrameNumber><ImageAvailableStatusCode>false</ImageAvailableStatusCode></AssignmentRecord><AssigneeBag><Assignee><AssigneeNameText>Second Owner</AssigneeNameText></Assignee></AssigneeBag><PropertyBag><PatentProperty><PublicationNumber>US20240000001A1</PublicationNumber></PatentProperty></PropertyBag></PatentAssignment>
        </PatentAssignments>
        """;
        XmlRecord[] records = ReadRecords(xml, "PatentAssignments", "PatentAssignment");

        PatentAssignmentXmlBatch result = PatentAssignmentXmlParser.FlattenRecords(records, CreateProvenance());

        Assert.Multiple(() =>
        {
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.Records, Has.Length.EqualTo(2));
            Assert.That(result.RecordsByReelAndFrame.Keys, Is.EquivalentTo(new string[] { "000001/0001", "000002/0002" }));
            Assert.That(result.RecordsByReelAndFrame["000001/0001"].Transaction.ReelAndFrameNumber, Is.EqualTo("source-combined"));
            Assert.That(result.RecordsByReelAndFrame["000001/0001"].Transaction.Assignors.Single().Name, Is.EqualTo("First Seller"));
            Assert.That(result.RecordsByReelAndFrame["000001/0001"].Transaction.Properties.Single().ApplicationNumber, Is.EqualTo("18111111"));
            Assert.That(result.RecordsByReelAndFrame["000002/0002"].Transaction.Assignees.Single().Name, Is.EqualTo("Second Owner"));
            Assert.That(result.RecordsByReelAndFrame["000002/0002"].Transaction.Properties.Single().PublicationNumber, Is.EqualTo("US20240000001A1"));
            Assert.That(result.RecordsByReelAndFrame["000002/0002"].Transaction.HasIsImageAvailable, Is.True);
            Assert.That(result.RecordsByReelAndFrame["000002/0002"].Transaction.IsImageAvailable, Is.False);
        });
    }

    [Test]
    public void FlatLegacyGenerationSupportsDirectCollectionsAndRepeatedUnknownValues()
    {
        string xml = """
        <assignment>
          <reel>77</reel><frame>8</frame><image-available-status-code>0</image-available-status-code><future-value>first</future-value><future-value>second</future-value>
          <assignor><assignor-name>Flat Seller</assignor-name></assignor>
          <assignee><assignee-name>Flat Buyer</assignee-name><country>United States</country></assignee>
          <patent-property><application-number-text>18123456</application-number-text><invention-title>Flat invention</invention-title></patent-property>
        </assignment>
        """;

        PatentAssignmentFlatRecord result = PatentAssignmentXmlParser.ParseRecord(CreateRecord(xml), CreateProvenance());

        Assert.Multiple(() =>
        {
            Assert.That(result.ReelAndFrameKey, Is.EqualTo("77/8"));
            Assert.That(result.Transaction.HasIsImageAvailable, Is.True);
            Assert.That(result.Transaction.IsImageAvailable, Is.False);
            Assert.That(result.Transaction.Assignors.Single().Name, Is.EqualTo("Flat Seller"));
            Assert.That(result.Transaction.Assignees.Single().Name, Is.EqualTo("Flat Buyer"));
            Assert.That(result.Transaction.Assignees.Single().Address.CountryName, Is.EqualTo("United States"));
            Assert.That(result.Transaction.Properties.Single().ApplicationNumber, Is.EqualTo("18123456"));
            Assert.That(result.Transaction.Properties.Single().Title, Is.EqualTo("Flat invention"));
            Assert.That(result.Transaction.UnknownFields["future-value"], Is.EqualTo("first"));
            Assert.That(result.Transaction.UnknownFields["future-value[2]"], Is.EqualTo("second"));
        });
    }

    [Test]
    public void DuplicateReelFrameKeysAreRejectedInsteadOfOverwritingTransactions()
    {
        string xml = "<patent-assignments><patent-assignment><assignment-record><reel-no>1</reel-no><frame-no>2</frame-no></assignment-record></patent-assignment><patent-assignment><assignment-record><reel-no>1</reel-no><frame-no>2</frame-no></assignment-record></patent-assignment></patent-assignments>";
        XmlRecord[] records = ReadRecords(xml);

        ParseException failure = Assert.Throws<ParseException>(() => PatentAssignmentXmlParser.FlattenRecords(records, CreateProvenance()));

        Assert.Multiple(() =>
        {
            Assert.That(failure.Format, Is.EqualTo(DocumentFormat.Xml));
            Assert.That(failure.Message, Does.Contain("duplicate reel/frame"));
            Assert.That(failure.DataPath, Is.EqualTo("/patent-assignments/patent-assignment[2]"));
        });
    }

    [TestCase("<patent-assignment><assignment-record><frame-no>2</frame-no></assignment-record></patent-assignment>", "reel number")]
    [TestCase("<patent-assignment><assignment-record><reel-no>1</reel-no></assignment-record></patent-assignment>", "frame number")]
    [TestCase("<patent-assignment><assignment-record><reel-no>1</reel-no><frame-no>2</frame-no><page-count>-1</page-count></assignment-record></patent-assignment>", "page count")]
    [TestCase("<patent-assignment><assignment-record><reel-no>1</reel-no><frame-no>2</frame-no><image-available-indicator>perhaps</image-available-indicator></assignment-record></patent-assignment>", "image-availability")]
    [TestCase("<patent-assignment><assignment-record><reel-no>1</reel-no><frame-no>2</frame-no><assignment-document-location-uri>relative.pdf</assignment-document-location-uri></assignment-record></patent-assignment>", "document location")]
    [TestCase("<patent-assignment><assignment-record><reel-no>1</reel-no><frame-no>2</frame-no><recorded-date>2024-99-03</recorded-date></assignment-record></patent-assignment>", "date")]
    public void InvalidRequiredIdentityAndTypedValuesReportXmlParseFailures(string xml, string expectedMessage)
    {
        XmlRecord record = CreateRecord(xml);

        ParseException failure = Assert.Throws<ParseException>(() => PatentAssignmentXmlParser.ParseRecord(record, CreateProvenance()));

        Assert.Multiple(() =>
        {
            Assert.That(failure.Format, Is.EqualTo(DocumentFormat.Xml));
            Assert.That(failure.Message, Does.Contain(expectedMessage));
            Assert.That(failure.HasLineNumber, Is.True);
            Assert.That(failure.LineNumber, Is.EqualTo(17));
        });
    }

    [Test]
    public void WrongRootRawSourceValidationAndDtdRemainRejected()
    {
        ParseException root = Assert.Throws<ParseException>(() => PatentAssignmentXmlParser.ParseRecord(CreateRecord("<trademark-assignment/>"), CreateProvenance()));
        XmlRecord missing = new XmlRecord();
        ValidationException raw = Assert.Throws<ValidationException>(() => PatentAssignmentXmlParser.ParseRecord(missing, CreateProvenance()));
        XmlRecord dtd = CreateRecord("<!DOCTYPE patent-assignment [<!ENTITY hidden 'value'>]><patent-assignment><assignment-record><reel-no>1</reel-no><frame-no>&hidden;</frame-no></assignment-record></patent-assignment>");
        XmlException dtdFailure = Assert.Catch<XmlException>(() => PatentAssignmentXmlParser.ParseRecord(dtd, CreateProvenance()));

        Assert.Multiple(() =>
        {
            Assert.That(root.DataPath, Is.EqualTo("/patent-assignments/patent-assignment"));
            Assert.That(raw.FieldName, Is.EqualTo("RawXml"));
            Assert.That(dtdFailure.Message, Does.Contain("DTD").IgnoreCase);
        });
    }

    private static XmlRecord[] ReadRecords(string xml, string rootName = "patent-assignments", string recordName = "patent-assignment")
    {
        using MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        XmlRecordReader reader = new XmlRecordReader(input, rootName, null, recordName, null, "assignments.xml", CreateLimits(), CancellationToken.None);
        return reader.ReadBatch(10);
    }

    private static XmlRecord CreateRecord(string xml)
    {
        XmlRecord result = new XmlRecord();
        result.RecordNumber = 4L;
        result.Path = "/patent-assignments/patent-assignment";
        result.StartLineNumber = 17;
        result.HasLineInfo = true;
        result.RawXml = xml;
        return result;
    }

    private static PatentDataProvenance CreateProvenance()
    {
        PatentDataProvenance result = new PatentDataProvenance();
        result.Source = DataProvenance.Bulk;
        result.DatasetIdentifier = "PASDL";
        result.DatasetVersion = "2026-09-01";
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
