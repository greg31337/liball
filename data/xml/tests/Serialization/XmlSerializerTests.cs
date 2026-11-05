using xml;

namespace xml.Tests;

[TestFixture]
public class XmlSerializerTests
{
    [Test]
    public void SerializesEveryPublicInputFormThroughTheSelectedMethod()
    {
        XmlDocument document = Parse("<root>value</root>", XmlVersion.Xml10);
        XmlSerializationOptions xmlOptions = Options(XmlSerializationMethod.Xml);
        xmlOptions.OmitXmlDeclaration = true;
        XmlSerializationOptions textOptions = Options(XmlSerializationMethod.Text);
        XmlSerializationOptions jsonOptions = Options(XmlSerializationMethod.Json);
        XdmSequence sequence = AtomicSequence("value");
        XmlParserOptions eventOptions = ParserOptions(XmlVersion.Xml10);
        XmlEventBatch events = new XmlEventEmitter().EmitXmlNodeEvents(document.DocumentNode!, eventOptions);

        string serializedDocument = SerializeDocument(document, xmlOptions);
        string serializedNode = SerializeNode(document.DocumentElement!, textOptions);
        string serializedSequence = SerializeSequence(sequence, jsonOptions);
        string serializedEvents = SerializeEvents(events, xmlOptions);

        Assert.Multiple(() =>
        {
            Assert.That(serializedDocument, Is.EqualTo("<root>value</root>"));
            Assert.That(serializedNode, Is.EqualTo("value"));
            Assert.That(serializedSequence, Is.EqualTo("\"value\""));
            Assert.That(serializedEvents, Is.EqualTo("<root>value</root>"));
        });
    }

    [Test]
    public void ReportsStandardCodesForInvalidParameterDomains()
    {
        XmlDocument document = Parse("<root/>", XmlVersion.Xml10);
        XmlSerializationOptions method = Options((XmlSerializationMethod)99);
        XmlSerializationOptions standalone = Options(XmlSerializationMethod.Xml);
        standalone.Standalone = (XmlStandalone)99;
        XmlSerializationOptions encoding = Options(XmlSerializationMethod.Xml);
        encoding.Encoding = "utf 8";
        XmlSerializationOptions unsupportedEncoding = Options(XmlSerializationMethod.Xml);
        unsupportedEncoding.Encoding = "x-example";
        XmlSerializationOptions emptyNormalization = Options(XmlSerializationMethod.Xml);
        emptyNormalization.NormalizationForm = "";
        XmlSerializationOptions unsupportedNormalization = Options(XmlSerializationMethod.Xml);
        unsupportedNormalization.NormalizationForm = "NFX";

        Assert.Multiple(() =>
        {
            AssertCode(XmlErrorCodes.SEPM0016, () => SerializeDocument(document, method));
            AssertCode(XmlErrorCodes.SEPM0016, () => SerializeDocument(document, standalone));
            AssertCode(XmlErrorCodes.SEPM0016, () => SerializeDocument(document, encoding));
            AssertCode(XmlErrorCodes.SESU0007, () => SerializeDocument(document, unsupportedEncoding));
            AssertCode(XmlErrorCodes.SEPM0016, () => SerializeDocument(document, emptyNormalization));
            AssertCode(XmlErrorCodes.SESU0011, () => SerializeDocument(document, unsupportedNormalization));
        });
    }

