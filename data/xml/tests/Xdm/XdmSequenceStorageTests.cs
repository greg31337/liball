using xml;

namespace xml.Tests;

[TestFixture]
public class XdmSequenceStorageTests
{
    [Test]
    public void BuildsEmptySingletonAndHeterogeneousImmutableSequences()
    {
        XdmSequenceBuilder emptyBuilder = new XdmSequenceBuilder();
        XdmSequence empty = emptyBuilder.BuildImmutableXdmSequence();
        XmlNode node = new XmlNode { Kind = XmlNodeKind.Element };
        XdmAtomicValue atomic = new XdmAtomicValue { Type = XdmAtomicType.String, StringValue = "value" };
        XdmFunctionItem function = new XdmFunctionItem { Arity = 1 };
        XdmMapValue map = new XdmMapValue { Count = 0, Entries = [], IsImmutable = true };
        XdmArrayValue array = new XdmArrayValue { Count = 0, Members = [], IsImmutable = true };
        XdmSequenceBuilder singletonBuilder = new XdmSequenceBuilder();
        singletonBuilder.AddXdmAtomicValue(atomic);
        XdmSequence singleton = singletonBuilder.BuildImmutableXdmSequence();
        XdmSequenceBuilder mixedBuilder = new XdmSequenceBuilder();
        mixedBuilder.AddXdmNode(node);
        mixedBuilder.AddXdmAtomicValue(atomic);
        mixedBuilder.AddXdmMap(map);
        mixedBuilder.AddXdmArray(array);
        mixedBuilder.AddXdmFunction(function);
        XdmSequence mixed = mixedBuilder.BuildImmutableXdmSequence();

        Assert.Multiple(() =>
        {
            Assert.That(empty.Count, Is.Zero);
            Assert.That(empty.ItemKinds, Is.Empty);
            Assert.That(empty.ItemIndexes, Is.Empty);
            Assert.That(empty.Nodes, Is.Empty);
            Assert.That(empty.AtomicValues, Is.Empty);
            Assert.That(empty.Functions, Is.Empty);
            Assert.That(empty.Maps, Is.Empty);
            Assert.That(empty.Arrays, Is.Empty);
            Assert.That(empty.IsImmutable, Is.True);
            Assert.That(singleton.Count, Is.EqualTo(1));
            Assert.That(singleton.ItemKinds, Is.EqualTo(new[] { XdmItemKind.Atomic }));
            Assert.That(singleton.ItemIndexes, Is.EqualTo(new[] { 0 }));
            Assert.That(singleton.AtomicValues, Is.EqualTo(new[] { atomic }));
            Assert.That(singleton.IsImmutable, Is.True);
            Assert.That(mixed.Count, Is.EqualTo(5));
            Assert.That(mixed.ItemKinds, Is.EqualTo(new[] { XdmItemKind.Node, XdmItemKind.Atomic, XdmItemKind.Map, XdmItemKind.Array, XdmItemKind.Function }));
            Assert.That(mixed.ItemIndexes, Is.EqualTo(new[] { 0, 0, 0, 0, 0 }));
            Assert.That(mixed.Nodes, Is.EqualTo(new[] { node }));
            Assert.That(mixed.AtomicValues, Is.EqualTo(new[] { atomic }));
            Assert.That(mixed.Functions, Is.EqualTo(new[] { function }));
            Assert.That(mixed.Maps, Is.EqualTo(new[] { map }));
            Assert.That(mixed.Arrays, Is.EqualTo(new[] { array }));
            Assert.That(mixed.IsImmutable, Is.True);
        });
    }

    [Test]
    public void CursorTraversesHeterogeneousItemsInLogicalOrderWithoutFlatteningCategories()
    {
        XmlNode firstNode = new XmlNode { Kind = XmlNodeKind.Element };
        XmlNode secondNode = new XmlNode { Kind = XmlNodeKind.Text, Value = "tail" };
        XdmAtomicValue atomic = new XdmAtomicValue { Type = XdmAtomicType.Integer, IntegerValue = 7 };
        XdmMapValue map = new XdmMapValue();
        XdmSequence sequence = new XdmSequence
        {
            Count = 4,
            ItemKinds = [XdmItemKind.Node, XdmItemKind.Atomic, XdmItemKind.Node, XdmItemKind.Map],
            ItemIndexes = [0, 0, 1, 0],
            Nodes = [firstNode, secondNode],
            AtomicValues = [atomic],
            Maps = [map]
        };
        XdmSequenceCursor cursor = new XdmSequenceCursor(sequence);

        Assert.Throws<InvalidOperationException>(() => cursor.ReadCurrentXdmSequenceItem());
        Assert.That(cursor.MoveNextXdmSequenceItem(), Is.True);
        Assert.That(cursor.ReadCurrentXdmSequenceItem().Node, Is.SameAs(firstNode));
        Assert.That(cursor.MoveNextXdmSequenceItem(), Is.True);
        Assert.That(cursor.ReadCurrentXdmSequenceItem().AtomicValue, Is.SameAs(atomic));
        Assert.That(cursor.MoveNextXdmSequenceItem(), Is.True);
        Assert.That(cursor.ReadCurrentXdmSequenceItem().Node, Is.SameAs(secondNode));
        Assert.That(cursor.MoveNextXdmSequenceItem(), Is.True);
        Assert.That(cursor.ReadCurrentXdmSequenceItem().Map, Is.SameAs(map));
        Assert.That(cursor.MoveNextXdmSequenceItem(), Is.False);
        Assert.That(cursor.MoveNextXdmSequenceItem(), Is.False);
        Assert.Throws<InvalidOperationException>(() => cursor.ReadCurrentXdmSequenceItem());
    }

