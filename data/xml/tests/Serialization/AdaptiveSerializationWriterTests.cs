using xml;

namespace xml.Tests;

[TestFixture]
public class AdaptiveSerializationWriterTests
{
    [Test]
    public void SerializesEveryXdmItemKindAndNestedSequenceShape()
    {
        XmlDocument document = Parse("<root>node</root>");
        XdmArrayValue array = new XdmArrayValue
        {
            Count = 3,
            Members =
            [
                EmptySequence(),
                AtomicSequence(String("member")),
                AtomicItems(Integer(7), Boolean(false))
            ]
        };
        XdmMapValue map = new XdmMapValue
        {
            Count = 1,
            Entries = [new XdmMapEntry { Key = String("items"), Value = ArraySequence(array) }]
        };
        XdmFunctionItem function = new XdmFunctionItem
        {
            Name = Name(XPathNamespaces.Functions, "exists"),
            Arity = 1
        };
        XdmSequence sequence = new XdmSequence
        {
            Count = 5,
            ItemKinds = [XdmItemKind.Atomic, XdmItemKind.Node, XdmItemKind.Map, XdmItemKind.Array, XdmItemKind.Function],
            ItemIndexes = [0, 0, 0, 0, 0],
            AtomicValues = [String("a\"b")],
            Nodes = [document.DocumentElement!],
            Maps = [map],
            Arrays = [array],
            Functions = [function]
        };
        XmlSerializationOptions options = AdaptiveOptions();
        options.OmitXmlDeclaration = true;

        string result = Serialize(sequence, options);

        Assert.That(result, Is.EqualTo("\"a\"\"b\"\n<root>node</root>\nmap{\"items\":[(),\"member\",(7,false())]}\n[(),\"member\",(7,false())]\nfn:exists#1"));
    }

    [Test]
    public void UsesAdaptiveSpellingsForAtomicCategories()
    {
        XdmAtomicValue qname = new XdmAtomicValue { Type = XdmAtomicType.QName, NameValue = Name("urn:q", "item") };
        XdmAtomicValue date = new XdmAtomicValue { Type = XdmAtomicType.Date, StringValue = "2026-09-12" };
        XdmAtomicValue binary = new XdmAtomicValue { Type = XdmAtomicType.HexBinary, BinaryValue = [0x0A, 0xFF] };
        XdmSequence sequence = AtomicItems(Boolean(true), Decimal(12.5m), Integer(-4), Floating(1.25), Floating(double.NaN), Floating(double.PositiveInfinity), qname, date, binary);
        XmlSerializationOptions options = AdaptiveOptions();
        options.ItemSeparator = "|";

        string result = Serialize(sequence, options);

        Assert.That(result, Is.EqualTo("true()|12.5|-4|1.25e0|NaN|INF|Q{urn:q}item|xs:date(\"2026-09-12\")|xs:hexBinary(\"0AFF\")"));
    }

    [Test]
    public void UsesConventionalAndUriQualifiedFunctionNames()
    {
        XdmSequence sequence = new XdmSequence
        {
            Count = 4,
            ItemKinds = [XdmItemKind.Function, XdmItemKind.Function, XdmItemKind.Function, XdmItemKind.Function],
            ItemIndexes = [0, 1, 2, 3],
            Functions =
            [
                new XdmFunctionItem { Name = Name(XPathNamespaces.Math, "pi"), Arity = 0 },
                new XdmFunctionItem { Name = Name(XPathNamespaces.Map, "get"), Arity = 2 },
                new XdmFunctionItem { Name = Name("urn:custom", "work"), Arity = 3 },
                new XdmFunctionItem { Arity = 1 }
            ]
        };
        XmlSerializationOptions options = AdaptiveOptions();
        options.ItemSeparator = ",";

        string result = Serialize(sequence, options);

        Assert.That(result, Is.EqualTo("math:pi#0,map:get#2,Q{urn:custom}work#3,(anonymous-function)#1"));
    }

