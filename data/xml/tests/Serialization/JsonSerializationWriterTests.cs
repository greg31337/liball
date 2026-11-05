using xml;

namespace xml.Tests;

[TestFixture]
public class JsonSerializationWriterTests
{
    [Test]
    public void SerializesNestedMapsArraysAtomicsAndEmptyMembers()
    {
        XdmArrayValue array = new XdmArrayValue
        {
            Count = 3,
            Members = [AtomicSequence(Floating(1.5)), AtomicSequence(Boolean(true)), EmptySequence()]
        };
        XdmMapValue map = new XdmMapValue
        {
            Count = 2,
            Entries =
            [
                Entry(String("name"), AtomicSequence(String("A\n\"B"))),
                Entry(String("items"), ArraySequence(array))
            ]
        };

        string result = Serialize(MapSequence(map), JsonOptions());

        Assert.That(result, Is.EqualTo("{\"name\":\"A\\n\\\"B\",\"items\":[1.5,true,null]}"));
    }

    [Test]
    public void IndentsNestedValuesWithTheConfiguredNewline()
    {
        XdmArrayValue array = new XdmArrayValue
        {
            Count = 2,
            Members = [AtomicSequence(Integer(1)), AtomicSequence(Integer(2))]
        };
        XdmMapValue map = new XdmMapValue
        {
            Count = 1,
            Entries = [Entry(String("values"), ArraySequence(array))]
        };
        XmlSerializationOptions options = JsonOptions();
        options.Indent = true;
        options.NewLine = "\r\n";

        string result = Serialize(MapSequence(map), options);

        Assert.That(result, Is.EqualTo("{\r\n  \"values\": [\r\n    1,\r\n    2\r\n  ]\r\n}"));
    }

