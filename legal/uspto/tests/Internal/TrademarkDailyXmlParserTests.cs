using System.Text;
using System.Xml;
using uspto;

namespace uspto.Tests;

public class TrademarkDailyXmlParserTests
{
    [Test]
    public void LegacyCaseFileParsesStatusMarkOwnersEventsGoodsAndRawValues()
    {
        string xml = """
        <daily-feed xmlns="urn:uspto:tdxf">
          <case-file schema-version="2.3">
            <serial-number>90123456</serial-number><registration-number>7654321</registration-number><transaction-date>20250813</transaction-date>
            <case-file-header>
              <filing-date>20200115</filing-date><registration-date>20240812</registration-date><status-code>700</status-code><status-date>20240812</status-date>
              <status-description>REGISTERED</status-description><mark-identification>ACME ORBIT</mark-identification><mark-drawing-code>4</mark-drawing-code>
              <standard-character-claim>Y</standard-character-claim><color-drawing-current-in>N</color-drawing-current-in><filed-use-in>Y</filed-use-in>
              <principal-register-amended-in>Y</principal-register-amended-in><law-office-assigned>120</law-office-assigned><current-location>650</current-location>
            </case-file-header>
            <case-file-statements>
              <case-file-statement><type-code>DM0000</type-code><text>The mark consists of an orbit.</text></case-file-statement>
              <case-file-statement><type-code>CC0000</type-code><text>Blue is claimed.</text></case-file-statement>
              <case-file-statement><type-code>D10000</type-code><text>No claim to ORBIT.</text></case-file-statement>
              <case-file-statement><type-code>PM0001</type-code><text>ACME OR BIT</text></case-file-statement>
              <case-file-statement><type-code>GS0091</type-code><text>Downloadable computer software.</text></case-file-statement>
            </case-file-statements>
            <case-file-design-search-codes><case-file-design-search-code><design-search-code>01.01.03</design-search-code><description>Stars</description></case-file-design-search-code></case-file-design-search-codes>
            <case-file-owners>
              <case-file-owner><entry-number>1</entry-number><party-type>20</party-type><owner-name>Acme Holdings LLC</owner-name><entity-type-code>16</entity-type-code><nationality-country>US</nationality-country><address-1>100 Main Street</address-1><city>Alexandria</city><state>VA</state><postcode>22314</postcode><country-code>US</country-code><email-address>owner@example.test</email-address></case-file-owner>
            </case-file-owners>
            <case-file-event-statements>
              <case-file-event-statement><code>R.PR</code><type>A</type><description>Registered</description><date>20240812</date><number>10</number></case-file-event-statement>
              <case-file-event-statement><code>APP</code><description>Application filed</description><date>20200115</date><number>1</number></case-file-event-statement>
            </case-file-event-statements>
            <case-file-classifications>
              <case-file-classification><international-code-total-no>009</international-code-total-no><us-code-total-no>021</us-code-total-no><primary-code>009</primary-code><status-code>6</status-code><status-date>20240812</status-date><first-use-anywhere-date>20180301</first-use-anywhere-date><first-use-in-commerce-date>20180401</first-use-in-commerce-date></case-file-classification>
            </case-file-classifications>
            <future-section><future-value>retained</future-value></future-section>
          </case-file>
        </daily-feed>
        """;
        XmlRecord generic = ReadRecords(xml, "daily-feed", "case-file").Single();
        PatentDataProvenance provenance = CreateProvenance();

        TrademarkDailyXmlRecord result = TrademarkDailyXmlParser.ParseRecord(generic, provenance);
        TrademarkCaseStatus status = result.CaseStatus;

        Assert.Multiple(() =>
        {
            Assert.That(result.SchemaGeneration, Is.EqualTo("2.3"));
            Assert.That(result.TransactionDateText, Is.EqualTo("20250813"));
            Assert.That(result.RecordNumber, Is.EqualTo(1L));
            Assert.That(result.SourcePath, Is.EqualTo("/daily-feed/case-file"));
            Assert.That(result.RawXml, Does.Contain("future-section"));
            Assert.That(result.RawValues["/case-file/future-section/future-value"], Is.EqualTo("retained"));
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.Bulk));
            Assert.That(result.Provenance.IsAuthoritative, Is.True);
            Assert.That(result.Provenance, Is.Not.SameAs(provenance));
            Assert.That(status.RawFormat, Is.EqualTo(DocumentFormat.Xml));
            Assert.That(status.RawResponse, Is.EqualTo(result.RawXml));
            Assert.That(status.IsComplete, Is.True);
        });

        Assert.Multiple(() =>
        {
            Assert.That(status.SerialNumber, Is.EqualTo("90123456"));
            Assert.That(status.RegistrationNumber, Is.EqualTo("7654321"));
            Assert.That(status.CanonicalIdentifier, Is.EqualTo("sn90123456"));
            Assert.That(status.IdentifierKind, Is.EqualTo(TrademarkIdentifierKind.SerialNumber));
            Assert.That(status.Status.Code, Is.EqualTo(700));
            Assert.That(status.Status.CodeText, Is.EqualTo("700"));
            Assert.That(status.Status.ExternalDescription, Is.EqualTo("REGISTERED"));
            Assert.That(status.Status.HasFilingDate, Is.True);
            Assert.That(status.Status.FilingDate, Is.EqualTo(new DateTimeOffset(2020, 1, 15, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(status.Status.HasRegistrationDate, Is.True);
            Assert.That(status.Status.RegisterCategory, Is.EqualTo("Principal"));
            Assert.That(status.Status.LawOfficeCode, Is.EqualTo("120"));
            Assert.That(status.Status.CurrentLocationCode, Is.EqualTo("650"));
        });

        Assert.Multiple(() =>
        {
            Assert.That(status.Mark.VerbalElementText, Is.EqualTo("ACME ORBIT"));
            Assert.That(status.Mark.DrawingCode, Is.EqualTo("4"));
            Assert.That(status.Mark.IsStandardCharacter, Is.True);
            Assert.That(status.Mark.HasIsStandardCharacter, Is.True);
            Assert.That(status.Mark.IsColorDrawing, Is.False);
            Assert.That(status.Mark.HasIsColorDrawing, Is.True);
            Assert.That(status.Mark.Description, Is.EqualTo("The mark consists of an orbit."));
            Assert.That(status.Mark.ColorClaimText, Is.EqualTo("Blue is claimed."));
            Assert.That(status.Mark.DisclaimerText, Is.EqualTo("No claim to ORBIT."));
            Assert.That(status.Mark.PseudoMarkText, Is.EqualTo("ACME OR BIT"));
            Assert.That(status.Mark.DesignSearchClassifications.Single().Code, Is.EqualTo("01.01.03"));
            Assert.That(status.FilingBasis.Section1A, Is.True);
            Assert.That(status.FilingBasis.HasSection1A, Is.True);
        });

        Assert.Multiple(() =>
        {
            TrademarkOwner owner = status.CurrentOwners.Single();
            Assert.That(owner.Name, Is.EqualTo("Acme Holdings LLC"));
            Assert.That(owner.SequenceNumber, Is.EqualTo(1));
            Assert.That(owner.PartyTypeCode, Is.EqualTo(20));
            Assert.That(owner.EntityTypeCode, Is.EqualTo("16"));
            Assert.That(owner.CitizenshipCountryCode, Is.EqualTo("US"));
            Assert.That(owner.Address.City, Is.EqualTo("Alexandria"));
            Assert.That(owner.Address.PostalCode, Is.EqualTo("22314"));
            Assert.That(owner.EmailAddresses, Is.EqualTo(new string[] { "owner@example.test" }));
            Assert.That(owner.IsCurrent, Is.True);
            Assert.That(status.ProsecutionEvents, Has.Length.EqualTo(2));
            Assert.That(status.ProsecutionEvents[0].Code, Is.EqualTo("R.PR"));
            Assert.That(status.ProsecutionEvents[0].EntryNumber, Is.EqualTo(10));
            Assert.That(status.ProsecutionEvents[0].HasEventDate, Is.True);
        });

        Assert.Multiple(() =>
        {
            TrademarkGoodsAndService goods = status.GoodsAndServices.Single();
            Assert.That(goods.SerialNumber, Is.EqualTo("90123456"));
            Assert.That(goods.PrimeClassCode, Is.EqualTo("009"));
            Assert.That(goods.Description, Is.EqualTo("Downloadable computer software."));
            Assert.That(goods.StatusCode, Is.EqualTo("6"));
            Assert.That(goods.HasStatusDate, Is.True);
            Assert.That(goods.HasFirstUseDate, Is.True);
            Assert.That(goods.FirstUseDateText, Is.EqualTo("20180301"));
            Assert.That(goods.HasFirstUseInCommerceDate, Is.True);
            Assert.That(goods.Classes.Select(item => item.Category), Is.EqualTo(new string[] { "international", "us" }));
            Assert.That(goods.Classes.Select(item => item.Code), Is.EqualTo(new string[] { "009", "021" }));
            Assert.That(goods.FilingBasis.Section1A, Is.True);
        });
    }

    [Test]
    public void TransactionGenerationParsesModernNamespacedStatusHierarchy()
    {
        string xml = """
        <feed xmlns:t="urn:uspto:tsdr">
          <t:TrademarkTransaction version="transaction-3">
            <t:Trademark>
              <t:Status><t:SerialNumber>88111222</t:SerialNumber><t:USRegistrationNumber>6123456</t:USRegistrationNumber><t:Status>630</t:Status><t:StatusDate>date pending</t:StatusDate><t:FilingDate>2019-03-04</t:FilingDate><t:MarkElement>MODERN MARK</t:MarkElement><t:StandardChar>true</t:StandardChar><t:FiledItu>true</t:FiledItu></t:Status>
              <t:Parties><t:CurrentOwners><t:Party><t:EntityNum>2</t:EntityNum><t:PartyType>10</t:PartyType><t:Name>Modern Owner Inc.</t:Name><t:Address1>200 New Road</t:Address1><t:City>Arlington</t:City><t:CountryCode>US</t:CountryCode></t:Party></t:CurrentOwners><t:PriorOwners><t:Owner><t:Name>Old Owner LLC</t:Name></t:Owner></t:PriorOwners></t:Parties>
              <t:ProsecutionHistory><t:Prosecution><t:EntryCode>M-1</t:EntryCode><t:EntryDesc>Modern event</t:EntryDesc><t:EntryDate>2024-06-07</t:EntryDate><t:SequenceNum>4</t:SequenceNum></t:Prosecution></t:ProsecutionHistory>
              <t:GsList><t:GoodsAndService><t:PrimeClassCode>042</t:PrimeClassCode><t:Description>Technology services</t:Description><t:FirstUseDate>20200101</t:FirstUseDate><t:InternationalClasses><t:TmClass><t:Code>042</t:Code><t:Description>Scientific services</t:Description></t:TmClass></t:InternationalClasses><t:USClasses><t:Class><t:Code>100</t:Code></t:Class></t:USClasses></t:GoodsAndService></t:GsList>
            </t:Trademark>
          </t:TrademarkTransaction>
        </feed>
        """;
        XmlRecord generic = ReadRecords(xml, "feed", "TrademarkTransaction").Single();

        TrademarkDailyXmlRecord result = TrademarkDailyXmlParser.ParseRecord(generic, CreateProvenance());
        TrademarkCaseStatus status = result.CaseStatus;

        Assert.Multiple(() =>
        {
            Assert.That(result.SchemaGeneration, Is.EqualTo("transaction-3"));
            Assert.That(status.SerialNumber, Is.EqualTo("88111222"));
            Assert.That(status.RegistrationNumber, Is.EqualTo("6123456"));
            Assert.That(status.Status.Code, Is.EqualTo(630));
            Assert.That(status.Status.StatusDateText, Is.EqualTo("date pending"));
            Assert.That(status.Status.HasStatusDate, Is.False);
            Assert.That(status.Status.HasFilingDate, Is.True);
            Assert.That(status.Mark.VerbalElementText, Is.EqualTo("MODERN MARK"));
            Assert.That(status.Mark.IsStandardCharacter, Is.True);
            Assert.That(status.FilingBasis.Section1B, Is.True);
            Assert.That(status.CurrentOwners.Single().Name, Is.EqualTo("Modern Owner Inc."));
            Assert.That(status.CurrentOwners.Single().Address.Line1, Is.EqualTo("200 New Road"));
            Assert.That(status.PriorOwners.Single().Name, Is.EqualTo("Old Owner LLC"));
            Assert.That(status.PriorOwners.Single().IsCurrent, Is.False);
            Assert.That(status.ProsecutionEvents.Single().SequenceNumber, Is.EqualTo(4));
            Assert.That(status.GoodsAndServices.Single().Description, Is.EqualTo("Technology services"));
            Assert.That(status.GoodsAndServices.Single().Classes.Select(item => item.Code), Is.EqualTo(new string[] { "042", "100" }));
            Assert.That(status.GoodsAndServices.Single().Classes[0].Description, Is.EqualTo("Scientific services"));
        });
    }

    [Test]
    public void AlternateCaseFileGenerationUsesPunctuationInsensitiveAliases()
    {
        string xml = """
        <TrademarkCaseFile dtd-version="1.0">
          <SerialNumber>77123456</SerialNumber>
          <CaseFileHeader><StatusCode>600</StatusCode><StatusDate>2007/10/11</StatusDate><MarkIdentification>OLD MARK</MarkIdentification><SupplementalRegisterIn>1</SupplementalRegisterIn></CaseFileHeader>
          <CaseFileOwners><CaseFileOwner><OwnerName>Legacy Owner</OwnerName><CurrentOwnerIn>N</CurrentOwnerIn></CaseFileOwner></CaseFileOwners>
          <CaseFileClassifications><CaseFileClassification><InternationalCode>035</InternationalCode><Identification>Retail store services</Identification></CaseFileClassification></CaseFileClassifications>
        </TrademarkCaseFile>
        """;
        TrademarkDailyXmlRecord result = TrademarkDailyXmlParser.ParseRecord(CreateRecord(xml), CreateProvenance());

        Assert.Multiple(() =>
        {
            Assert.That(result.SchemaGeneration, Is.EqualTo("1.0"));
            Assert.That(result.CaseStatus.SerialNumber, Is.EqualTo("77123456"));
            Assert.That(result.CaseStatus.Status.Code, Is.EqualTo(600));
            Assert.That(result.CaseStatus.Status.StatusDateText, Is.EqualTo("2007/10/11"));
            Assert.That(result.CaseStatus.Status.HasStatusDate, Is.False);
            Assert.That(result.CaseStatus.Status.RegisterCategory, Is.EqualTo("Supplemental"));
            Assert.That(result.CaseStatus.Mark.VerbalElementText, Is.EqualTo("OLD MARK"));
            Assert.That(result.CaseStatus.CurrentOwners.Single().IsCurrent, Is.False);
            Assert.That(result.CaseStatus.GoodsAndServices.Single().PrimeClassCode, Is.EqualTo("035"));
            Assert.That(result.CaseStatus.GoodsAndServices.Single().Description, Is.EqualTo("Retail store services"));
        });
    }

    [TestCase("<case-file><serial-number>90123456</serial-number><future-field>kept</future-field></case-file>", "case-file", "/case-file/future-field")]
    [TestCase("<transaction><trademark><serial-number>88111222</serial-number><future-field>kept</future-field></trademark></transaction>", "transaction", "/trademark/future-field")]
    public void UnversionedDocumentedRootsUseStableGenerationFallbacks(string xml, string generation, string rawPath)
    {
        TrademarkDailyXmlRecord result = TrademarkDailyXmlParser.ParseRecord(CreateRecord(xml), CreateProvenance());

        Assert.Multiple(() =>
        {
            Assert.That(result.SchemaGeneration, Is.EqualTo(generation));
            Assert.That(result.CaseStatus.SerialNumber, Is.AnyOf("90123456", "88111222"));
            Assert.That(result.RawValues[rawPath], Is.EqualTo("kept"));
            Assert.That(result.RawXml, Is.EqualTo(xml));
        });
    }

    [Test]
    public void UnknownEmptyFieldsRemainRawWithoutCreatingTypedValues()
    {
        string xml = "<case-file schema-version=''><serial-number>90123456</serial-number><case-file-header><status-code></status-code></case-file-header><case-file-classifications><case-file-classification><international-code>009</international-code><future-classification>raw</future-classification></case-file-classification></case-file-classifications></case-file>";

        TrademarkDailyXmlRecord result = TrademarkDailyXmlParser.ParseRecord(CreateRecord(xml), CreateProvenance());

        Assert.Multiple(() =>
        {
            Assert.That(result.SchemaGeneration, Is.EqualTo("case-file"));
            Assert.That(result.CaseStatus.Status.CodeText, Is.Null);
            Assert.That(result.CaseStatus.GoodsAndServices.Single().Description, Is.Null);
            Assert.That(result.RawValues["/case-file/case-file-classifications/case-file-classification/future-classification"], Is.EqualTo("raw"));
        });
    }

    [TestCase("<case-file><case-file-header><status-code>700</status-code></case-file-header></case-file>", "no serial number")]
    [TestCase("<case-file><serial-number>123</serial-number></case-file>", "serial number is invalid")]
    [TestCase("<trademark-assignment><serial-number>90123456</serial-number></trademark-assignment>", "not a trademark application")]
    public void MissingInvalidAndWrongRecordIdentitiesReportTypedXmlFailures(string xml, string expectedMessage)
    {
        ParseException failure = Assert.Throws<ParseException>(() => TrademarkDailyXmlParser.ParseRecord(CreateRecord(xml), CreateProvenance()));

        Assert.Multiple(() =>
        {
            Assert.That(failure.Format, Is.EqualTo(DocumentFormat.Xml));
            Assert.That(failure.SourceName, Is.EqualTo("trademark daily XML"));
            Assert.That(failure.Message, Does.Contain(expectedMessage));
            Assert.That(failure.HasLineNumber, Is.True);
            Assert.That(failure.LineNumber, Is.EqualTo(12));
        });
    }

    [Test]
    public void MissingRawInputAndDtdEntityExpansionRemainRejected()
    {
        XmlRecord missing = new XmlRecord();
        ValidationException raw = Assert.Throws<ValidationException>(() => TrademarkDailyXmlParser.ParseRecord(missing, CreateProvenance()));
        XmlRecord dtd = CreateRecord("<!DOCTYPE case-file [<!ENTITY hidden '90123456'>]><case-file><serial-number>&hidden;</serial-number></case-file>");
        XmlException dtdFailure = Assert.Catch<XmlException>(() => TrademarkDailyXmlParser.ParseRecord(dtd, CreateProvenance()));

        Assert.Multiple(() =>
        {
            Assert.That(raw.FieldName, Is.EqualTo("RawXml"));
            Assert.That(dtdFailure.Message, Does.Contain("DTD").IgnoreCase);
        });
    }

    [Test]
    public void TruncatedTrademarkDailyXmlIsRejected()
    {
        XmlException failure = Assert.Throws<XmlException>(() => TrademarkDailyXmlParser.ParseRecord(CreateRecord("<case-file><serial-number>90123456"), CreateProvenance()));

        Assert.That(failure.Message, Does.Contain("unexpected end").IgnoreCase);
    }

    private static XmlRecord[] ReadRecords(string xml, string rootName, string recordName)
    {
        using MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        XmlRecordReader reader = new XmlRecordReader(input, rootName, null, recordName, null, "trademark-daily.xml", CreateLimits(), CancellationToken.None);
        return reader.ReadBatch(10);
    }

    private static XmlRecord CreateRecord(string xml)
    {
        XmlRecord result = new XmlRecord();
        result.RecordNumber = 3L;
        result.Path = "/daily-feed/case-file";
        result.StartLineNumber = 12;
        result.HasLineInfo = true;
        result.RawXml = xml;
        return result;
    }

    private static PatentDataProvenance CreateProvenance()
    {
        PatentDataProvenance result = new PatentDataProvenance();
        result.Source = DataProvenance.Bulk;
        result.DatasetIdentifier = "TRTDXFAP";
        result.DatasetVersion = "2025-08-13";
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
