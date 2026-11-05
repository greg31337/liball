using System.Text;
using System.Xml;
using System.Xml.Linq;
using uspto;

namespace uspto.Tests;

public class XmlRecordReaderTests
{
    [Test]
    public void NamespacedRecordsRetainMetadataAttributesAndRawSubtrees()
    {
        string xml = "<?xml version=\"1.0\"?><p:catalog xmlns:p=\"urn:catalog\" xmlns:m=\"urn:meta\">\n<p:item m:id=\"1\"><p:name>A &amp; B</p:name><![CDATA[raw <text>]]></p:item>\n<p:item m:id=\"2\"/>\n<p:group><p:item m:id=\"3\"/></p:group></p:catalog>";
        byte[] bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(xml)).ToArray();
        using MemoryStream input = new MemoryStream(bytes);
        XmlRecordReader reader = new XmlRecordReader(input, "catalog", "urn:catalog", "item", "urn:catalog", "catalog.xml", CreateLimits(20, 1000, 2, 10000), CancellationToken.None);

        XmlRecord[] first = reader.ReadBatch(2);
        XmlRecord[] second = reader.ReadBatch(2);
        XmlRecord[] finished = reader.ReadBatch(2);
        XDocument firstRaw = XDocument.Parse(first[0].RawXml, LoadOptions.PreserveWhitespace);

