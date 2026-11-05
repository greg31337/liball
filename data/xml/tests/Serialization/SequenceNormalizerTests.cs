using xml;

namespace xml.Tests;

[TestFixture]
public class SequenceNormalizerTests
{
    [Test]
    public void EmptySequenceProducesAnEmptyResultDocument()
    {
        XdmSequence sequence = new XdmSequence { Count = 0, ItemKinds = [], ItemIndexes = [] };

        XmlDocument result = new SequenceNormalizer().NormalizeXdmSerializationSequence(sequence, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.DocumentNode, Is.Not.Null);
            Assert.That(result.DocumentNode!.Kind, Is.EqualTo(XmlNodeKind.Document));
            Assert.That(result.DocumentNode.Children, Is.Empty);
            Assert.That(result.DocumentElement, Is.Null);
        });
    }

    [Test]
    public void JoinsAdjacentAtomicValuesWithSingleSpaces()
    {
        XdmAtomicValue text = Atomic(XdmAtomicType.String, "alpha");
        XdmAtomicValue integer = new XdmAtomicValue { Type = XdmAtomicType.Integer, IntegerValue = 42 };
        XdmAtomicValue boolean = new XdmAtomicValue { Type = XdmAtomicType.Boolean, BooleanValue = true };
        XdmSequence sequence = new XdmSequence
        {
            Count = 3,
            ItemKinds = [XdmItemKind.Atomic, XdmItemKind.Atomic, XdmItemKind.Atomic],
            ItemIndexes = [0, 1, 2],
            AtomicValues = [text, integer, boolean]
        };

        XmlDocument result = new SequenceNormalizer().NormalizeXdmSerializationSequence(sequence, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.DocumentNode!.Children, Has.Length.EqualTo(1));
            Assert.That(result.DocumentNode.Children![0].Kind, Is.EqualTo(XmlNodeKind.Text));
            Assert.That(result.DocumentNode.Children[0].Value, Is.EqualTo("alpha 42 true"));
        });
    }

    [Test]
    public void InsertsItemSeparatorsBetweenAtomicAndNodeItems()
    {
        XmlDocument source = Parse("<middle><child/></middle>");
        XmlNode original = source.DocumentElement!;
        XdmSequence sequence = new XdmSequence
        {
            Count = 3,
            ItemKinds = [XdmItemKind.Atomic, XdmItemKind.Node, XdmItemKind.Atomic],
            ItemIndexes = [0, 0, 1],
            AtomicValues = [Atomic(XdmAtomicType.String, "left"), Atomic(XdmAtomicType.String, "right")],
            Nodes = [original]
        };

        XmlDocument result = new SequenceNormalizer().NormalizeXdmSerializationSequence(sequence, "|");
        XmlNode[] children = result.DocumentNode!.Children!;

        Assert.Multiple(() =>
        {
            Assert.That(children, Has.Length.EqualTo(3));
            Assert.That(children[0].Value, Is.EqualTo("left|"));
            Assert.That(children[1].Name!.LocalName, Is.EqualTo("middle"));
            Assert.That(children[2].Value, Is.EqualTo("|right"));
            Assert.That(children[1], Is.Not.SameAs(original));
            Assert.That(children[1].OwnerDocument, Is.SameAs(result));
            Assert.That(children[1].Children![0], Is.Not.SameAs(original.Children![0]));
            Assert.That(original.Parent, Is.SameAs(source.DocumentNode));
        });
    }

    [Test]
    public void FlattensNestedArraysBeforeApplyingSeparators()
    {
        XdmSequence first = AtomicSequence("one");
        XdmSequence second = AtomicSequence("two");
        XdmArrayValue inner = new XdmArrayValue { Count = 1, Members = [second] };
        XdmSequence innerSequence = new XdmSequence
        {
            Count = 1,
            ItemKinds = [XdmItemKind.Array],
            ItemIndexes = [0],
            Arrays = [inner]
        };
        XdmArrayValue outer = new XdmArrayValue { Count = 2, Members = [first, innerSequence] };
        XdmSequence sequence = new XdmSequence
        {
            Count = 1,
            ItemKinds = [XdmItemKind.Array],
            ItemIndexes = [0],
            Arrays = [outer]
        };

        XmlDocument result = new SequenceNormalizer().NormalizeXdmSerializationSequence(sequence, ",");

        Assert.That(result.DocumentNode!.Children![0].Value, Is.EqualTo("one,two"));
    }

    [Test]
    public void PromotesDocumentChildrenAndMergesAdjacentTextNodes()
    {
        XmlDocument source = Parse("<source>inside</source>");
        XdmSequence sequence = new XdmSequence
        {
            Count = 3,
            ItemKinds = [XdmItemKind.Atomic, XdmItemKind.Node, XdmItemKind.Atomic],
            ItemIndexes = [0, 0, 1],
            AtomicValues = [Atomic(XdmAtomicType.String, "before"), Atomic(XdmAtomicType.String, "after")],
            Nodes = [source.DocumentNode!]
        };

        XmlDocument result = new SequenceNormalizer().NormalizeXdmSerializationSequence(sequence, null);
        XmlNode[] children = result.DocumentNode!.Children!;

        Assert.Multiple(() =>
        {
            Assert.That(children, Has.Length.EqualTo(3));
            Assert.That(children[0].Value, Is.EqualTo("before"));
            Assert.That(children[1].Kind, Is.EqualTo(XmlNodeKind.Element));
            Assert.That(children[2].Value, Is.EqualTo("after"));
            Assert.That(children, Has.None.Matches<XmlNode>(node => node.Kind == XmlNodeKind.Document));
        });
    }

    [Test]
    public void NormalizesDocumentAndNodeInputsAsDeepCopies()
    {
        XmlDocument source = Parse("<root><child/></root>");
        SequenceNormalizer normalizer = new SequenceNormalizer();

        XmlDocument fromDocument = normalizer.NormalizeXmlSerializationDocument(source, null);
        XmlDocument fromNode = normalizer.NormalizeXmlSerializationNode(source.DocumentElement!, null);

        Assert.Multiple(() =>
        {
            Assert.That(fromDocument.DocumentElement!.Name!.LocalName, Is.EqualTo("root"));
            Assert.That(fromNode.DocumentElement!.Name!.LocalName, Is.EqualTo("root"));
            Assert.That(fromDocument.DocumentElement, Is.Not.SameAs(source.DocumentElement));
            Assert.That(fromNode.DocumentElement, Is.Not.SameAs(source.DocumentElement));
            Assert.That(fromDocument.DocumentElement!.Children![0], Is.Not.SameAs(source.DocumentElement!.Children![0]));
            Assert.That(fromDocument.DocumentElement.OwnerDocument, Is.SameAs(fromDocument));
            Assert.That(source.DocumentElement.OwnerDocument, Is.SameAs(source));
        });
    }

    [Test]
    public void DropsZeroLengthTextButRetainsSpacesBetweenEmptyAtomics()
    {
        XdmSequence oneEmpty = AtomicSequence("");
        XdmSequence twoEmpty = new XdmSequence
        {
            Count = 2,
            ItemKinds = [XdmItemKind.Atomic, XdmItemKind.Atomic],
            ItemIndexes = [0, 1],
            AtomicValues = [Atomic(XdmAtomicType.String, ""), Atomic(XdmAtomicType.String, "")]
        };

        XmlDocument one = new SequenceNormalizer().NormalizeXdmSerializationSequence(oneEmpty, null);
        XmlDocument two = new SequenceNormalizer().NormalizeXdmSerializationSequence(twoEmpty, null);
        XmlDocument separated = new SequenceNormalizer().NormalizeXdmSerializationSequence(twoEmpty, "");

        Assert.Multiple(() =>
        {
            Assert.That(one.DocumentNode!.Children, Is.Empty);
            Assert.That(two.DocumentNode!.Children![0].Value, Is.EqualTo(" "));
            Assert.That(separated.DocumentNode!.Children, Is.Empty);
        });
    }

    [Test]
    public void ReportsSenr0001ForFunctionAttributeAndMalformedStorage()
    {
        XdmSequence function = new XdmSequence { Count = 1, ItemKinds = [XdmItemKind.Function], ItemIndexes = [0] };
        XmlDocument owner = Parse("<root a='value'/>");
        XmlNode attribute = owner.DocumentElement!.Attributes![0];
        XdmSequence attributeSequence = new XdmSequence { Count = 1, ItemKinds = [XdmItemKind.Node], ItemIndexes = [0], Nodes = [attribute] };
        XdmSequence malformed = new XdmSequence { Count = 1, ItemKinds = [], ItemIndexes = [] };

        XmlSerializationException functionFailure = Assert.Throws<XmlSerializationException>(() => new SequenceNormalizer().NormalizeXdmSerializationSequence(function, null))!;
        XmlSerializationException attributeFailure = Assert.Throws<XmlSerializationException>(() => new SequenceNormalizer().NormalizeXdmSerializationSequence(attributeSequence, null))!;
        XmlSerializationException storageFailure = Assert.Throws<XmlSerializationException>(() => new SequenceNormalizer().NormalizeXdmSerializationSequence(malformed, null))!;

        Assert.Multiple(() =>
        {
            Assert.That(functionFailure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.SENR0001));
            Assert.That(attributeFailure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.SENR0001));
            Assert.That(storageFailure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.SENR0001));
        });
    }

    [Test]
    public void RejectsRecursiveArraysAndMissingAtomicRepresentations()
    {
        XdmArrayValue array = new XdmArrayValue();
        XdmSequence recursive = new XdmSequence { Count = 1, ItemKinds = [XdmItemKind.Array], ItemIndexes = [0], Arrays = [array] };
        array.Count = 1;
        array.Members = [recursive];
        XdmSequence missing = new XdmSequence
        {
            Count = 1,
            ItemKinds = [XdmItemKind.Atomic],
            ItemIndexes = [0],
            AtomicValues = [new XdmAtomicValue { Type = XdmAtomicType.Date }]
        };

        Assert.Throws<XmlSerializationException>(() => new SequenceNormalizer().NormalizeXdmSerializationSequence(recursive, null));
        Assert.Throws<XmlSerializationException>(() => new SequenceNormalizer().NormalizeXdmSerializationSequence(missing, null));
    }

    private static XdmAtomicValue Atomic(XdmAtomicType type, string value)
    {
        return new XdmAtomicValue { Type = type, StringValue = value };
    }

    private static XdmSequence AtomicSequence(string value)
    {
        return new XdmSequence
        {
            Count = 1,
            ItemKinds = [XdmItemKind.Atomic],
            ItemIndexes = [0],
            AtomicValues = [Atomic(XdmAtomicType.String, value)]
        };
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