    [Test]
    public void ConcatenationPreservesOrderAndCreatesDetachedCompactStorage()
    {
        XdmSequenceOperations operations = new XdmSequenceOperations();
        XdmAtomicValue one = new XdmAtomicValue { Type = XdmAtomicType.String, StringValue = "one" };
        XdmAtomicValue two = new XdmAtomicValue { Type = XdmAtomicType.String, StringValue = "two" };
        XmlNode node = new XmlNode { Kind = XmlNodeKind.Comment, Value = "node" };
        XdmSequence first = operations.CreateXdmAtomicSequence(one);
        XdmSequence second = operations.CreateEmptyXdmSequence();
        XdmSequenceBuilder thirdBuilder = new XdmSequenceBuilder();
        thirdBuilder.AddXdmNode(node);
        thirdBuilder.AddXdmAtomicValue(two);
        XdmSequence third = thirdBuilder.BuildImmutableXdmSequence();

        XdmSequence result = operations.ConcatenateXdmSequences([first, second, third]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result.ItemKinds, Is.EqualTo(new[] { XdmItemKind.Atomic, XdmItemKind.Node, XdmItemKind.Atomic }));
            Assert.That(result.ItemIndexes, Is.EqualTo(new[] { 0, 0, 1 }));
            Assert.That(result.AtomicValues, Is.EqualTo(new[] { one, two }));
            Assert.That(result.Nodes, Is.EqualTo(new[] { node }));
            Assert.That(result.ItemKinds, Is.Not.SameAs(first.ItemKinds));
            Assert.That(result.AtomicValues, Is.Not.SameAs(first.AtomicValues));
            Assert.That(result.IsImmutable, Is.True);
        });
    }

    [Test]
    public void FrozenSnapshotsAreStructurallyDetachedFromMutableSourceStorage()
    {
        XdmAtomicValue original = new XdmAtomicValue { Type = XdmAtomicType.String, StringValue = "original" };
        XdmAtomicValue replacement = new XdmAtomicValue { Type = XdmAtomicType.String, StringValue = "replacement" };
        XdmSequence mutable = new XdmSequence
        {
            Count = 1,
            ItemKinds = [XdmItemKind.Atomic],
            ItemIndexes = [0],
            AtomicValues = [original],
            IsImmutable = false
        };

        XdmSequence snapshot = new XdmSequenceOperations().FreezeXdmSequenceStorage(mutable);
        mutable.ItemKinds![0] = XdmItemKind.Node;
        mutable.ItemIndexes![0] = 9;
        mutable.AtomicValues![0] = replacement;
        mutable.Count = 0;

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.Count, Is.EqualTo(1));
            Assert.That(snapshot.ItemKinds, Is.EqualTo(new[] { XdmItemKind.Atomic }));
            Assert.That(snapshot.ItemIndexes, Is.EqualTo(new[] { 0 }));
            Assert.That(snapshot.AtomicValues, Is.EqualTo(new[] { original }));
            Assert.That(snapshot.IsImmutable, Is.True);
        });
    }

    [Test]
    public void ChecksAndRequiresEveryOccurrenceCardinality()
    {
        XdmSequenceOperations operations = new XdmSequenceOperations();
        XdmSequence empty = operations.CreateEmptyXdmSequence();
        XdmSequence one = operations.CreateXdmAtomicSequence(new XdmAtomicValue());
        XdmSequence two = operations.ConcatenateXdmSequences([one, one]);

        Assert.Multiple(() =>
        {
            Assert.That(operations.MatchesXdmSequenceCardinality(empty, XdmOccurrence.ExactlyOne), Is.False);
            Assert.That(operations.MatchesXdmSequenceCardinality(one, XdmOccurrence.ExactlyOne), Is.True);
            Assert.That(operations.MatchesXdmSequenceCardinality(empty, XdmOccurrence.ZeroOrOne), Is.True);
            Assert.That(operations.MatchesXdmSequenceCardinality(one, XdmOccurrence.ZeroOrOne), Is.True);
            Assert.That(operations.MatchesXdmSequenceCardinality(two, XdmOccurrence.ZeroOrOne), Is.False);
            Assert.That(operations.MatchesXdmSequenceCardinality(empty, XdmOccurrence.ZeroOrMore), Is.True);
            Assert.That(operations.MatchesXdmSequenceCardinality(two, XdmOccurrence.ZeroOrMore), Is.True);
            Assert.That(operations.MatchesXdmSequenceCardinality(empty, XdmOccurrence.OneOrMore), Is.False);
            Assert.That(operations.MatchesXdmSequenceCardinality(two, XdmOccurrence.OneOrMore), Is.True);
            Assert.That(operations.RequireXdmSequenceCardinality(one, XdmOccurrence.ExactlyOne), Is.SameAs(one));
        });

        XPathTypeException exception = Assert.Throws<XPathTypeException>(() => operations.RequireXdmSequenceCardinality(two, XdmOccurrence.ExactlyOne))!;
        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.XPTY0004));
            Assert.That(exception.Diagnostic.Category, Is.EqualTo(XmlErrorCategory.XPathType));
            Assert.Throws<ArgumentOutOfRangeException>(() => operations.MatchesXdmSequenceCardinality(one, (XdmOccurrence)99));
        });
    }

    [Test]
    public void RejectsMalformedStorageNullInputsAndInvalidPositions()
    {
        XdmSequenceOperations operations = new XdmSequenceOperations();
        XdmSequence inconsistentOrder = new XdmSequence { Count = 1, ItemKinds = [], ItemIndexes = [] };
        XdmSequence invalidIndex = new XdmSequence
        {
            Count = 1,
            ItemKinds = [XdmItemKind.Atomic],
            ItemIndexes = [1],
            AtomicValues = [new XdmAtomicValue()]
        };
        XdmSequence invalidKind = new XdmSequence
        {
            Count = 1,
            ItemKinds = [(XdmItemKind)99],
            ItemIndexes = [0]
        };
        XdmSequence empty = operations.CreateEmptyXdmSequence();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => operations.ValidateXdmSequenceStorage(inconsistentOrder));
            Assert.Throws<ArgumentException>(() => operations.ValidateXdmSequenceStorage(invalidIndex));
            Assert.Throws<ArgumentException>(() => operations.ValidateXdmSequenceStorage(invalidKind));
            Assert.Throws<ArgumentOutOfRangeException>(() => operations.ReadXdmSequenceItem(empty, 0));
            Assert.Throws<ArgumentException>(() => operations.ConcatenateXdmSequences([empty, null!]));
            Assert.Throws<ArgumentNullException>(() => new XdmSequenceBuilder().AddXdmNode(null!));
        });
    }

    [Test]
    public void ExistingPublicProducersReturnCanonicalImmutableEmptyAndNodeStorage()
    {
        XdmSequence functionResult = new XPathFunctionAdapter().InvokeXPathFunction(new XPathFunctionBinding(), [], new XPathDynamicContext());
        XdmSequence collectionResult = new XPathCollectionResolver().ResolveXPathCollection(null, new XmlResolverPolicy());
        XmlDocument document = Parse("<root/>");
        XdmSequence nodeResult = new XmlSerializer().CreateNodeJsonSerializationSequence(document.DocumentElement!);

        Assert.Multiple(() =>
        {
            Assert.That(functionResult.Count, Is.Zero);
            Assert.That(functionResult.ItemKinds, Is.Empty);
            Assert.That(functionResult.ItemIndexes, Is.Empty);
            Assert.That(functionResult.IsImmutable, Is.True);
            Assert.That(collectionResult.Count, Is.Zero);
            Assert.That(collectionResult.ItemKinds, Is.Empty);
            Assert.That(collectionResult.IsImmutable, Is.True);
            Assert.That(nodeResult.Count, Is.EqualTo(1));
            Assert.That(nodeResult.ItemKinds, Is.EqualTo(new[] { XdmItemKind.Node }));
            Assert.That(nodeResult.ItemIndexes, Is.EqualTo(new[] { 0 }));
            Assert.That(nodeResult.Nodes, Is.EqualTo(new[] { document.DocumentElement }));
            Assert.That(nodeResult.IsImmutable, Is.True);
        });
    }

    private static XmlDocument Parse(string source)
    {
        XmlParserOptions options = new XmlParserOptions
        {
            Version = XmlVersion.Xml10,
            NamespaceHandling = XmlNamespaceHandling.Namespaces10,
            DtdProcessing = XmlDtdProcessing.Prohibit,
            EntityHandling = XmlEntityHandling.Expand,
            ValidationMode = XmlValidationMode.None
        };
        return new XmlParser().ParseXmlDocument(new XmlInput { Kind = XmlInputKind.String, Text = source }, options, null, null, null);
    }
}
