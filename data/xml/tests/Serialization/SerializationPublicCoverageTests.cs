using System.Text;
using xml;

namespace xml.Tests;

[TestFixture]
public class SerializationPublicCoverageTests
{
    [Test]
    public void DispatchesEveryOutputMethodAndEveryBuiltInExtensionName()
    {
        XdmSequence sequence = AtomicSequence("value");
        XmlSerializationMethod[] methods =
        [
            XmlSerializationMethod.Xml,
            XmlSerializationMethod.Xhtml,
            XmlSerializationMethod.Html,
            XmlSerializationMethod.Text,
            XmlSerializationMethod.Json,
            XmlSerializationMethod.Adaptive
        ];
        string[] extensionNames = ["xml", "xhtml", "html", "text", "json", "adaptive"];
        string[] expected = ["value", "value", "value", "value", "\"value\"", "\"value\""];

        Assert.Multiple(() =>
        {
            for (int index = 0; index < methods.Length; index++)
            {
                XmlSerializationOptions direct = Options(methods[index]);
                direct.OmitXmlDeclaration = true;
                Assert.That(SerializeSequence(sequence, direct), Is.EqualTo(expected[index]), methods[index].ToString());

                XmlSerializationOptions extension = Options(XmlSerializationMethod.Extension);
                extension.ExtensionMethodName = Name(null, extensionNames[index]);
                extension.OmitXmlDeclaration = true;
                Assert.That(SerializeSequence(sequence, extension), Is.EqualTo(expected[index]), "extension " + extensionNames[index]);
            }
        });
    }

    [Test]
    public void NormalizesEverySequenceShapeThroughThePublicSerializer()
    {
        XmlDocument middle = Parse("<middle/>");
        XdmSequence empty = new XdmSequence { Count = 0, ItemKinds = [], ItemIndexes = [] };
        XdmSequence adjacent = new XdmSequence
        {
            Count = 2,
            ItemKinds = [XdmItemKind.Atomic, XdmItemKind.Atomic],
            ItemIndexes = [0, 1],
            AtomicValues = [Atomic("one"), Atomic("two")]
        };
        XdmArrayValue nested = new XdmArrayValue
        {
            Count = 2,
            Members = [AtomicSequence("one"), AtomicSequence("two")]
        };
        XdmSequence mixed = new XdmSequence
        {
            Count = 3,
            ItemKinds = [XdmItemKind.Array, XdmItemKind.Node, XdmItemKind.Atomic],
            ItemIndexes = [0, 0, 0],
            AtomicValues = [Atomic("three")],
            Nodes = [middle.DocumentNode!],
            Arrays = [nested]
        };
        XdmSequence function = new XdmSequence
        {
            Count = 1,
            ItemKinds = [XdmItemKind.Function],
            ItemIndexes = [0],
            Functions = [new XdmFunctionItem { Name = Name(null, "f"), Arity = 0 }]
        };
        XmlSerializationOptions compact = Options(XmlSerializationMethod.Xml);
        compact.OmitXmlDeclaration = true;
        XmlSerializationOptions separated = Options(XmlSerializationMethod.Xml);
        separated.OmitXmlDeclaration = true;
        separated.ItemSeparator = "|";

        Assert.Multiple(() =>
        {
            Assert.That(SerializeSequence(empty, compact), Is.Empty);
            Assert.That(SerializeSequence(AtomicSequence("one"), compact), Is.EqualTo("one"));
            Assert.That(SerializeSequence(adjacent, compact), Is.EqualTo("one two"));
            Assert.That(SerializeSequence(mixed, separated), Is.EqualTo("one|two|<middle/>|three"));
            AssertCode(XmlErrorCodes.SENR0001, () => SerializeSequence(function, compact));
        });
    }

    [TestCase("utf-8")]
    [TestCase("utf-16le")]
    [TestCase("utf-16be")]
    [TestCase("utf-32le")]
    [TestCase("utf-32be")]
    [TestCase("us-ascii")]
    [TestCase("iso-8859-1")]
    public void WritesEachSupportedEncodingFamily(string encodingName)
    {
        MemoryStream stream = new MemoryStream();
        XmlOutput output = new XmlOutput { Kind = XmlOutputKind.Stream, ByteStream = stream };
        XmlSerializationOptions options = Options(XmlSerializationMethod.Text);
        options.Encoding = encodingName;
        options.ByteOrderMark = true;

        new XmlSerializer().SerializeXdmSequence(AtomicSequence("AZ"), output, options);

        Encoding encoding = Encoding.GetEncoding(encodingName);
        byte[] expected = encoding.GetPreamble().Concat(encoding.GetBytes("AZ")).ToArray();
        Assert.That(stream.ToArray(), Is.EqualTo(expected));
    }