        Assert.Multiple(() =>
        {
            Assert.That(first.Length, Is.EqualTo(2));
            Assert.That(first[0].RecordNumber, Is.EqualTo(1L));
            Assert.That(first[0].LocalName, Is.EqualTo("item"));
            Assert.That(first[0].NamespaceUri, Is.EqualTo("urn:catalog"));
            Assert.That(first[0].Prefix, Is.EqualTo("p"));
            Assert.That(first[0].Path, Is.EqualTo("/p:catalog/p:item"));
            Assert.That(first[0].StartLineNumber, Is.EqualTo(2));
            Assert.That(first[0].HasLineInfo, Is.True);
            Assert.That(first[0].ReadAttribute("id", "urn:meta").Value, Is.EqualTo("1"));
            Assert.That(first[0].ReadAttribute("missing", null).IsPresent, Is.False);
            Assert.That(firstRaw.Root?.Name, Is.EqualTo(XName.Get("item", "urn:catalog")));
            Assert.That(firstRaw.Root?.Attribute(XName.Get("id", "urn:meta"))?.Value, Is.EqualTo("1"));
            Assert.That(firstRaw.Root?.Element(XName.Get("name", "urn:catalog"))?.Value, Is.EqualTo("A & B"));
            Assert.That(firstRaw.Root?.Nodes().OfType<XCData>().Single().Value, Is.EqualTo("raw <text>"));
            Assert.That(second.Single().Path, Is.EqualTo("/p:catalog/p:group/p:item"));
            Assert.That(second.Single().ReadAttribute("id", "urn:meta").Value, Is.EqualTo("3"));
            Assert.That(finished, Is.Empty);
            Assert.That(reader.ActualRootNamespaceUri, Is.EqualTo("urn:catalog"));
        });
    }

    [Test]
    public void NullNamespaceSelectorsMatchAnyPrefixAndNamespace()
    {
        string xml = "<a:root xmlns:a=\"urn:a\" xmlns:b=\"urn:b\"><b:entry>one</b:entry><a:entry>two</a:entry></a:root>";
        using MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        XmlRecordReader reader = new XmlRecordReader(input, "root", null, "entry", null, "wildcard.xml", CreateLimits(10, 100, 10, 1000), CancellationToken.None);

        XmlRecord[] records = reader.ReadBatch(10);

        Assert.Multiple(() =>
        {
            Assert.That(records.Select(record => record.NamespaceUri), Is.EqualTo(new string[] { "urn:b", "urn:a" }));
            Assert.That(records.Select(record => XDocument.Parse(record.RawXml).Root?.Value), Is.EqualTo(new string[] { "one", "two" }));
        });
    }

    [Test]
    public void ExactRecordNamespaceSkipsSameNamedElementsFromOtherNamespaces()
    {
        string xml = "<root xmlns:x=\"urn:x\" xmlns:y=\"urn:y\"><x:entry id=\"1\"/><y:entry id=\"2\"/><entry id=\"3\"/></root>";
        using MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        XmlRecordReader reader = new XmlRecordReader(input, "root", string.Empty, "entry", "urn:y", "exact.xml", CreateLimits(10, 100, 10, 1000), CancellationToken.None);

        XmlRecord record = RequireRecord(reader.ReadRecord());

        Assert.Multiple(() =>
        {
            Assert.That(record.NamespaceUri, Is.EqualTo("urn:y"));
            Assert.That(record.ReadAttribute("id", string.Empty).Value, Is.EqualTo("2"));
            Assert.That(reader.ReadRecord(), Is.Null);
        });
    }

    [Test]
    public void NestedMatchingElementsRemainPartOfTheirOuterRawRecord()
    {
        string xml = "<root><record id=\"1\"><record id=\"2\"/></record><record id=\"3\"/></root>";
        using MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        XmlRecordReader reader = new XmlRecordReader(input, "root", string.Empty, "record", string.Empty, "nested.xml", CreateLimits(10, 100, 10, 1000), CancellationToken.None);

        XmlRecord[] records = reader.ReadBatch(10);
        XDocument outer = XDocument.Parse(records[0].RawXml);

        Assert.Multiple(() =>
        {
            Assert.That(records.Length, Is.EqualTo(2));
            Assert.That(records.Select(record => record.ReadAttribute("id", string.Empty).Value), Is.EqualTo(new string[] { "1", "3" }));
            Assert.That(outer.Root?.Element("record")?.Attribute("id")?.Value, Is.EqualTo("2"));
        });
    }

    [Test]
    public void BatchReadsContinueWithoutClosingTheCallerStream()
    {
        string xml = "<root><record>1</record><record>2</record><record>3</record><record>4</record><record>5</record></root>";
        using MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        XmlRecordReader reader = new XmlRecordReader(input, "root", string.Empty, "record", string.Empty, "batch.xml", CreateLimits(10, 100, 2, 1000), CancellationToken.None);

        XmlRecord[] first = reader.ReadBatch(2);
        XmlRecord[] second = reader.ReadBatch(2);
        XmlRecord[] third = reader.ReadBatch(2);

        Assert.Multiple(() =>
        {
            Assert.That(first.Select(ReadText), Is.EqualTo(new string[] { "1", "2" }));
            Assert.That(second.Select(ReadText), Is.EqualTo(new string[] { "3", "4" }));
            Assert.That(third.Select(ReadText), Is.EqualTo(new string[] { "5" }));
        });

        reader.Close();
        Assert.That(input.CanRead, Is.True);
    }

    [Test]
    public void RootMismatchAndMissingRootReportConfiguredSourceContext()
    {
        using MemoryStream mismatchInput = new MemoryStream(Encoding.UTF8.GetBytes("<other/>"));
        XmlRecordReader mismatchReader = new XmlRecordReader(mismatchInput, "root", string.Empty, "record", string.Empty, "mismatch.xml", CreateLimits(10, 100, 10, 1000), CancellationToken.None);
        ParseException mismatch = Assert.Throws<ParseException>(() => mismatchReader.ReadRecord());

        using MemoryStream emptyInput = new MemoryStream();
        XmlRecordReader emptyReader = new XmlRecordReader(emptyInput, "root", string.Empty, "record", string.Empty, "empty.xml", CreateLimits(10, 100, 10, 1000), CancellationToken.None);
        XmlException missing = Assert.Catch<XmlException>(() => emptyReader.ReadRecord());

        Assert.Multiple(() =>
        {
            Assert.That(mismatch.Format, Is.EqualTo(DocumentFormat.Xml));
            Assert.That(mismatch.SourceName, Is.EqualTo("mismatch.xml"));
            Assert.That(mismatch.DataPath, Is.EqualTo("/other"));
            Assert.That(mismatch.LineNumber, Is.EqualTo(1));
            Assert.That(missing.Message, Does.Contain("Root element is missing"));
        });
    }

    [Test]
    public void DepthTextAndRawSubtreeLimitsAreEnforced()
    {
        using MemoryStream depthInput = new MemoryStream(Encoding.UTF8.GetBytes("<root><group><record/></group></root>"));
        XmlRecordReader depthReader = new XmlRecordReader(depthInput, "root", string.Empty, "record", string.Empty, "depth.xml", CreateLimits(2, 100, 10, 1000), CancellationToken.None);
        ParseException depth = Assert.Throws<ParseException>(() => depthReader.ReadRecord());

        using MemoryStream textInput = new MemoryStream(Encoding.UTF8.GetBytes("<root><record>ab<![CDATA[cd]]>e</record></root>"));
        XmlRecordReader textReader = new XmlRecordReader(textInput, "root", string.Empty, "record", string.Empty, "text.xml", CreateLimits(10, 4, 10, 1000), CancellationToken.None);
        ParseException text = Assert.Throws<ParseException>(() => textReader.ReadRecord());

        using MemoryStream rawInput = new MemoryStream(Encoding.UTF8.GetBytes("<root><record>abc</record></root>"));
        XmlRecordReader rawReader = new XmlRecordReader(rawInput, "root", string.Empty, "record", string.Empty, "raw.xml", CreateLimits(10, 100, 10, 12), CancellationToken.None);
        ParseException raw = Assert.Throws<ParseException>(() => rawReader.ReadRecord());

        Assert.Multiple(() =>
        {
            Assert.That(depth.DataPath, Is.EqualTo("/root/group/record"));
            Assert.That(text.DataPath, Is.EqualTo("/root/record"));
            Assert.That(raw.SourceName, Is.EqualTo("raw.xml"));
            Assert.That(raw.DataPath, Is.EqualTo("/root/record"));
            Assert.That(raw.Message, Does.Contain("raw character limit"));
        });
    }

    [Test]
    public void DtdProcessingRemainsProhibited()
    {
        string xml = "<!DOCTYPE root [<!ENTITY value \"hidden\">]><root><record>&value;</record></root>";
        using MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        XmlRecordReader reader = new XmlRecordReader(input, "root", string.Empty, "record", string.Empty, "dtd.xml", CreateLimits(10, 100, 10, 1000), CancellationToken.None);

        XmlException failure = Assert.Catch<XmlException>(() => reader.ReadRecord());

        Assert.That(failure.Message, Does.Contain("DTD").IgnoreCase);
    }

    [Test]
    public void InvalidBatchSizesAndCancellationAreHonored()
    {
        using MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes("<root/>"));
        XmlRecordReader reader = new XmlRecordReader(input, "root", string.Empty, "record", string.Empty, "batch.xml", CreateLimits(10, 100, 2, 1000), CancellationToken.None);
        ValidationException batch = Assert.Throws<ValidationException>(() => reader.ReadBatch(3));

        using CancellationTokenSource cancellation = new CancellationTokenSource();
        using MemoryStream cancelledInput = new MemoryStream(Encoding.UTF8.GetBytes("<root><record>one</record><record>two</record></root>"));
        XmlRecordReader cancelledReader = new XmlRecordReader(cancelledInput, "root", string.Empty, "record", string.Empty, "cancelled.xml", CreateLimits(10, 100, 2, 1000), cancellation.Token);
        XmlRecord first = RequireRecord(cancelledReader.ReadRecord());
        cancellation.Cancel();

        Assert.Multiple(() =>
        {
            Assert.That(batch.ErrorCode, Is.EqualTo("BATCH_SIZE_OUT_OF_RANGE"));
            Assert.That(ReadText(first), Is.EqualTo("one"));
            Assert.Throws<OperationCanceledException>(() => cancelledReader.ReadRecord());
        });
    }

    [Test]
    public void RequiredNamesAndConfiguredLimitsAreValidated()
    {
        using MemoryStream input = new MemoryStream();
        UsptoLimits valid = CreateLimits(10, 100, 2, 1000);
        ValidationException root = Assert.Throws<ValidationException>(() => new XmlRecordReader(input, " ", null, "record", null, "invalid.xml", valid, CancellationToken.None));
        ValidationException record = Assert.Throws<ValidationException>(() => new XmlRecordReader(input, "root", null, "", null, "invalid.xml", valid, CancellationToken.None));

        UsptoLimits invalidRaw = CreateLimits(10, 100, 2, 0);
        ValidationException raw = Assert.Throws<ValidationException>(() => new XmlRecordReader(input, "root", null, "record", null, "invalid.xml", invalidRaw, CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(root.FieldName, Is.EqualTo("rootLocalName"));
            Assert.That(record.FieldName, Is.EqualTo("recordLocalName"));
            Assert.That(raw.FieldName, Is.EqualTo("MaximumRawResponseBytes"));
        });
    }

    private static XmlRecord RequireRecord(XmlRecord? record)
    {
        return record ?? throw new AssertionException("Expected another XML record.");
    }

    private static string ReadText(XmlRecord record)
    {
        return XDocument.Parse(record.RawXml).Root?.Value ?? string.Empty;
    }

    private static UsptoLimits CreateLimits(int maximumDepth, int maximumTextCharacters, int maximumResults, long maximumRawCharacters)
    {
        UsptoLimits limits = new UsptoLimits();
        limits.MaximumXmlDepth = maximumDepth;
        limits.MaximumStringCharacters = maximumTextCharacters;
        limits.MaximumResults = maximumResults;
        limits.MaximumRawResponseBytes = maximumRawCharacters;
        return limits;
    }
}