    [Test]
    public void DetectsDuplicateKeyStringValuesUnlessAllowed()
    {
        XdmMapValue map = new XdmMapValue
        {
            Count = 2,
            Entries =
            [
                Entry(String("1"), AtomicSequence(String("first"))),
                Entry(Integer(1), AtomicSequence(String("second")))
            ]
        };
        XmlSerializationOptions rejected = JsonOptions();
        XmlSerializationOptions allowed = JsonOptions();
        allowed.AllowDuplicateNames = true;

        XmlSerializationException exception = Assert.Throws<XmlSerializationException>(() => Serialize(MapSequence(map), rejected))!;
        string result = Serialize(MapSequence(map), allowed);

        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.SERE0022));
            Assert.That(result, Is.EqualTo("{\"1\":\"first\",\"1\":\"second\"}"));
        });
    }

    [Test]
    public void AppliesCharacterMapsBeforeNormalizationAndJsonEscaping()
    {
        XmlSerializationOptions options = JsonOptions();
        options.NormalizationForm = "NFC";
        options.UseCharacterMaps = [new XmlCharacterMapEntry { CodePoint = 0x22, Replacement = "\\u0022" }];
        XdmSequence sequence = AtomicSequence(String("é\"/\n\u0085"));

        string result = Serialize(sequence, options);

        Assert.That(result, Is.EqualTo("é".Insert(0, "\"") + "\\u0022\\/\\n\\u0085\""));
    }

    [Test]
    public void SerializesNodesThroughXmlOrTextFallbackBeforeJsonEscaping()
    {
        XmlDocument document = Parse("<root a=\"1\">value</root>");
        XdmSequence sequence = NodeSequence(document.DocumentElement!);
        XmlSerializationOptions xmlOptions = JsonOptions();
        xmlOptions.JsonNodeOutputMethod = XmlSerializationMethod.Xml;
        XmlSerializationOptions textOptions = JsonOptions();
        textOptions.JsonNodeOutputMethod = XmlSerializationMethod.Text;

        string xml = Serialize(sequence, xmlOptions);
        string text = Serialize(sequence, textOptions);

        Assert.Multiple(() =>
        {
            Assert.That(xml, Is.EqualTo("\"<root a=\\\"1\\\">value<\\/root>\""));
            Assert.That(text, Is.EqualTo("\"value\""));
        });
    }

    [Test]
    public void RejectsNonFiniteNumbers()
    {
        XdmSequence nan = AtomicSequence(Floating(double.NaN));
        XdmSequence infinity = AtomicSequence(Floating(double.PositiveInfinity));

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<XmlSerializationException>(() => Serialize(nan, JsonOptions()))!.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.SERE0020));
            Assert.That(Assert.Throws<XmlSerializationException>(() => Serialize(infinity, JsonOptions()))!.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.SERE0020));
        });
    }

    [Test]
    public void RejectsTopLevelAndNestedSequencesWithMultipleItems()
    {
        XdmSequence multiple = new XdmSequence
        {
            Count = 2,
            ItemKinds = [XdmItemKind.Atomic, XdmItemKind.Atomic],
            ItemIndexes = [0, 1],
            AtomicValues = [Integer(1), Integer(2)]
        };
        XdmArrayValue array = new XdmArrayValue { Count = 1, Members = [multiple] };

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<XmlSerializationException>(() => Serialize(multiple, JsonOptions()))!.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.SERE0023));
            Assert.That(Assert.Throws<XmlSerializationException>(() => Serialize(ArraySequence(array), JsonOptions()))!.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.SERE0023));
        });
    }

    [Test]
    public void RejectsFunctionsRecursiveArraysAndInvalidNodeFallbacks()
    {
        XdmSequence function = new XdmSequence
        {
            Count = 1,
            ItemKinds = [XdmItemKind.Function],
            ItemIndexes = [0],
            Functions = [new XdmFunctionItem { Arity = 0 }]
        };
        XdmArrayValue recursive = new XdmArrayValue();
        XdmSequence recursiveSequence = ArraySequence(recursive);
        recursive.Count = 1;
        recursive.Members = [recursiveSequence];
        XmlSerializationOptions invalidFallback = JsonOptions();
        invalidFallback.JsonNodeOutputMethod = XmlSerializationMethod.Json;
        XdmSequence node = NodeSequence(Parse("<root/>").DocumentElement!);

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<XmlSerializationException>(() => Serialize(function, JsonOptions()))!.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.SERE0021));
            Assert.That(Assert.Throws<XmlSerializationException>(() => Serialize(recursiveSequence, JsonOptions()))!.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.SERE0021));
            Assert.That(Assert.Throws<XmlSerializationException>(() => Serialize(node, invalidFallback))!.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.SEPM0004));
        });
    }

    [Test]
    public void EmptyTopLevelSequenceSerializesAsNull()
    {
        Assert.That(Serialize(EmptySequence(), JsonOptions()), Is.EqualTo("null"));
    }

    private static XmlSerializationOptions JsonOptions()
    {
        return new XmlSerializationOptions { Method = XmlSerializationMethod.Json };
    }

    private static XdmAtomicValue String(string value)
    {
        return new XdmAtomicValue { Type = XdmAtomicType.String, StringValue = value };
    }

    private static XdmAtomicValue Integer(long value)
    {
        return new XdmAtomicValue { Type = XdmAtomicType.Integer, IntegerValue = value };
    }

    private static XdmAtomicValue Floating(double value)
    {
        return new XdmAtomicValue { Type = XdmAtomicType.Double, FloatingValue = value };
    }

    private static XdmAtomicValue Boolean(bool value)
    {
        return new XdmAtomicValue { Type = XdmAtomicType.Boolean, BooleanValue = value };
    }

    private static XdmMapEntry Entry(XdmAtomicValue key, XdmSequence value)
    {
        return new XdmMapEntry { Key = key, Value = value };
    }

    private static XdmSequence EmptySequence()
    {
        return new XdmSequence { Count = 0, ItemKinds = [], ItemIndexes = [] };
    }

    private static XdmSequence AtomicSequence(XdmAtomicValue value)
    {
        return new XdmSequence
        {
            Count = 1,
            ItemKinds = [XdmItemKind.Atomic],
            ItemIndexes = [0],
            AtomicValues = [value]
        };
    }

    private static XdmSequence NodeSequence(XmlNode node)
    {
        return new XdmSequence
        {
            Count = 1,
            ItemKinds = [XdmItemKind.Node],
            ItemIndexes = [0],
            Nodes = [node]
        };
    }

    private static XdmSequence MapSequence(XdmMapValue map)
    {
        return new XdmSequence
        {
            Count = 1,
            ItemKinds = [XdmItemKind.Map],
            ItemIndexes = [0],
            Maps = [map]
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