    [Test]
    public void WritesEveryOutputKindAndHonorsDestinationEncodingPrecedence()
    {
        XdmSequence sequence = AtomicSequence("AZ");
        XmlSerializationOptions options = Options(XmlSerializationMethod.Text);
        options.Encoding = "utf-8";

        StringWriter text = new StringWriter();
        XmlOutput textOutput = new XmlOutput
        {
            Kind = XmlOutputKind.TextWriter,
            TextWriter = text,
            BaseUri = "urn:output:text",
            MediaType = "text/plain"
        };
        MemoryStream stream = new MemoryStream();
        XmlOutput streamOutput = new XmlOutput
        {
            Kind = XmlOutputKind.Stream,
            ByteStream = stream,
            EncodingName = "utf-16be",
            BaseUri = "urn:output:stream",
            MediaType = "application/xml"
        };
        XmlByteCollector collector = new XmlByteCollector();
        XmlOutput sinkOutput = new XmlOutput
        {
            Kind = XmlOutputKind.ByteSink,
            ByteSink = collector,
            Encoding = Encoding.Unicode,
            EncodingName = "utf-32",
            BaseUri = "urn:output:sink",
            MediaType = "application/octet-stream"
        };

        XmlSerializer serializer = new XmlSerializer();
        serializer.SerializeXdmSequence(sequence, textOutput, options);
        serializer.SerializeXdmSequence(sequence, streamOutput, options);
        serializer.SerializeXdmSequence(sequence, sinkOutput, options);

        Assert.Multiple(() =>
        {
            Assert.That(text.ToString(), Is.EqualTo("AZ"));
            Assert.That(stream.ToArray(), Is.EqualTo(Encoding.BigEndianUnicode.GetBytes("AZ")));
            Assert.That(collector.SnapshotCollectedXmlBytes(), Is.EqualTo(Encoding.Unicode.GetBytes("AZ")));
        });
    }

