using System;
using System.IO;
using System.Text;
using xml;

namespace xml.Tests;

[TestFixture]
public class XmlParserChunkBoundaryTests
{
    [Test]
    public void ParsesUtf8AtEveryByteBoundary()
    {
        AssertEveryByteBoundary(new UTF8Encoding(true, true), "UTF-8");
    }

    [Test]
    public void ParsesUtf16AtEveryByteBoundary()
    {
        AssertEveryByteBoundary(new UnicodeEncoding(false, true, true), "UTF-16");
    }

    [Test]
    public void ParsesAtEveryUtf16CharacterBoundary()
    {
        string text = CreateBoundaryDocument("UTF-8");
        int boundary = 1;
        while (boundary < text.Length)
        {
            BoundaryTextReader reader = new BoundaryTextReader(text, boundary);
            XmlInput input = new XmlInput();
            input.Kind = XmlInputKind.TextReader;
            input.TextReader = reader;

            XmlDocument document = new XmlParser().ParseXmlDocument(input, CreateOptions(), null, null, null);

            AssertBoundaryDocument(document, "character boundary " + boundary);
            Assert.That(reader.ReadCallCount, Is.GreaterThanOrEqualTo(2), "character boundary " + boundary);
            boundary = boundary + 1;
        }
    }

    [Test]
    public void TextReaderDecoderCollectsRepeatedShortCharacterBlocks()
    {
        string text = CreateBoundaryDocument("UTF-8");
        SingleCharacterTextReader reader = new SingleCharacterTextReader(text);
        XmlInput input = new XmlInput();
        input.Kind = XmlInputKind.TextReader;
        input.TextReader = reader;

        XmlDocument document = new XmlParser().ParseXmlDocument(input, CreateOptions(), null, null, null);

        AssertBoundaryDocument(document, "single-character blocks");
        Assert.That(reader.ReadCallCount, Is.EqualTo(text.Length + 1));
    }

    private static void AssertEveryByteBoundary(Encoding encoding, string declaredEncoding)
    {
        string text = CreateBoundaryDocument(declaredEncoding);
        byte[] preamble = encoding.GetPreamble();
        byte[] payload = encoding.GetBytes(text);
        byte[] bytes = new byte[preamble.Length + payload.Length];
        Array.Copy(preamble, 0, bytes, 0, preamble.Length);
        Array.Copy(payload, 0, bytes, preamble.Length, payload.Length);

        int boundary = 1;
        while (boundary < bytes.Length)
        {
            BoundaryByteStream stream = new BoundaryByteStream(bytes, boundary);
            XmlInput input = new XmlInput();
            input.Kind = XmlInputKind.Stream;
            input.ByteStream = stream;

            XmlDocument document = new XmlParser().ParseXmlDocument(input, CreateOptions(), null, null, null);

            AssertBoundaryDocument(document, encoding.WebName + " byte boundary " + boundary);
            Assert.That(stream.ReadCallCount, Is.GreaterThanOrEqualTo(2), encoding.WebName + " byte boundary " + boundary);
            boundary = boundary + 1;
        }
    }

    private static string CreateBoundaryDocument(string encodingName)
    {
        return "<?xml version='1.0' encoding='" + encodingName + "'?><!--before--><root emoji='&#x1F600;' amp='&amp;'><![CDATA[left]]><?pi data?>\u00E9\U0001F600&amp;<child>tail</child></root><!--after-->";
    }

    private static XmlParserOptions CreateOptions()
    {
        XmlParserOptions options = new XmlParserOptions();
        options.Version = XmlVersion.Xml10;
        options.NamespaceHandling = XmlNamespaceHandling.Disabled;
        options.DtdProcessing = XmlDtdProcessing.Prohibit;
        options.EntityHandling = XmlEntityHandling.Expand;
        options.ValidationMode = XmlValidationMode.None;
        options.PreserveCData = true;
        options.PreserveComments = true;
        options.PreserveProcessingInstructions = true;
        return options;
    }

    private static void AssertBoundaryDocument(XmlDocument document, string message)
    {
        Assert.That(document.Version, Is.EqualTo(XmlVersion.Xml10), message);
        Assert.That(document.DocumentNode!.Children, Has.Length.EqualTo(3), message);
        Assert.That(document.DocumentNode.Children![0].Kind, Is.EqualTo(XmlNodeKind.Comment), message);
        Assert.That(document.DocumentNode.Children[2].Kind, Is.EqualTo(XmlNodeKind.Comment), message);
        XmlNode root = document.DocumentElement!;
        Assert.That(root.Name!.QualifiedName, Is.EqualTo("root"), message);
        Assert.That(root.Attributes, Has.Length.EqualTo(2), message);
        Assert.That(root.Attributes![0].Value, Is.EqualTo("\U0001F600"), message);
        Assert.That(root.Attributes[1].Value, Is.EqualTo("&"), message);
        Assert.That(root.Children, Has.Length.EqualTo(4), message);
        Assert.That(root.Children![0].Kind, Is.EqualTo(XmlNodeKind.CData), message);
        Assert.That(root.Children[0].Value, Is.EqualTo("left"), message);
        Assert.That(root.Children[1].Kind, Is.EqualTo(XmlNodeKind.ProcessingInstruction), message);
        Assert.That(root.Children[1].Name!.QualifiedName, Is.EqualTo("pi"), message);
        Assert.That(root.Children[2].Value, Is.EqualTo("\u00E9\U0001F600&"), message);
        Assert.That(root.Children[3].Name!.QualifiedName, Is.EqualTo("child"), message);
        Assert.That(root.Children[3].Children![0].Value, Is.EqualTo("tail"), message);
    }

    private sealed class BoundaryByteStream : MemoryStream
    {
        internal int Boundary;
        internal int ReadCallCount;

        internal BoundaryByteStream(byte[] bytes, int boundary) : base(bytes)
        {
            Boundary = boundary;
            ReadCallCount = 0;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ReadCallCount = ReadCallCount + 1;
            if (Position < Boundary && Position + count > Boundary)
            {
                count = Boundary - (int)Position;
            }

            return base.Read(buffer, offset, count);
        }
    }

    private sealed class BoundaryTextReader : StringReader
    {
        internal int Boundary;
        internal int Position;
        internal int ReadCallCount;

        internal BoundaryTextReader(string text, int boundary) : base(text)
        {
            Boundary = boundary;
            Position = 0;
            ReadCallCount = 0;
        }

        public override int Read(char[] buffer, int index, int count)
        {
            ReadCallCount = ReadCallCount + 1;
            if (Position < Boundary && Position + count > Boundary)
            {
                count = Boundary - Position;
            }

            int actual = base.Read(buffer, index, count);
            Position = Position + actual;
            return actual;
        }
    }

    private sealed class SingleCharacterTextReader : StringReader
    {
        internal int ReadCallCount;

        internal SingleCharacterTextReader(string text) : base(text)
        {
            ReadCallCount = 0;
        }

        public override int Read(char[] buffer, int index, int count)
        {
            ReadCallCount = ReadCallCount + 1;
            return base.Read(buffer, index, count > 1 ? 1 : count);
        }
    }
}
