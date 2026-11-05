using System.Text;
using xml;

namespace xml.Tests;

[TestFixture]
public class TextSerializationWriterTests
{
    [Test]
    public void ExtractsDescendantTextAndIgnoresMarkupCommentsAndInstructions()
    {
        XmlDocument document = Parse("<root>before<!--ignored--><?work ignored?><child>A<![CDATA[B]]></child>after</root>");
        XmlSerializationOptions options = TextOptions();

        string result = SerializeDocument(document, options);

        Assert.That(result, Is.EqualTo("beforeABafter"));
    }

    [Test]
    public void ExtractsTheStringValueOfASerializedNode()
    {
        XmlDocument document = Parse("<root><selected>one<inner>two</inner>three</selected><ignored>four</ignored></root>");
        StringWriter writer = new StringWriter();
        XmlOutput output = new XmlOutput { Kind = XmlOutputKind.TextWriter, TextWriter = writer };

        new XmlSerializer().SerializeXmlNode(document.DocumentElement!.Children![0], output, TextOptions());

        Assert.That(writer.ToString(), Is.EqualTo("onetwothree"));
    }

    [Test]
    public void PreservesExplicitItemSeparatorsAcrossAtomicAndNodeItems()
    {
        XmlDocument document = Parse("<middle>node</middle>");
        XdmSequence sequence = new XdmSequence
        {
            Count = 3,
            ItemKinds = [XdmItemKind.Atomic, XdmItemKind.Node, XdmItemKind.Atomic],
            ItemIndexes = [0, 0, 1],
            AtomicValues = [Atomic("left"), Atomic("right")],
            Nodes = [document.DocumentElement!]
        };
        XmlSerializationOptions options = TextOptions();
        options.ItemSeparator = "|";

        string result = SerializeSequence(sequence, options);

        Assert.That(result, Is.EqualTo("left|node|right"));
    }

    [Test]
    public void UsesDefaultSpacingBetweenAdjacentAtomicValues()
    {
        XdmSequence sequence = new XdmSequence
        {
            Count = 2,
            ItemKinds = [XdmItemKind.Atomic, XdmItemKind.Atomic],
            ItemIndexes = [0, 1],
            AtomicValues = [Atomic("one"), Atomic("two")]
        };

        string result = SerializeSequence(sequence, TextOptions());

        Assert.That(result, Is.EqualTo("one two"));
    }

    [Test]
    public void NormalizesAcrossDescendantNodeBoundariesAndConvertsLineFeeds()
    {
        XmlDocument document = Parse("<root>e<part>́</part>a\nb&#xD;c</root>");
        XmlSerializationOptions options = TextOptions();
        options.NormalizationForm = "NFC";
        options.NewLine = "\r\n";

        string result = SerializeDocument(document, options);

        Assert.That(result, Is.EqualTo("éa\r\nb\rc"));
    }

    [Test]
    public void AppliesCharacterMapsWithoutMarkupEscaping()
    {
        XmlDocument document = Parse("<root>café &amp; tea</root>");
        XmlSerializationOptions options = TextOptions();
        options.UseCharacterMaps =
        [
            new XmlCharacterMapEntry { CodePoint = 0xE9, Replacement = "[accent]" },
            new XmlCharacterMapEntry { CodePoint = 0x26, Replacement = "<amp>" }
        ];

        string result = SerializeDocument(document, options);

        Assert.That(result, Is.EqualTo("caf[accent] <amp> tea"));
    }

    [Test]
    public void AppliesMapsBeforeNormalizationAndLeavesReplacementNewlinesUntouched()
    {
        XmlDocument document = Parse("<root>éx\ny</root>");
        XmlSerializationOptions options = TextOptions();
        options.NormalizationForm = "NFC";
        options.NewLine = "\r\n";
        options.UseCharacterMaps =
        [
            new XmlCharacterMapEntry { CodePoint = 0x65, Replacement = "[e]" },
            new XmlCharacterMapEntry { CodePoint = 0x78, Replacement = "ý\n" },
            new XmlCharacterMapEntry { CodePoint = 0x79, Replacement = "[y]" }
        ];

        string result = SerializeDocument(document, options);

        Assert.That(result, Is.EqualTo("[e]́ý\n\r\n[y]"));
    }

    [Test]
    public void UsesRequestedByteEncodingAndByteOrderMarkWithoutADeclaration()
    {
        XmlDocument document = Parse("<root>café</root>");
        MemoryStream stream = new MemoryStream();
        XmlOutput output = new XmlOutput { Kind = XmlOutputKind.Stream, ByteStream = stream };
        XmlSerializationOptions options = TextOptions();
        options.Encoding = "utf-16le";
        options.ByteOrderMark = true;

        new XmlSerializer().SerializeXmlDocument(document, output, options);

        byte[] expected = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("café")).ToArray();
        Assert.That(stream.ToArray(), Is.EqualTo(expected));
    }

    [Test]
    public void IgnoresMarkupOnlyParametersForTextOutput()
    {
        XmlDocument document = Parse("<root>value</root>");
        XmlSerializationOptions options = TextOptions();
        options.Indent = true;
        options.OmitXmlDeclaration = false;
        options.Standalone = XmlStandalone.Yes;
        options.DoctypePublic = "public";
        options.DoctypeSystem = "system";
        options.IncludeContentType = true;
        options.UndeclarePrefixes = true;

        string result = SerializeDocument(document, options);

        Assert.That(result, Is.EqualTo("value"));
    }

    [Test]
    public void RejectsMalformedUnicodeAndUnsupportedNormalizationForms()
    {
        XdmSequence malformed = new XdmSequence
        {
            Count = 1,
            ItemKinds = [XdmItemKind.Atomic],
            ItemIndexes = [0],
            AtomicValues = [Atomic("bad\uD800value")]
        };
        XmlSerializationOptions invalidNormalization = TextOptions();
        invalidNormalization.NormalizationForm = "unknown";

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<XmlSerializationException>(() => SerializeSequence(malformed, TextOptions()))!.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.SERE0008));
            Assert.That(Assert.Throws<XmlSerializationException>(() => SerializeSequence(new XdmSequence { Count = 0, ItemKinds = [], ItemIndexes = [] }, invalidNormalization))!.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.SESU0011));
        });
    }

    private static XdmAtomicValue Atomic(string value)
    {
        return new XdmAtomicValue { Type = XdmAtomicType.String, StringValue = value };
    }

    private static XmlSerializationOptions TextOptions()
    {
        return new XmlSerializationOptions { Method = XmlSerializationMethod.Text };
    }

    private static string SerializeDocument(XmlDocument document, XmlSerializationOptions options)
    {
        StringWriter writer = new StringWriter();
        XmlOutput output = new XmlOutput { Kind = XmlOutputKind.TextWriter, TextWriter = writer };
        new XmlSerializer().SerializeXmlDocument(document, output, options);
        return writer.ToString();
    }

    private static string SerializeSequence(XdmSequence sequence, XmlSerializationOptions options)
    {
        StringWriter writer = new StringWriter();
        XmlOutput output = new XmlOutput { Kind = XmlOutputKind.TextWriter, TextWriter = writer };
        new XmlSerializer().SerializeXdmSequence(sequence, output, options);
        return writer.ToString();
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
            PreserveCData = true,
            PreserveComments = true,
            PreserveProcessingInstructions = true
        };
        return new XmlParser().ParseXmlDocument(input, options, null, null, null);
    }
}
