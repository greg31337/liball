using System.Text;
using uspto;

namespace uspto.Tests;

public class CompoundXmlReaderTests
{
    [Test]
    public void DeclarationsDoctypesCommentsAndCdataSplitAcrossTinyBuffers()
    {
        string first = "<?xml version=\"1.0\"?>\n<!DOCTYPE grant [\n<!ELEMENT grant ANY>\n<!ENTITY marker \">\">\n]>\n<grant id=\"1\"><text><![CDATA[not </grant> or <?xml]]></text><!-- </grant> --></grant>";
        string second = "<?xml version=\"1.0\"?>\r\n<!DOCTYPE grant SYSTEM \"test>file.dtd\">\r\n<grant id=\"2\"/>";
        byte[] prefix = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes("\r\n  ")).ToArray();
        byte[] inputBytes = prefix.Concat(Encoding.UTF8.GetBytes(first + "\n\n" + second + "\r\n")).ToArray();
        using MemoryStream input = new MemoryStream(inputBytes);
        CompoundXmlReader reader = new CompoundXmlReader(input, "weekly.xml", 3, CreateLimits(10000));

        CompoundXmlDocument firstDocument = RequireDocument(reader.ReadDocument());
        CompoundXmlDocument secondDocument = RequireDocument(reader.ReadDocument());