    [Test]
    public void ValidatesVersionMediaTypeDoctypeAndExpandedNameParameters()
    {
        XmlDocument document = Parse("<root/>", XmlVersion.Xml10);
        XmlSerializationOptions xmlVersion = Options(XmlSerializationMethod.Xml);
        xmlVersion.Version = "2.0";
        XmlSerializationOptions htmlSyntax = Options(XmlSerializationMethod.Html);
        htmlSyntax.HtmlVersion = "five";
        XmlSerializationOptions htmlSupport = Options(XmlSerializationMethod.Html);
        htmlSupport.HtmlVersion = "6.0";
        XmlSerializationOptions mediaType = Options(XmlSerializationMethod.Xml);
        mediaType.MediaType = "application/xml; charset=utf-8";
        XmlSerializationOptions publicIdentifier = Options(XmlSerializationMethod.Xml);
        publicIdentifier.DoctypePublic = "bad[public]";
        XmlSerializationOptions systemIdentifier = Options(XmlSerializationMethod.Xml);
        systemIdentifier.DoctypeSystem = "both\"quotes'";
        XmlSerializationOptions names = Options(XmlSerializationMethod.Xml);
        names.CDataSectionElements = [new XmlName { LocalName = "bad:name" }];

        Assert.Multiple(() =>
        {
            AssertCode(XmlErrorCodes.SESU0013, () => SerializeDocument(document, xmlVersion));
            AssertCode(XmlErrorCodes.SEPM0016, () => SerializeDocument(document, htmlSyntax));
            AssertCode(XmlErrorCodes.SESU0013, () => SerializeDocument(document, htmlSupport));
            AssertCode(XmlErrorCodes.SEPM0016, () => SerializeDocument(document, mediaType));
            AssertCode(XmlErrorCodes.SEPM0016, () => SerializeDocument(document, publicIdentifier));
            AssertCode(XmlErrorCodes.SEPM0016, () => SerializeDocument(document, systemIdentifier));
            AssertCode(XmlErrorCodes.SEPM0016, () => SerializeDocument(document, names));
        });
    }

    [Test]
    public void EnforcesXmlRootParameterCombinationsAfterSequenceNormalization()
    {
        XmlDocument first = Parse("<one/>", XmlVersion.Xml10);
        XmlDocument second = Parse("<two/>", XmlVersion.Xml10);
        XdmSequence elements = new XdmSequence
        {
            Count = 2,
            ItemKinds = [XdmItemKind.Node, XdmItemKind.Node],
            ItemIndexes = [0, 1],
            Nodes = [first.DocumentElement!, second.DocumentElement!]
        };
        XmlSerializationOptions doctype = Options(XmlSerializationMethod.Xml);
        doctype.DoctypeSystem = "root.dtd";
        XmlSerializationOptions standalone = Options(XmlSerializationMethod.Xml);
        standalone.Standalone = XmlStandalone.Yes;

        Assert.Multiple(() =>
        {
            AssertCode(XmlErrorCodes.SEPM0004, () => SerializeSequence(elements, doctype));
            AssertCode(XmlErrorCodes.SEPM0004, () => SerializeSequence(AtomicSequence("text"), standalone));
        });
    }

    [Test]
    public void EnforcesCrossParameterRulesBeforeWritingOutput()
    {
        XmlDocument document = Parse("<root/>", XmlVersion.Xml10);
        XmlSerializationOptions declaration = Options(XmlSerializationMethod.Xml);
        declaration.OmitXmlDeclaration = true;
        declaration.Version = "1.1";
        declaration.DoctypeSystem = "root.dtd";
        XmlSerializationOptions jsonFallback = Options(XmlSerializationMethod.Json);
        jsonFallback.JsonNodeOutputMethod = XmlSerializationMethod.Json;

        Assert.Multiple(() =>
        {
            AssertCode(XmlErrorCodes.SEPM0009, () => SerializeDocument(document, declaration));
            AssertCode(XmlErrorCodes.SEPM0004, () => SerializeSequence(new XdmSequence { Count = 0, ItemKinds = [], ItemIndexes = [] }, jsonFallback));
        });
    }

    [Test]
    public void RejectsLeadingCombiningCharactersForFullyNormalizedOutput()
    {
        XmlSerializationOptions options = Options(XmlSerializationMethod.Text);
        options.NormalizationForm = "fully-normalized";

        AssertCode(XmlErrorCodes.SERE0012, () => SerializeSequence(AtomicSequence("\u0301value"), options));
    }