    [Test]
    public void SerializesStandaloneAttributeAndNamespaceNodes()
    {
        XmlDocument document = Parse("<root xmlns:p=\"urn:p\" p:value=\"&quot; &amp;\"/>");
        XmlNode attribute = document.DocumentElement!.Attributes!.Single(item => item.Name!.LocalName == "value");
        XmlNode namespaceNode = new XmlNode
        {
            Kind = XmlNodeKind.Namespace,
            Name = Name(null, "p"),
            Value = "urn:p"
        };
        XdmSequence sequence = new XdmSequence
        {
            Count = 2,
            ItemKinds = [XdmItemKind.Node, XdmItemKind.Node],
            ItemIndexes = [0, 1],
            Nodes = [attribute, namespaceNode]
        };
        XmlSerializationOptions options = AdaptiveOptions();
        options.ItemSeparator = "|";

        string result = Serialize(sequence, options);

        Assert.That(result, Is.EqualTo("p:value=\"&quot; &amp;\"|xmlns:p=\"urn:p\""));
    }

    [Test]
    public void AppliesCharacterMapsBeforeNormalizationAndQuoteDoubling()
    {
        XmlDocument document = Parse("<root value=\"x &amp; y\"/>");
        XdmSequence sequence = new XdmSequence
        {
            Count = 2,
            ItemKinds = [XdmItemKind.Atomic, XdmItemKind.Node],
            ItemIndexes = [0, 0],
            AtomicValues = [String("é\"x")],
            Nodes = [document.DocumentElement!.Attributes![0]]
        };
        XmlSerializationOptions options = AdaptiveOptions();
        options.ItemSeparator = "|";
        options.NormalizationForm = "NFC";
        options.UseCharacterMaps = [new XmlCharacterMapEntry { CodePoint = 0x78, Replacement = "<mapped>" }];

        string result = Serialize(sequence, options);

        Assert.That(result, Is.EqualTo("\"é\"\"<mapped>\"|value=\"<mapped> &amp; y\""));
    }

    [Test]
    public void PassesXmlParametersToEachDelegatedNodeButWritesOneBom()
    {
        XmlDocument first = Parse("<one/>");
        XmlDocument second = Parse("<two/>");
        XdmSequence sequence = new XdmSequence
        {
            Count = 2,
            ItemKinds = [XdmItemKind.Node, XdmItemKind.Node],
            ItemIndexes = [0, 1],
            Nodes = [first.DocumentElement!, second.DocumentElement!]
        };
        MemoryStream stream = new MemoryStream();
        XmlOutput output = new XmlOutput { Kind = XmlOutputKind.Stream, ByteStream = stream };
        XmlSerializationOptions options = AdaptiveOptions();
        options.Encoding = "utf-8";
        options.ByteOrderMark = true;
        options.ItemSeparator = "|";

        new XmlSerializer().SerializeXdmSequence(sequence, output, options);

        byte[] bytes = stream.ToArray();
        string result = System.Text.Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        Assert.Multiple(() =>
        {
            Assert.That(bytes.Take(3), Is.EqualTo(new byte[] { 0xEF, 0xBB, 0xBF }));
            Assert.That(result, Is.EqualTo("<?xml version=\"1.0\" encoding=\"utf-8\"?><one/>|<?xml version=\"1.0\" encoding=\"utf-8\"?><two/>"));
        });
    }

