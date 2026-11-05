using System.Text;
using System.Xml;
using iso20022;

namespace iso20022.Tests;

public class SecureXmlApiTests
{
    [TestCase("<!DOCTYPE Root><Root/>")]
    [TestCase("<!DOCTYPE Root [<!ENTITY local 'expanded'>]><Root>&local;</Root>")]
    [TestCase("<!DOCTYPE Root [<!ENTITY external SYSTEM 'file:///private/iso20022-xxe-sentinel'>]><Root>&external;</Root>")]
    [TestCase("<!DOCTYPE Root [<!ENTITY a '1234567890'><!ENTITY b '&a;&a;&a;&a;&a;&a;&a;&a;&a;&a;'><!ENTITY c '&b;&b;&b;&b;&b;&b;&b;&b;&b;&b;'>]><Root>&c;</Root>")]
    public void ReaderApiRejectsDtdXxeAndEntityExpansionForEveryInputForm(string xml)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        IXmlMessageReader reader = new XmlMessageReader();

        XmlException? textException = Assert.Throws<XmlException>(() => reader.ReadGenericText(xml, new MessageReadOptions()));
        XmlException? byteException = Assert.Throws<XmlException>(() => reader.ReadGenericBytes(bytes, new MessageReadOptions()));
        XmlException? streamException = Assert.Throws<XmlException>(() => reader.ReadGenericStream(new MemoryStream(bytes, false), new MessageReadOptions()));