    [Test]
    public void ReportsStandardCodesForXmlWellFormednessNameAndCharacterFailures()
    {
        XmlDocument invalidName = Parse("<root/>", XmlVersion.Xml10);
        invalidName.DocumentElement!.Name!.LocalName = "bad:name";
        XmlDocument invalidCharacter = Parse("<root>value</root>", XmlVersion.Xml10);
        invalidCharacter.DocumentElement!.Children![0].Value = "bad\0value";
        XmlSerializationOptions options = Options(XmlSerializationMethod.Xml);
        options.OmitXmlDeclaration = true;
        XmlSerializationOptions doctype = Options(XmlSerializationMethod.Xml);
        doctype.OmitXmlDeclaration = true;
        doctype.DoctypeSystem = "root.dtd";

        Assert.Multiple(() =>
        {
            AssertCode(XmlErrorCodes.SERE0005, () => SerializeDocument(invalidName, options));
            AssertCode(XmlErrorCodes.SERE0006, () => SerializeDocument(invalidCharacter, options));
            AssertCode(XmlErrorCodes.SERE0003, () => SerializeSequence(new XdmSequence { Count = 0, ItemKinds = [], ItemIndexes = [] }, doctype));
        });
    }

    private static void AssertCode(string code, TestDelegate action)
    {
        XmlSerializationException exception = Assert.Throws<XmlSerializationException>(action)!;
        Assert.That(exception.Diagnostic!.Code, Is.EqualTo(code));
    }

    private static XmlSerializationOptions Options(XmlSerializationMethod method)
    {
        return new XmlSerializationOptions { Method = method, InvalidCharacterPolicy = XmlInvalidCharacterPolicy.Error };
    }

    private static XdmSequence AtomicSequence(string value)
    {
        return new XdmSequence
        {
            Count = 1,
            ItemKinds = [XdmItemKind.Atomic],
            ItemIndexes = [0],
            AtomicValues = [new XdmAtomicValue { Type = XdmAtomicType.String, StringValue = value }]
        };
    }

    private static string SerializeDocument(XmlDocument document, XmlSerializationOptions options)
    {
        StringWriter writer = new StringWriter();
        new XmlSerializer().SerializeXmlDocument(document, new XmlOutput { Kind = XmlOutputKind.TextWriter, TextWriter = writer }, options);
        return writer.ToString();
    }

    private static string SerializeNode(XmlNode node, XmlSerializationOptions options)
    {
        StringWriter writer = new StringWriter();
        new XmlSerializer().SerializeXmlNode(node, new XmlOutput { Kind = XmlOutputKind.TextWriter, TextWriter = writer }, options);
        return writer.ToString();
    }

    private static string SerializeSequence(XdmSequence sequence, XmlSerializationOptions options)
    {
        StringWriter writer = new StringWriter();
        new XmlSerializer().SerializeXdmSequence(sequence, new XmlOutput { Kind = XmlOutputKind.TextWriter, TextWriter = writer }, options);
        return writer.ToString();
    }

    private static string SerializeEvents(XmlEventBatch events, XmlSerializationOptions options)
    {
        StringWriter writer = new StringWriter();
        new XmlSerializer().SerializeXmlEvents(events, new XmlOutput { Kind = XmlOutputKind.TextWriter, TextWriter = writer }, options);
        return writer.ToString();
    }

    private static XmlDocument Parse(string source, XmlVersion version)
    {
        XmlInput input = new XmlInput { Kind = XmlInputKind.String, Text = source };
        return new XmlParser().ParseXmlDocument(input, ParserOptions(version), null, null, null);
    }

    private static XmlParserOptions ParserOptions(XmlVersion version)
    {
        return new XmlParserOptions
        {
            Version = version,
            NamespaceHandling = version == XmlVersion.Xml11 ? XmlNamespaceHandling.Namespaces11 : XmlNamespaceHandling.Namespaces10,
            DtdProcessing = XmlDtdProcessing.Prohibit,
            EntityHandling = XmlEntityHandling.Expand,
            ValidationMode = XmlValidationMode.None,
            PreserveComments = true,
            PreserveProcessingInstructions = true,
            RetainNamespaceDeclarations = true
        };
    }
}