        Assert.Multiple(() =>
        {
            Assert.That(Encoding.UTF8.GetString(firstDocument.Bytes), Is.EqualTo(first));
            Assert.That(Encoding.UTF8.GetString(secondDocument.Bytes), Is.EqualTo(second));
            Assert.That(firstDocument.DocumentNumber, Is.EqualTo(1L));
            Assert.That(secondDocument.DocumentNumber, Is.EqualTo(2L));
            Assert.That(firstDocument.StartOffset, Is.EqualTo(prefix.Length));
            Assert.That(firstDocument.EndOffset, Is.EqualTo(prefix.Length + Encoding.UTF8.GetByteCount(first)));
            Assert.That(secondDocument.EndOffset - secondDocument.StartOffset, Is.EqualTo(Encoding.UTF8.GetByteCount(second)));
            Assert.That(firstDocument.StartLineNumber, Is.EqualTo(2L));
            Assert.That(firstDocument.EndLineNumber, Is.GreaterThan(firstDocument.StartLineNumber));
            Assert.That(reader.ReadDocument(), Is.Null);
        });
    }

    [Test]
    public void RootDepthIgnoresMarkupTextAndQuotedTagCharacters()
    {
        string first = "<!--lead--><?work value?><root attr=\"literal /> and >\"><root/><child><x/></child></root>";
        string second = "<next/>";
        using MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes(first + second));
        CompoundXmlReader reader = new CompoundXmlReader(input, "roots.xml", 1, CreateLimits(1000));

        CompoundXmlDocument firstDocument = RequireDocument(reader.ReadDocument());
        CompoundXmlDocument secondDocument = RequireDocument(reader.ReadDocument());

        Assert.Multiple(() =>
        {
            Assert.That(Encoding.UTF8.GetString(firstDocument.Bytes), Is.EqualTo(first));
            Assert.That(Encoding.UTF8.GetString(secondDocument.Bytes), Is.EqualTo(second));
            Assert.That(secondDocument.StartOffset, Is.EqualTo(Encoding.UTF8.GetByteCount(first)));
            Assert.That(reader.ReadDocument(), Is.Null);
        });
    }

    [Test]
    public void NamespacesAndMixedContentPreserveExactDocumentBoundaries()
    {
        const string first = "<ns:record xmlns:ns=\"urn:records\">before<ns:item code=\"1&gt;0\">one</ns:item>after &amp; more</ns:record>";
        const string second = "<record xmlns=\"urn:default\">lead<child><![CDATA[<not-a-tag>]]></child>tail</record>";
        using MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes(first + "\r\n" + second));
        CompoundXmlReader reader = new CompoundXmlReader(input, "namespaces.xml", 2, CreateLimits(1000));

        CompoundXmlDocument firstDocument = RequireDocument(reader.ReadDocument());
        CompoundXmlDocument secondDocument = RequireDocument(reader.ReadDocument());

        Assert.Multiple(() =>
        {
            Assert.That(Encoding.UTF8.GetString(firstDocument.Bytes), Is.EqualTo(first));
            Assert.That(Encoding.UTF8.GetString(secondDocument.Bytes), Is.EqualTo(second));
            Assert.That(firstDocument.EndOffset, Is.EqualTo(Encoding.UTF8.GetByteCount(first)));
            Assert.That(secondDocument.StartOffset, Is.EqualTo(Encoding.UTF8.GetByteCount(first + "\r\n")));
            Assert.That(secondDocument.StartLineNumber, Is.EqualTo(2L));
            Assert.That(reader.ReadDocument(), Is.Null);
        });
    }

    [Test]
    public void Utf8BomIsAcceptedAtEveryDocumentBoundary()
    {
        byte[] bom = Encoding.UTF8.GetPreamble();
        byte[] first = Encoding.UTF8.GetBytes("<first/>");
        byte[] second = Encoding.UTF8.GetBytes("<second/>");
        byte[] inputBytes = bom.Concat(first).Concat(bom).Concat(second).ToArray();
        using MemoryStream input = new MemoryStream(inputBytes);
        CompoundXmlReader reader = new CompoundXmlReader(input, "bom.xml", 2, CreateLimits(100));

        CompoundXmlDocument firstDocument = RequireDocument(reader.ReadDocument());
        CompoundXmlDocument secondDocument = RequireDocument(reader.ReadDocument());

        Assert.Multiple(() =>
        {
            Assert.That(firstDocument.Bytes, Is.EqualTo(first));
            Assert.That(secondDocument.Bytes, Is.EqualTo(second));
            Assert.That(firstDocument.StartOffset, Is.EqualTo(3L));
            Assert.That(secondDocument.StartOffset, Is.EqualTo(3L + first.Length + 3L));
        });
    }

    [Test]
    public void DocumentByteLimitReportsDocumentOffsetAndLine()
    {
        string xml = "\n<root>12345</root>";
        using MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        CompoundXmlReader reader = new CompoundXmlReader(input, "large.xml", 4, CreateLimits(10));

        ParseException exception = Assert.Throws<ParseException>(() => reader.ReadDocument());

        Assert.Multiple(() =>
        {
            Assert.That(exception.Format, Is.EqualTo(DocumentFormat.Xml));
            Assert.That(exception.SourceName, Is.EqualTo("large.xml"));
            Assert.That(exception.DataPath, Is.EqualTo("document[1]"));
            Assert.That(exception.HasOffset, Is.True);
            Assert.That(exception.Offset, Is.EqualTo(11L));
            Assert.That(exception.HasLineNumber, Is.True);
            Assert.That(exception.LineNumber, Is.EqualTo(2));
            Assert.That(exception.Message, Does.Contain("byte limit"));
        });
    }

    [Test]
    public void TruncatedRootReportsTheEndOffset()
    {
        string xml = "<root><child/></root";
        using MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        CompoundXmlReader reader = new CompoundXmlReader(input, "truncated.xml", 5, CreateLimits(100));

        ParseException exception = Assert.Throws<ParseException>(() => reader.ReadDocument());

        Assert.Multiple(() =>
        {
            Assert.That(exception.DataPath, Is.EqualTo("document[1]"));
            Assert.That(exception.Offset, Is.EqualTo(Encoding.UTF8.GetByteCount(xml)));
            Assert.That(exception.LineNumber, Is.EqualTo(1));
            Assert.That(exception.Message, Does.Contain("ended before"));
        });
    }

    [Test]
    public void UnexpectedBytesBetweenDocumentsIdentifyTheNextDocument()
    {
        using MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes("<first/>junk<second/>"));
        CompoundXmlReader reader = new CompoundXmlReader(input, "unexpected.xml", 64, CreateLimits(100));

        CompoundXmlDocument first = RequireDocument(reader.ReadDocument());
        ParseException exception = Assert.Throws<ParseException>(() => reader.ReadDocument());

        Assert.Multiple(() =>
        {
            Assert.That(Encoding.UTF8.GetString(first.Bytes), Is.EqualTo("<first/>"));
            Assert.That(exception.DataPath, Is.EqualTo("document[2]"));
            Assert.That(exception.Offset, Is.EqualTo(8L));
            Assert.That(exception.LineNumber, Is.EqualTo(1));
        });
    }

    [Test]
    public void FixedBufferBoundsInputReadRequestsAcrossManyDocuments()
    {
        string xml = string.Concat(Enumerable.Repeat("<record/>", 100));
        using TrackingReadStream input = new TrackingReadStream(Encoding.UTF8.GetBytes(xml));
        CompoundXmlReader reader = new CompoundXmlReader(input, "many.xml", 7, CreateLimits(100));

        int count = 0;
        while (reader.ReadDocument() != null)
        {
            count += 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(100));
            Assert.That(input.LargestReadRequest, Is.EqualTo(7));
        });
    }

    [Test]
    public void WhitespaceOnlyInputContainsNoDocuments()
    {
        using MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes(" \r\n\t"));
        CompoundXmlReader reader = new CompoundXmlReader(input, "empty.xml", 2, CreateLimits(100));

        Assert.That(reader.ReadDocument(), Is.Null);
    }

    private static CompoundXmlDocument RequireDocument(CompoundXmlDocument? document)
    {
        return document ?? throw new AssertionException("Expected another XML document.");
    }

    private static UsptoLimits CreateLimits(long maximumDocumentBytes)
    {
        UsptoLimits limits = new UsptoLimits();
        limits.MaximumResponseBytes = maximumDocumentBytes;
        return limits;
    }

    private sealed class TrackingReadStream : MemoryStream
    {
        public int LargestReadRequest { get; private set; }

        public TrackingReadStream(byte[] bytes)
            : base(bytes)
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count > LargestReadRequest)
            {
                LargestReadRequest = count;
            }

            return base.Read(buffer, offset, count);
        }
    }
}