    [Test]
    public void ResolvesUnqualifiedCallerMethodNamesAndRejectsUnavailableExtensions()
    {
        XmlDocument document = Parse("<root>text</root>");
        XmlSerializationOptions text = new XmlSerializationOptions
        {
            Method = XmlSerializationMethod.Extension,
            ExtensionMethodName = Name(null, "text")
        };
        XmlSerializationOptions qualified = new XmlSerializationOptions
        {
            Method = XmlSerializationMethod.Extension,
            ExtensionMethodName = Name("urn:custom", "format")
        };
        XmlSerializationOptions unknown = new XmlSerializationOptions
        {
            Method = XmlSerializationMethod.Extension,
            ExtensionMethodName = Name(null, "unknown")
        };

        string result = SerializeDocument(document, text);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo("text"));
            Assert.That(Assert.Throws<XmlSerializationException>(() => SerializeDocument(document, qualified))!.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.SESU0013));
            Assert.That(Assert.Throws<XmlSerializationException>(() => SerializeDocument(document, unknown))!.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.SEPM0004));
        });
    }

    [Test]
    public void RejectsRecursiveContainersAndNegativeFunctionArity()
    {
        XdmArrayValue recursive = new XdmArrayValue();
        XdmSequence recursiveSequence = ArraySequence(recursive);
        recursive.Count = 1;
        recursive.Members = [recursiveSequence];
        XdmSequence function = new XdmSequence
        {
            Count = 1,
            ItemKinds = [XdmItemKind.Function],
            ItemIndexes = [0],
            Functions = [new XdmFunctionItem { Arity = -1 }]
        };

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<XmlSerializationException>(() => Serialize(recursiveSequence, AdaptiveOptions()))!.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.SERE0021));
            Assert.That(Assert.Throws<XmlSerializationException>(() => Serialize(function, AdaptiveOptions()))!.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.SERE0021));
        });
    }

    private static XmlSerializationOptions AdaptiveOptions()
    {
        return new XmlSerializationOptions
        {
            Method = XmlSerializationMethod.Adaptive,
            InvalidCharacterPolicy = XmlInvalidCharacterPolicy.Error
        };
    }

    private static XmlName Name(string? namespaceUri, string localName)
    {
        return new XmlName { NamespaceUri = namespaceUri, LocalName = localName, QualifiedName = localName };
    }

    private static XdmAtomicValue String(string value)
    {
        return new XdmAtomicValue { Type = XdmAtomicType.String, StringValue = value };
    }

    private static XdmAtomicValue Boolean(bool value)
    {
        return new XdmAtomicValue { Type = XdmAtomicType.Boolean, BooleanValue = value };
    }

    private static XdmAtomicValue Integer(long value)
    {
        return new XdmAtomicValue { Type = XdmAtomicType.Integer, IntegerValue = value };
    }

    private static XdmAtomicValue Decimal(decimal value)
    {
        return new XdmAtomicValue { Type = XdmAtomicType.Decimal, DecimalValue = value };
    }

    private static XdmAtomicValue Floating(double value)
    {
        return new XdmAtomicValue { Type = XdmAtomicType.Double, FloatingValue = value };
    }

    private static XdmSequence EmptySequence()
    {
        return new XdmSequence { Count = 0, ItemKinds = [], ItemIndexes = [] };
    }

    private static XdmSequence AtomicSequence(XdmAtomicValue value)
    {
        return AtomicItems(value);
    }

    private static XdmSequence AtomicItems(params XdmAtomicValue[] values)
    {
        return new XdmSequence
        {
            Count = values.Length,
            ItemKinds = Enumerable.Repeat(XdmItemKind.Atomic, values.Length).ToArray(),
            ItemIndexes = Enumerable.Range(0, values.Length).ToArray(),
            AtomicValues = values
        };
    }

    private static XdmSequence ArraySequence(XdmArrayValue array)
    {
        return new XdmSequence
        {
            Count = 1,
            ItemKinds = [XdmItemKind.Array],
            ItemIndexes = [0],
            Arrays = [array]
        };
    }

    private static string Serialize(XdmSequence sequence, XmlSerializationOptions options)
    {
        StringWriter writer = new StringWriter();
        XmlOutput output = new XmlOutput { Kind = XmlOutputKind.TextWriter, TextWriter = writer };
        new XmlSerializer().SerializeXdmSequence(sequence, output, options);
        return writer.ToString();
    }

    private static string SerializeDocument(XmlDocument document, XmlSerializationOptions options)
    {
        StringWriter writer = new StringWriter();
        XmlOutput output = new XmlOutput { Kind = XmlOutputKind.TextWriter, TextWriter = writer };
        new XmlSerializer().SerializeXmlDocument(document, output, options);
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
            PreservePrefixes = true,
            RetainNamespaceDeclarations = true
        };
        return new XmlParser().ParseXmlDocument(input, options, null, null, null);
    }
}