        Assert.Multiple(() =>
        {
            Assert.That(textException?.Message, Does.Contain("DTD").IgnoreCase);
            Assert.That(byteException?.Message, Does.Contain("DTD").IgnoreCase);
            Assert.That(streamException?.Message, Does.Contain("DTD").IgnoreCase);
        });
    }

    [Test]
    public void DetectorApiRejectsExternalEntitiesWithoutConsumingOrResolvingThem()
    {
        string xml = "<!DOCTYPE Document [<!ENTITY external SYSTEM 'https://invalid.example.test/secret'>]><Document xmlns='" + Namespace("pacs.008.001.10") + "'>&external;</Document>";
        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        IMessageDetector detector = new MessageDetector();

        XmlException? textException = Assert.Throws<XmlException>(() => detector.DetectText(xml, new MessageReadOptions()));
        XmlException? byteException = Assert.Throws<XmlException>(() => detector.DetectBytes(bytes, new MessageReadOptions()));
        XmlException? streamException = Assert.Throws<XmlException>(() => detector.DetectStream(new NonSeekableStream(bytes), new MessageReadOptions()));

        Assert.Multiple(() =>
        {
            Assert.That(textException?.Message, Does.Contain("DTD").IgnoreCase);
            Assert.That(byteException?.Message, Does.Contain("DTD").IgnoreCase);
            Assert.That(streamException?.Message, Does.Contain("DTD").IgnoreCase);
        });
    }

    [TestCase("depth", "MaxDepth")]
    [TestCase("attributes", "MaxAttributesPerElement")]
    [TestCase("text", "MaxTextLength")]
    [TestCase("attribute-value", "MaxTextLength")]
    [TestCase("elements", "MaxElementCount")]
    [TestCase("collection", "MaxCollectionCount")]
    [TestCase("bytes", "MaxTotalBytes")]
    public void GenericReaderApiRejectsEveryOversizedConstructWithNamedLimitDiagnostic(string limitKind, string expectedLimit)
    {
        string xml = SelectLimitXml(limitKind);
        MessageReadOptions options = CreateExceededLimitOptions(limitKind, Encoding.UTF8.GetByteCount(xml));
        IXmlMessageReader reader = new XmlMessageReader();

        XmlException? exception = Assert.Throws<XmlException>(() => reader.ReadGenericText(xml, options));

        Assert.That(exception?.Message, Does.Contain(expectedLimit));
    }

    [Test]
    public void DeepTreeAndLargeSiblingCollectionAreRejectedBeforeMaterializationCompletes()
    {
        string deep = CreateDeepXml(32);
        string wide = CreateWideXml(64);
        MessageReadOptions depthOptions = new MessageReadOptions();
        depthOptions.Limits.MaxDepth = 16;
        MessageReadOptions collectionOptions = new MessageReadOptions();
        collectionOptions.Limits.MaxCollectionCount = 32;
        IXmlMessageReader reader = new XmlMessageReader();

        XmlException? depthException = Assert.Throws<XmlException>(() => reader.ReadGenericText(deep, depthOptions));
        XmlException? collectionException = Assert.Throws<XmlException>(() => reader.ReadGenericText(wide, collectionOptions));

        Assert.Multiple(() =>
        {
            Assert.That(depthException?.Message, Does.Contain("MaxDepth"));
            Assert.That(collectionException?.Message, Does.Contain("MaxCollectionCount"));
        });
    }

    [Test]
    public void ExactResourceBoundariesRemainReadableThroughPublicByteTextAndStreamApis()
    {
        string xml = "<Root a='123'><First>123</First><Second/></Root>";
        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        MessageReadOptions byteOptions = ExactBoundaryOptions(bytes.Length);
        MessageReadOptions textOptions = ExactBoundaryOptions(bytes.Length);
        MessageReadOptions streamOptions = ExactBoundaryOptions(bytes.Length);
        IXmlMessageReader reader = new XmlMessageReader();

        MessageReadResult byteResult = reader.ReadGenericBytes(bytes, byteOptions);
        MessageReadResult textResult = reader.ReadGenericText(xml, textOptions);
        MessageReadResult streamResult = reader.ReadGenericStream(new MemoryStream(bytes, false), streamOptions);

        Assert.Multiple(() =>
        {
            Assert.That(byteResult.Success, Is.True);
            Assert.That(textResult.Success, Is.True);
            Assert.That(streamResult.Success, Is.True);
            Assert.That(byteResult.BytesConsumed, Is.EqualTo(bytes.Length));
            Assert.That(textResult.BytesConsumed, Is.EqualTo(bytes.Length));
            Assert.That(streamResult.BytesConsumed, Is.EqualTo(bytes.Length));
        });
    }

    [Test]
    public void MalformedUtf8Utf16AndEncodingMismatchAreRejectedByPublicByteApi()
    {
        byte[] invalidUtf8 = Encoding.ASCII.GetBytes("<Root>ok</Root>");
        invalidUtf8[6] = 0xFF;
        byte[] truncatedUtf16 =
        [
            0xFF, 0xFE,
            0x3C, 0x00,
            0x52
        ];
        byte[] mismatchedDeclaration = Encoding.UTF8.GetBytes("<?xml version='1.0' encoding='utf-16'?><Root/>");
        IXmlMessageReader reader = new XmlMessageReader();

        XmlException? utf8Exception = Assert.Throws<XmlException>(() => reader.ReadGenericBytes(invalidUtf8, new MessageReadOptions()));
        XmlException? utf16Exception = Assert.Throws<XmlException>(() => reader.ReadGenericBytes(truncatedUtf16, new MessageReadOptions()));
        XmlException? mismatchException = Assert.Throws<XmlException>(() => reader.ReadGenericBytes(mismatchedDeclaration, new MessageReadOptions()));

        Assert.Multiple(() =>
        {
            Assert.That(utf8Exception?.Message, Is.Not.Empty);
            Assert.That(utf16Exception?.Message, Is.Not.Empty);
            Assert.That(mismatchException?.Message, Is.Not.Empty);
        });
    }

    [Test]
    public void StreamByteLimitFailureIsNamedAndDoesNotRequireSeekSupport()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("<Root>value</Root>");
        MessageReadOptions options = new MessageReadOptions();
        options.Limits.MaxTotalBytes = bytes.Length - 1;
        NonSeekableStream stream = new NonSeekableStream(bytes);
        IXmlMessageReader reader = new XmlMessageReader();

        XmlException? exception = Assert.Throws<XmlException>(() => reader.ReadGenericStream(stream, options));

        Assert.Multiple(() =>
        {
            Assert.That(exception?.Message, Does.Contain("MaxTotalBytes"));
            Assert.That(stream.ReadCount, Is.GreaterThan(0));
        });
    }

    internal static string Namespace(string identifier)
    {
        return Iso20022Namespaces.SchemaUrnPrefix + identifier;
    }

    internal static string SelectLimitXml(string kind)
    {
        if (kind == "depth")
        {
            return "<Root><First><Second/></First></Root>";
        }

        if (kind == "attributes")
        {
            return "<Root a='1' b='2'/>";
        }

        if (kind == "text")
        {
            return "<Root>four</Root>";
        }

        if (kind == "attribute-value")
        {
            return "<Root value='four'/>";
        }

        if (kind == "elements" || kind == "collection")
        {
            return "<Root><First/><Second/></Root>";
        }

        return "<Root>value</Root>";
    }

    internal static MessageReadOptions CreateExceededLimitOptions(string kind, int byteCount)
    {
        MessageReadOptions options = new MessageReadOptions();
        if (kind == "depth")
        {
            options.Limits.MaxDepth = 2;
        }
        else if (kind == "attributes")
        {
            options.Limits.MaxAttributesPerElement = 1;
        }
        else if (kind == "text" || kind == "attribute-value")
        {
            options.Limits.MaxTextLength = 3;
        }
        else if (kind == "elements")
        {
            options.Limits.MaxElementCount = 2;
        }
        else if (kind == "collection")
        {
            options.Limits.MaxCollectionCount = 1;
        }
        else
        {
            options.Limits.MaxTotalBytes = byteCount - 1;
        }

        return options;
    }

    internal static string CreateDeepXml(int depth)
    {
        StringBuilder value = new StringBuilder();
        int index = 0;
        while (index < depth)
        {
            value.Append("<Node>");
            index++;
        }

        index = 0;
        while (index < depth)
        {
            value.Append("</Node>");
            index++;
        }

        return value.ToString();
    }

    internal static string CreateWideXml(int width)
    {
        StringBuilder value = new StringBuilder();
        value.Append("<Root>");
        int index = 0;
        while (index < width)
        {
            value.Append("<Item/>");
            index++;
        }

        value.Append("</Root>");
        return value.ToString();
    }

    internal static MessageReadOptions ExactBoundaryOptions(int byteCount)
    {
        MessageReadOptions options = new MessageReadOptions();
        options.Limits.MaxDepth = 2;
        options.Limits.MaxAttributesPerElement = 1;
        options.Limits.MaxTextLength = 3;
        options.Limits.MaxElementCount = 3;
        options.Limits.MaxCollectionCount = 2;
        options.Limits.MaxTotalBytes = byteCount;
        return options;
    }

    internal class NonSeekableStream : Stream
    {
        MemoryStream inner;

        internal NonSeekableStream(byte[] bytes)
        {
            inner = new MemoryStream(bytes, false);
        }

        internal int ReadCount;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ReadCount++;
            return inner.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