    [Test]
    public void RepairsElementAndAttributeNamespacesOnTheNormalizedCopy()
    {
        XmlDocument document = Parse("<root a=\"value\"/>");
        XmlNode root = document.DocumentElement!;
        root.Name = Name("urn:root", "root", "p");
        root.NamespaceBindings = [];
        root.Attributes![0].Name = Name("urn:attribute", "a", "q");
        XmlSerializationOptions options = Options(XmlSerializationMethod.Xml);
        options.OmitXmlDeclaration = true;

        string result = SerializeDocument(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo("<p:root xmlns:p=\"urn:root\" xmlns:q=\"urn:attribute\" q:a=\"value\"/>"));
            Assert.That(root.NamespaceBindings, Is.Empty);
            Assert.That(root.Name!.Prefix, Is.EqualTo("p"));
            Assert.That(root.Attributes![0].Name!.Prefix, Is.EqualTo("q"));
        });
    }

    [Test]
    public void UsesTheConfiguredInvalidCharacterReplacementThroughExtensionMethods()
    {
        XmlDocument document = Parse("<root>value</root>");
        document.DocumentElement!.Children![0].Value = "a\0b";
        XmlDocument attributeDocument = Parse("<root a=\"value\"/>");
        attributeDocument.DocumentElement!.Attributes![0].Value = "a\0b";
        XdmSequence attribute = new XdmSequence
        {
            Count = 1,
            ItemKinds = [XdmItemKind.Node],
            ItemIndexes = [0],
            Nodes = [attributeDocument.DocumentElement.Attributes[0]]
        };
        XmlSerializationOptions xml = Options(XmlSerializationMethod.Extension);
        xml.ExtensionMethodName = Name(null, "xml");
        xml.OmitXmlDeclaration = true;
        xml.InvalidCharacterPolicy = XmlInvalidCharacterPolicy.Replace;
        xml.InvalidCharacterReplacement = "[invalid]";
        XmlSerializationOptions adaptive = Options(XmlSerializationMethod.Extension);
        adaptive.ExtensionMethodName = Name(null, "adaptive");
        adaptive.InvalidCharacterPolicy = XmlInvalidCharacterPolicy.Replace;
        adaptive.InvalidCharacterReplacement = "[invalid]";

        string xmlResult = SerializeDocument(document, xml);
        string adaptiveResult = SerializeSequence(attribute, adaptive);

        Assert.Multiple(() =>
        {
            Assert.That(xmlResult, Is.EqualTo("<root>a[invalid]b</root>"));
            Assert.That(adaptiveResult, Is.EqualTo("a=\"a[invalid]b\""));
        });
    }

    [Test]
    public void ReportsPreviouslyUncoveredStandardSerializationCodes()
    {
        XmlDocument document = Parse("<?build a>b?><html/>");
        XmlSerializationOptions invalidMap = Options(XmlSerializationMethod.Xml);
        invalidMap.UseCharacterMaps = [new XmlCharacterMapEntry { CodePoint = 0xD800, Replacement = "value" }];
        XmlSerializationOptions html = Options(XmlSerializationMethod.Html);
        html.HtmlVersion = "4.0";

        Assert.Multiple(() =>
        {
            AssertCode(XmlErrorCodes.SEPM0018, () => SerializeDocument(document, invalidMap));
            AssertCode(XmlErrorCodes.SERE0015, () => SerializeDocument(document, html));
        });
    }

    [Test]
    public void ConvertsEveryDestinationFailureAndCleansUpOwnedResources()
    {
        XdmSequence sequence = AtomicSequence("value");
        XmlSerializationOptions options = Options(XmlSerializationMethod.Text);
        FailingStream stream = new FailingStream();
        FailingWriter writer = new FailingWriter();
        FailingSink sink = new FailingSink();
        XmlSerializer serializer = new XmlSerializer();

        XmlSerializationException streamFailure = Assert.Throws<XmlSerializationException>(() => serializer.SerializeXdmSequence(sequence, new XmlOutput { Kind = XmlOutputKind.Stream, ByteStream = stream, OwnsResource = true }, options))!;
        XmlSerializationException writerFailure = Assert.Throws<XmlSerializationException>(() => serializer.SerializeXdmSequence(sequence, new XmlOutput { Kind = XmlOutputKind.TextWriter, TextWriter = writer, OwnsResource = true }, options))!;
        XmlSerializationException sinkFailure = Assert.Throws<XmlSerializationException>(() => serializer.SerializeXdmSequence(sequence, new XmlOutput { Kind = XmlOutputKind.ByteSink, ByteSink = sink }, options))!;

        Assert.Multiple(() =>
        {
            Assert.That(streamFailure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.GeneralFailure));
            Assert.That(writerFailure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.GeneralFailure));
            Assert.That(sinkFailure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.GeneralFailure));
            Assert.That(stream.WasDisposed, Is.True);
            Assert.That(writer.WasDisposed, Is.True);
            Assert.That(sink.WriteCount, Is.EqualTo(1));
        });
    }

    private static XmlSerializationOptions Options(XmlSerializationMethod method)
    {
        return new XmlSerializationOptions
        {
            Method = method,
            InvalidCharacterPolicy = XmlInvalidCharacterPolicy.Error
        };
    }

    private static XdmAtomicValue Atomic(string value)
    {
        return new XdmAtomicValue { Type = XdmAtomicType.String, StringValue = value };
    }

    private static XdmSequence AtomicSequence(string value)
    {
        return new XdmSequence
        {
            Count = 1,
            ItemKinds = [XdmItemKind.Atomic],
            ItemIndexes = [0],
            AtomicValues = [Atomic(value)]
        };
    }

    private static XmlName Name(string? namespaceUri, string localName, string? prefix = null)
    {
        return new XmlName
        {
            Prefix = prefix,
            LocalName = localName,
            NamespaceUri = namespaceUri,
            QualifiedName = prefix == null ? localName : prefix + ":" + localName
        };
    }

    private static string SerializeDocument(XmlDocument document, XmlSerializationOptions options)
    {
        StringWriter writer = new StringWriter();
        new XmlSerializer().SerializeXmlDocument(document, new XmlOutput { Kind = XmlOutputKind.TextWriter, TextWriter = writer }, options);
        return writer.ToString();
    }

    private static string SerializeSequence(XdmSequence sequence, XmlSerializationOptions options)
    {
        StringWriter writer = new StringWriter();
        new XmlSerializer().SerializeXdmSequence(sequence, new XmlOutput { Kind = XmlOutputKind.TextWriter, TextWriter = writer }, options);
        return writer.ToString();
    }

    private static void AssertCode(string code, TestDelegate action)
    {
        XmlSerializationException exception = Assert.Throws<XmlSerializationException>(action)!;
        Assert.That(exception.Diagnostic!.Code, Is.EqualTo(code));
    }

    private static XmlDocument Parse(string source)
    {
        XmlInput input = new XmlInput { Kind = XmlInputKind.String, Text = source };
        XmlParserOptions options = new XmlParserOptions
        {
            Version = XmlVersion.Xml10,
            NamespaceHandling = XmlNamespaceHandling.Namespaces10,
            DtdProcessing = XmlDtdProcessing.Prohibit,
            EntityHandling = XmlEntityHandling.Expand,
            ValidationMode = XmlValidationMode.None,
            PreservePrefixes = true,
            PreserveComments = true,
            PreserveProcessingInstructions = true,
            RetainNamespaceDeclarations = true
        };
        return new XmlParser().ParseXmlDocument(input, options, null, null, null);
    }

    private sealed class FailingSink : IXmlByteSink
    {
        public int WriteCount;

        public void WriteXmlBytes(ReadOnlyMemory<byte> bytes)
        {
            WriteCount++;
            throw new IOException("sink failure");
        }
    }

    private sealed class FailingStream : MemoryStream
    {
        public bool WasDisposed;

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new IOException("stream failure");
        }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class FailingWriter : StringWriter
    {
        public bool WasDisposed;

        public override void Write(string? value)
        {
            throw new IOException("writer failure");
        }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }
}
