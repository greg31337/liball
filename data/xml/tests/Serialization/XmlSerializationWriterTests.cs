using System.Text;
using xml;

namespace xml.Tests;

[TestFixture]
public class XmlSerializationWriterTests
{
    [Test]
    public void WritesVersionEncodingStandaloneDoctypeAndEmptyElement()
    {
        XmlDocument document = Parse("<root/>", XmlVersion.Xml10);
        XmlSerializationOptions options = XmlOptions();
        options.Version = "1.1";
        options.Standalone = XmlStandalone.Yes;
        options.DoctypePublic = "-//EXAMPLE//DTD ROOT 1.0//EN";
        options.DoctypeSystem = "urn:example:root.dtd";

        string result = SerializeDocument(document, options);

        Assert.That(result, Is.EqualTo("<?xml version=\"1.1\" encoding=\"utf-16\" standalone=\"yes\"?><!DOCTYPE root PUBLIC \"-//EXAMPLE//DTD ROOT 1.0//EN\" \"urn:example:root.dtd\"><root/>"));
    }

    [Test]
    public void FixesNamespacesAndEscapesAttributeAndTextValues()
    {
        XmlDocument document = Parse("<p:root xmlns:p=\"urn:parent\"><child xmlns=\"urn:child\" p:value=\"&quot; &amp;\">a &lt; b &amp; c</child></p:root>", XmlVersion.Xml10);
        XmlSerializationOptions options = XmlOptions();
        options.OmitXmlDeclaration = true;

        string result = SerializeDocument(document, options);

        Assert.That(result, Is.EqualTo("<p:root xmlns:p=\"urn:parent\"><child xmlns=\"urn:child\" p:value=\"&quot; &amp;\">a &lt; b &amp; c</child></p:root>"));
    }

    [Test]
    public void ExcludesConfiguredCDataContentFromCharacterMapsAndSplitsTerminators()
    {
        XmlDocument document = Parse("<root>café]]&gt;x</root>", XmlVersion.Xml10);
        XmlSerializationOptions options = XmlOptions();
        options.OmitXmlDeclaration = true;
        options.CDataSectionElements = [Name(null, "root")];
        options.UseCharacterMaps = [new XmlCharacterMapEntry { CodePoint = 0xE9, Replacement = "<accent/>" }];

        string result = SerializeDocument(document, options);

        Assert.That(result, Is.EqualTo("<root><![CDATA[café]]]]><![CDATA[>x]]></root>"));
    }

    [Test]
    public void MergesNamedMapsBeforeNormalizationAndKeepsReplacementsAtomic()
    {
        XmlDocument document = Parse("<root>éxy</root>", XmlVersion.Xml10);
        XmlSerializationOptions options = XmlOptions();
        options.OmitXmlDeclaration = true;
        options.NormalizationForm = "NFC";
        options.UseCharacterMaps =
        [
            new XmlCharacterMapEntry { CodePoint = 0x65, Replacement = "<first/>", MapName = Name("urn:maps", "first") },
            new XmlCharacterMapEntry { CodePoint = 0x78, Replacement = "ý", MapName = Name("urn:maps", "first") },
            new XmlCharacterMapEntry { CodePoint = 0x79, Replacement = "<mapped-y/>", MapName = Name("urn:maps", "second") },
            new XmlCharacterMapEntry { CodePoint = 0x65, Replacement = "<second/>", MapName = Name("urn:maps", "second") }
        ];

        string result = SerializeDocument(document, options);

        Assert.That(result, Is.EqualTo("<root><second/>́ý<mapped-y/></root>"));
    }

    [Test]
    public void DoesNotApplyCharacterMapsToNamespaceBindings()
    {
        XmlDocument document = Parse("<p:root xmlns:p=\"urn:x\"/>", XmlVersion.Xml10);
        XmlSerializationOptions options = XmlOptions();
        options.OmitXmlDeclaration = true;
        options.UseCharacterMaps = [new XmlCharacterMapEntry { CodePoint = 0x78, Replacement = "<mapped/>" }];

        string result = SerializeDocument(document, options);

        Assert.That(result, Is.EqualTo("<p:root xmlns:p=\"urn:x\"/>"));
    }

    [Test]
    public void RejectsMalformedCharacterMapReplacements()
    {
        XmlDocument document = Parse("<root>x</root>", XmlVersion.Xml10);
        XmlSerializationOptions options = XmlOptions();
        options.OmitXmlDeclaration = true;
        options.UseCharacterMaps = [new XmlCharacterMapEntry { CodePoint = 0x78, Replacement = "bad\uD800replacement" }];

        XmlSerializationException exception = Assert.Throws<XmlSerializationException>(() => SerializeDocument(document, options))!;

        Assert.That(exception.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.SERE0008));
    }

    [Test]
    public void RejectsCharacterMapReplacementsNotRepresentableInTheOutputEncoding()
    {
        XmlDocument document = Parse("<root>x</root>", XmlVersion.Xml10);
        MemoryStream stream = new MemoryStream();
        XmlOutput output = new XmlOutput { Kind = XmlOutputKind.Stream, ByteStream = stream, EncodingName = "us-ascii" };
        XmlSerializationOptions options = XmlOptions();
        options.OmitXmlDeclaration = true;
        options.UseCharacterMaps = [new XmlCharacterMapEntry { CodePoint = 0x78, Replacement = "é" }];

        XmlSerializationException exception = Assert.Throws<XmlSerializationException>(() => new XmlSerializer().SerializeXmlDocument(document, output, options))!;

        Assert.That(exception.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.SERE0008));
    }

    [Test]
    public void IndentsMarkupOnlyContentAndHonorsSuppression()
    {
        XmlDocument document = Parse("<root><group><item/></group><raw><item/></raw></root>", XmlVersion.Xml10);
        XmlSerializationOptions options = XmlOptions();
        options.OmitXmlDeclaration = true;
        options.Indent = true;
        options.NewLine = "\r\n";
        options.SuppressIndentation = [Name(null, "raw")];

        string result = SerializeDocument(document, options);

        Assert.That(result, Is.EqualTo("<root>\r\n  <group>\r\n    <item/>\r\n  </group>\r\n  <raw><item/></raw>\r\n</root>"));
    }

    [Test]
    public void NormalizesTextAndAttributeContent()
    {
        XmlDocument document = Parse("<root value=\"é\">é</root>", XmlVersion.Xml10);
        XmlSerializationOptions options = XmlOptions();
        options.OmitXmlDeclaration = true;
        options.NormalizationForm = "NFC";

        string result = SerializeDocument(document, options);

        Assert.That(result, Is.EqualTo("<root value=\"é\">é</root>"));
    }

    [Test]
    public void EmitsXml11PrefixUndeclarationsOnlyWhenRequested()
    {
        XmlDocument document = Parse("<?xml version=\"1.1\"?><root xmlns:p=\"urn:p\"><child xmlns:p=\"\"/></root>", XmlVersion.Xml11);
        XmlSerializationOptions withUndeclarations = XmlOptions();
        withUndeclarations.Version = "1.1";
        withUndeclarations.OmitXmlDeclaration = true;
        withUndeclarations.UndeclarePrefixes = true;
        XmlSerializationOptions withoutUndeclarations = XmlOptions();
        withoutUndeclarations.Version = "1.1";
        withoutUndeclarations.OmitXmlDeclaration = true;

        string emitted = SerializeDocument(document, withUndeclarations);
        string suppressed = SerializeDocument(document, withoutUndeclarations);

        Assert.Multiple(() =>
        {
            Assert.That(emitted, Is.EqualTo("<root xmlns:p=\"urn:p\"><child xmlns:p=\"\"/></root>"));
            Assert.That(suppressed, Is.EqualTo("<root xmlns:p=\"urn:p\"><child/></root>"));
        });
    }

    [Test]
    public void SerializesNormalizedSequencesToTheRequestedByteEncoding()
    {
        XdmSequence sequence = new XdmSequence
        {
            Count = 2,
            ItemKinds = [XdmItemKind.Atomic, XdmItemKind.Atomic],
            ItemIndexes = [0, 1],
            AtomicValues =
            [
                new XdmAtomicValue { Type = XdmAtomicType.String, StringValue = "one" },
                new XdmAtomicValue { Type = XdmAtomicType.String, StringValue = "two" }
            ]
        };
        MemoryStream stream = new MemoryStream();
        XmlOutput output = new XmlOutput { Kind = XmlOutputKind.Stream, ByteStream = stream };
        XmlSerializationOptions options = XmlOptions();
        options.Encoding = "utf-8";
        options.ByteOrderMark = true;
        options.ItemSeparator = "|";

        new XmlSerializer().SerializeXdmSequence(sequence, output, options);

        byte[] expected = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes("<?xml version=\"1.0\" encoding=\"utf-8\"?>one|two")).ToArray();
        Assert.That(stream.ToArray(), Is.EqualTo(expected));
    }

    [Test]
    public void RejectsIncompatibleXmlSerializationParameters()
    {
        XmlDocument document = Parse("<root/>", XmlVersion.Xml10);
        XmlSerializationOptions undeclare = XmlOptions();
        undeclare.UndeclarePrefixes = true;
        XmlSerializationOptions standalone = XmlOptions();
        standalone.OmitXmlDeclaration = true;
        standalone.Standalone = XmlStandalone.No;
        XmlSerializationOptions publicWithoutSystem = XmlOptions();
        publicWithoutSystem.OmitXmlDeclaration = true;
        publicWithoutSystem.DoctypePublic = "public";

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<XmlSerializationException>(() => SerializeDocument(document, undeclare))!.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.SEPM0010));
            Assert.That(Assert.Throws<XmlSerializationException>(() => SerializeDocument(document, standalone))!.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.SEPM0009));
            Assert.That(SerializeDocument(document, publicWithoutSystem), Is.EqualTo("<root/>"));
        });
    }

    private static XmlSerializationOptions XmlOptions()
    {
        return new XmlSerializationOptions
        {
            Method = XmlSerializationMethod.Xml,
            InvalidCharacterPolicy = XmlInvalidCharacterPolicy.Error
        };
    }

    private static XmlName Name(string? namespaceUri, string localName)
    {
        return new XmlName { LocalName = localName, NamespaceUri = namespaceUri, QualifiedName = localName };
    }

    private static string SerializeDocument(XmlDocument document, XmlSerializationOptions options)
    {
        StringWriter writer = new StringWriter();
        XmlOutput output = new XmlOutput { Kind = XmlOutputKind.TextWriter, TextWriter = writer };
        new XmlSerializer().SerializeXmlDocument(document, output, options);
        return writer.ToString();
    }

    private static XmlDocument Parse(string source, XmlVersion version)
    {
        XmlInput input = new XmlInput { Kind = XmlInputKind.String, Text = source };
        XmlParserOptions options = new XmlParserOptions
        {
            Version = version,
            NamespaceHandling = version == XmlVersion.Xml11 ? XmlNamespaceHandling.Namespaces11 : XmlNamespaceHandling.Namespaces10,
            DtdProcessing = XmlDtdProcessing.Prohibit,
            EntityHandling = XmlEntityHandling.Expand,
            ValidationMode = XmlValidationMode.None,
            PreservePrefixes = true,
            PreserveCData = true,
            PreserveComments = true,
            PreserveProcessingInstructions = true,
            RetainNamespaceDeclarations = true
        };
        return new XmlParser().ParseXmlDocument(input, options, null, null, null);
    }
}
