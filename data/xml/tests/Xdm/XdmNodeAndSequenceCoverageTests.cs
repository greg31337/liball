using xml;

namespace xml.Tests;

[TestFixture]
public class XdmNodeAndSequenceCoverageTests
{
    [Test]
    public void CoalescedTextTypedValueUsesTheCompleteProjectedStringValue()
    {
        XmlNode root = new XmlNode { Kind = XmlNodeKind.Element };
        XmlNode first = new XmlNode { Kind = XmlNodeKind.Text, Value = "A", Parent = root };
        XmlNode cdata = new XmlNode { Kind = XmlNodeKind.CData, Value = "B", Parent = root };
        XmlNode entity = new XmlNode { Kind = XmlNodeKind.EntityReference, Parent = root };
        XmlNode entityText = new XmlNode { Kind = XmlNodeKind.Text, Value = "C", Parent = entity };
        XmlNode last = new XmlNode { Kind = XmlNodeKind.Text, Value = "D", Parent = root };
        entity.Children = [entityText];
        root.Children = [first, cdata, entity, last];
        XdmNodeAccessor accessor = new XdmNodeAccessor();

        XdmNode projected = accessor.GetXdmNodeChildren(accessor.WrapXmlNode(root)).Single();
        XdmSequence typedValue = accessor.GetXdmNodeTypedValue(projected);

        Assert.Multiple(() =>
        {
            Assert.That(accessor.GetXdmNodeKind(projected), Is.EqualTo(XdmNodeKind.Text));
            Assert.That(projected.Node, Is.SameAs(first));
            Assert.That(accessor.GetXdmNodeStringValue(projected), Is.EqualTo("ABCD"));
            Assert.That(typedValue.Count, Is.EqualTo(1));
            Assert.That(typedValue.AtomicValues![0].Type, Is.EqualTo(XdmAtomicType.UntypedAtomic));
            Assert.That(typedValue.AtomicValues[0].StringValue, Is.EqualTo("ABCD"));
            Assert.That(typedValue.AtomicValues[0].IsImmutable, Is.True);
            Assert.That(typedValue.IsImmutable, Is.True);
        });
    }

    [Test]
    public void BooleanNodePropertiesReturnImmutableValuesForApplicableKinds()
    {
        XmlSchemaTypeInfo annotation = new XmlSchemaTypeInfo { IsNilled = true, IsId = true, IsIdReference = true };
        XmlNode element = new XmlNode { Kind = XmlNodeKind.Element, TypeInfo = annotation };
        XmlNode attribute = new XmlNode { Kind = XmlNodeKind.Attribute, TypeInfo = annotation };
        XdmNodeAccessor accessor = new XdmNodeAccessor();

        XdmSequence nilled = accessor.GetXdmNodeNilled(accessor.WrapXmlNode(element));
        XdmSequence isId = accessor.GetXdmNodeIsId(accessor.WrapXmlNode(attribute));
        XdmSequence isIdReference = accessor.GetXdmNodeIsIdReference(accessor.WrapXmlNode(attribute));
        XdmSequence falseId = accessor.GetXdmNodeIsId(accessor.WrapXmlNode(new XmlNode { Kind = XmlNodeKind.Element }));

        Assert.Multiple(() =>
        {
            Assert.That(nilled.AtomicValues![0].BooleanValue, Is.True);
            Assert.That(nilled.AtomicValues[0].IsImmutable, Is.True);
            Assert.That(isId.AtomicValues![0].BooleanValue, Is.True);
            Assert.That(isId.AtomicValues[0].IsImmutable, Is.True);
            Assert.That(isIdReference.AtomicValues![0].BooleanValue, Is.True);
            Assert.That(isIdReference.AtomicValues[0].IsImmutable, Is.True);
            Assert.That(falseId.AtomicValues![0].BooleanValue, Is.False);
            Assert.That(falseId.AtomicValues[0].IsImmutable, Is.True);
            Assert.That(nilled.IsImmutable, Is.True);
            Assert.That(isId.IsImmutable, Is.True);
        });
    }

    [Test]
    public void SingletonFactoriesAndCursorExposeEveryXdmItemCategory()
    {
        XdmSequenceOperations sequences = new XdmSequenceOperations();
        XmlNode node = new XmlNode { Kind = XmlNodeKind.Comment, Value = "node" };
        XdmAtomicValue atomic = Atomic("atomic", XdmAtomicType.String);
        XdmFunctionItem function = Function();
        XdmMapValue map = new XdmMapOperations().CreateEmptyXdmMap();
        XdmArrayValue array = new XdmArrayOperations().CreateEmptyXdmArray();
        XdmSequence[] singletons =
        [
            sequences.CreateXdmNodeSequence(node),
            sequences.CreateXdmAtomicSequence(atomic),
            sequences.CreateXdmFunctionSequence(function),
            sequences.CreateXdmMapSequence(map),
            sequences.CreateXdmArraySequence(array)
        ];
        XdmItemKind[] expectedKinds = [XdmItemKind.Node, XdmItemKind.Atomic, XdmItemKind.Function, XdmItemKind.Map, XdmItemKind.Array];

        int index = 0;
        while (index < singletons.Length)
        {
            XdmSequenceCursor cursor = new XdmSequenceCursor(singletons[index]);
            Assert.That(cursor.MoveNextXdmSequenceItem(), Is.True);
            XdmSequenceItem item = cursor.ReadCurrentXdmSequenceItem();
            Assert.Multiple(() =>
            {
                Assert.That(singletons[index].Count, Is.EqualTo(1));
                Assert.That(singletons[index].ItemKinds, Is.EqualTo(new[] { expectedKinds[index] }));
                Assert.That(singletons[index].ItemIndexes, Is.EqualTo(new[] { 0 }));
                Assert.That(singletons[index].IsImmutable, Is.True);
                Assert.That(item.Kind, Is.EqualTo(expectedKinds[index]));
                Assert.That(cursor.MoveNextXdmSequenceItem(), Is.False);
            });
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(new XdmSequenceCursor(singletons[0]).Sequence.Nodes![0], Is.SameAs(node));
            Assert.That(singletons[1].AtomicValues![0], Is.SameAs(atomic));
            Assert.That(singletons[2].Functions![0], Is.SameAs(function));
            Assert.That(singletons[3].Maps![0], Is.SameAs(map));
            Assert.That(singletons[4].Arrays![0], Is.SameAs(array));
        });
    }

    [Test]
    public void SequenceStorageValidationRejectsInvalidReferencesForEveryItemCategory()
    {
        XdmSequenceOperations sequences = new XdmSequenceOperations();
        XdmItemKind[] kinds = [XdmItemKind.Node, XdmItemKind.Atomic, XdmItemKind.Function, XdmItemKind.Map, XdmItemKind.Array];

        Assert.Multiple(() =>
        {
            foreach (XdmItemKind kind in kinds)
            {
                Assert.Throws<ArgumentException>(() => sequences.ValidateXdmSequenceStorage(new XdmSequence
                {
                    Count = 1,
                    ItemKinds = [kind],
                    ItemIndexes = [0]
                }));
            }

            Assert.Throws<ArgumentException>(() => sequences.ValidateXdmSequenceStorage(new XdmSequence
            {
                Count = 1,
                ItemKinds = [XdmItemKind.Node],
                ItemIndexes = [0],
                Nodes = [new XmlNode { Kind = XmlNodeKind.EntityReference }]
            }));
            Assert.Throws<ArgumentException>(() => sequences.ValidateXdmSequenceStorage(new XdmSequence
            {
                Count = 1,
                ItemKinds = [XdmItemKind.Array],
                ItemIndexes = [-1],
                Arrays = [new XdmArrayValue()]
            }));
            Assert.Throws<ArgumentException>(() => new XdmSequenceBuilder().AddXdmNode(new XmlNode { Kind = XmlNodeKind.DocumentType }));
        });
    }

    [Test]
    public void SequenceTypeMatchingCoversEveryItemCategoryAndWildcard()
    {
        XdmTypeConverter converter = new XdmTypeConverter();
        XdmSequenceOperations sequences = new XdmSequenceOperations();
        XdmSequence node = sequences.CreateXdmNodeSequence(new XmlNode { Kind = XmlNodeKind.Comment });
        XdmSequence atomic = sequences.CreateXdmAtomicSequence(Atomic("7", XdmAtomicType.Int));
        XdmSequence function = sequences.CreateXdmFunctionSequence(Function());
        XdmSequence map = sequences.CreateXdmMapSequence(new XdmMapOperations().CreateEmptyXdmMap());
        XdmSequence array = sequences.CreateXdmArraySequence(new XdmArrayOperations().CreateEmptyXdmArray());
        XdmSequence mixedFunctions = sequences.ConcatenateXdmSequences([function, map, array]);
        XdmSequence allItems = sequences.ConcatenateXdmSequences([node, atomic, mixedFunctions]);

        Assert.Multiple(() =>
        {
            Assert.That(converter.MatchesXdmSequenceType(node, NodeType(XdmNodeKind.Comment, false)), Is.True);
            Assert.That(converter.MatchesXdmSequenceType(node, NodeType(XdmNodeKind.Document, true)), Is.True);
            Assert.That(converter.MatchesXdmSequenceType(atomic, ItemType(XdmItemKind.Atomic)), Is.True);
            Assert.That(converter.MatchesXdmSequenceType(function, ItemType(XdmItemKind.Function)), Is.True);
            Assert.That(converter.MatchesXdmSequenceType(map, ItemType(XdmItemKind.Map)), Is.True);
            Assert.That(converter.MatchesXdmSequenceType(array, ItemType(XdmItemKind.Array)), Is.True);
            Assert.That(converter.MatchesXdmSequenceType(mixedFunctions, FunctionWildcardType()), Is.True);
            Assert.That(converter.MatchesXdmSequenceType(allItems, ItemWildcardType()), Is.True);
            Assert.That(converter.MatchesXdmSequenceType(node, ItemType(XdmItemKind.Array)), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => converter.MatchesXdmSequenceType(node, NodeType((XdmNodeKind)99, false)));
            Assert.Throws<ArgumentOutOfRangeException>(() => converter.MatchesXdmSequenceType(atomic, AtomicType((XdmAtomicType)99)));
        });
    }

    [Test]
    public void EffectiveBooleanValueCoversAtomicEdgesAndRejectsEveryFunctionCategory()
    {
        XdmTypeConverter converter = new XdmTypeConverter();
        XdmSequenceOperations sequences = new XdmSequenceOperations();
        XdmSequence function = sequences.CreateXdmFunctionSequence(Function());
        XdmSequence map = sequences.CreateXdmMapSequence(new XdmMapOperations().CreateEmptyXdmMap());
        XdmSequence array = sequences.CreateXdmArraySequence(new XdmArrayOperations().CreateEmptyXdmArray());

        Assert.Multiple(() =>
        {
            Assert.That(converter.EffectiveBooleanValueXdmSequence(sequences.CreateXdmAtomicSequence(new XdmTypeConverter().CreateXdmUntypedAtomicValue("value"))), Is.True);
            Assert.That(converter.EffectiveBooleanValueXdmSequence(sequences.CreateXdmAtomicSequence(new XdmAnyUriDatatype().ParseXdmAnyUri(""))), Is.False);
            Assert.That(converter.EffectiveBooleanValueXdmSequence(sequences.CreateXdmAtomicSequence(Atomic("-0", XdmAtomicType.Double))), Is.False);
            Assert.That(converter.EffectiveBooleanValueXdmSequence(sequences.CreateXdmAtomicSequence(Atomic("NaN", XdmAtomicType.Float))), Is.False);
            Assert.That(converter.EffectiveBooleanValueXdmSequence(sequences.CreateXdmAtomicSequence(Atomic("INF", XdmAtomicType.Double))), Is.True);
            Assert.That(Assert.Throws<XPathDynamicException>(() => converter.EffectiveBooleanValueXdmSequence(function))!.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORG0006));
            Assert.That(Assert.Throws<XPathDynamicException>(() => converter.EffectiveBooleanValueXdmSequence(map))!.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORG0006));
            Assert.That(Assert.Throws<XPathDynamicException>(() => converter.EffectiveBooleanValueXdmSequence(array))!.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORG0006));
        });
    }

    [Test]
    public void FreezingHeterogeneousSequencesDetachesAllCategoryStorageAndRetainsItemIdentity()
    {
        XmlNode node = new XmlNode { Kind = XmlNodeKind.Element };
        XdmAtomicValue atomic = Atomic("atomic", XdmAtomicType.String);
        XdmFunctionItem function = Function();
        XdmMapValue map = new XdmMapOperations().CreateEmptyXdmMap();
        XdmArrayValue array = new XdmArrayOperations().CreateEmptyXdmArray();
        XdmSequence source = Sequence(node, atomic, function, map, array);

        XdmSequence frozen = new XdmSequenceOperations().FreezeXdmSequenceStorage(source);

        source.ItemKinds![0] = XdmItemKind.Array;
        source.ItemIndexes![0] = 99;
        source.Nodes![0] = new XmlNode { Kind = XmlNodeKind.Comment };
        source.AtomicValues![0] = Atomic("changed", XdmAtomicType.String);
        source.Functions![0] = new XdmFunctionItem();
        source.Maps![0] = new XdmMapValue();
        source.Arrays![0] = new XdmArrayValue();

        Assert.Multiple(() =>
        {
            Assert.That(frozen.ItemKinds, Is.EqualTo(new[] { XdmItemKind.Node, XdmItemKind.Atomic, XdmItemKind.Function, XdmItemKind.Map, XdmItemKind.Array }));
            Assert.That(frozen.ItemIndexes, Is.EqualTo(new[] { 0, 0, 0, 0, 0 }));
            Assert.That(frozen.ItemKinds, Is.Not.SameAs(source.ItemKinds));
            Assert.That(frozen.ItemIndexes, Is.Not.SameAs(source.ItemIndexes));
            Assert.That(frozen.Nodes, Is.Not.SameAs(source.Nodes));
            Assert.That(frozen.AtomicValues, Is.Not.SameAs(source.AtomicValues));
            Assert.That(frozen.Functions, Is.Not.SameAs(source.Functions));
            Assert.That(frozen.Maps, Is.Not.SameAs(source.Maps));
            Assert.That(frozen.Arrays, Is.Not.SameAs(source.Arrays));
            Assert.That(frozen.Nodes![0], Is.SameAs(node));
            Assert.That(frozen.AtomicValues![0], Is.SameAs(atomic));
            Assert.That(frozen.Functions![0], Is.SameAs(function));
            Assert.That(frozen.Maps![0], Is.SameAs(map));
            Assert.That(frozen.Arrays![0], Is.SameAs(array));
            Assert.That(frozen.IsImmutable, Is.True);
            Assert.That(map.IsImmutable, Is.True);
            Assert.That(array.IsImmutable, Is.True);
            Assert.That(function.IsImmutable, Is.True);
        });
    }

    [Test]
    public void NamespaceProjectionCacheTracksMutationAndAccessorValidationRejectsInvalidTrees()
    {
        XmlDocument document = new XmlDocument();
        XmlNode element = new XmlNode
        {
            Kind = XmlNodeKind.Element,
            OwnerDocument = document,
            NamespaceBindings = [new XmlNamespaceBinding { Prefix = "p", NamespaceUri = "urn:first" }]
        };
        XdmNodeAccessor accessor = new XdmNodeAccessor();
        XdmNode wrapped = accessor.WrapXmlNode(element);
        XdmNode first = accessor.GetXdmNodeNamespaces(wrapped).Single(node => accessor.GetXdmNodeName(node)?.LocalName == "p");

        element.NamespaceBindings = [new XmlNamespaceBinding { Prefix = "p", NamespaceUri = "urn:second" }];
        document.MutationVersion++;
        XdmNode[] refreshed = accessor.GetXdmNodeNamespaces(wrapped);
        XdmNode second = refreshed.Single(node => accessor.GetXdmNodeName(node)?.LocalName == "p");
        refreshed[0] = wrapped;

        Assert.Multiple(() =>
        {
            Assert.That(accessor.GetXdmNodeStringValue(first), Is.EqualTo("urn:first"));
            Assert.That(accessor.GetXdmNodeStringValue(second), Is.EqualTo("urn:second"));
            Assert.That(accessor.HasSameXdmNodeIdentity(first, second), Is.False);
            Assert.That(accessor.GetXdmNodeNamespaces(wrapped)[0].Kind, Is.EqualTo(XdmNodeKind.Namespace));
        });

        element.Attributes = [new XmlNode { Kind = XmlNodeKind.Text }];
        element.Children = [new XmlNode { Kind = XmlNodeKind.Namespace }];
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => accessor.GetXdmNodeAttributes(wrapped));
            Assert.Throws<ArgumentException>(() => accessor.GetXdmNodeChildren(wrapped));
        });
    }

    private sealed class EmptyFunction : IXPathFunction
    {
        public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
        {
            return new XdmSequenceOperations().CreateEmptyXdmSequence();
        }
    }

    private static XdmFunctionItem Function()
    {
        XPathFunctionBinding binding = new XPathFunctionBinding
        {
            Arity = 0,
            ParameterTypes = [],
            ParameterOccurrences = [],
            ResultType = XdmAtomicType.AnyAtomicType,
            ResultOccurrence = XdmOccurrence.ZeroOrMore
        };
        return new XdmFunctionOperations().CreateXdmFunctionItem(binding, new EmptyFunction(), []);
    }

    private static XdmAtomicValue Atomic(string lexical, XdmAtomicType type)
    {
        if (type == XdmAtomicType.Float || type == XdmAtomicType.Double)
        {
            return new XdmFloatingDatatype().ParseXdmFloating(lexical, type);
        }

        if (new XdmTypeSystem().IsXdmAtomicSubtypeOf(type, XdmAtomicType.Integer))
        {
            return new XdmNumericDatatype().ParseXdmInteger(lexical, type);
        }

        return new XdmStringDatatype().ParseXdmStringAtomic(lexical, type);
    }

    private static XdmSequence Sequence(params object[] items)
    {
        XdmSequenceBuilder builder = new XdmSequenceBuilder();
        foreach (object item in items)
        {
            if (item is XmlNode node)
            {
                builder.AddXdmNode(node);
            }
            else if (item is XdmAtomicValue atomic)
            {
                builder.AddXdmAtomicValue(atomic);
            }
            else if (item is XdmFunctionItem function)
            {
                builder.AddXdmFunction(function);
            }
            else if (item is XdmMapValue map)
            {
                builder.AddXdmMap(map);
            }
            else if (item is XdmArrayValue array)
            {
                builder.AddXdmArray(array);
            }
        }

        return builder.BuildImmutableXdmSequence();
    }

    private static XdmSequenceType ItemType(XdmItemKind kind)
    {
        return new XdmSequenceType { ItemKind = kind, Occurrence = XdmOccurrence.ExactlyOne };
    }

    private static XdmSequenceType AtomicType(XdmAtomicType type)
    {
        return new XdmSequenceType { ItemKind = XdmItemKind.Atomic, AtomicType = type, Occurrence = XdmOccurrence.ExactlyOne };
    }

    private static XdmSequenceType NodeType(XdmNodeKind kind, bool anyNode)
    {
        return new XdmSequenceType { ItemKind = XdmItemKind.Node, NodeKind = kind, AnyNode = anyNode, Occurrence = XdmOccurrence.ExactlyOne };
    }

    private static XdmSequenceType FunctionWildcardType()
    {
        return new XdmSequenceType { AnyFunction = true, Occurrence = XdmOccurrence.OneOrMore };
    }

    private static XdmSequenceType ItemWildcardType()
    {
        return new XdmSequenceType { AnyItem = true, Occurrence = XdmOccurrence.OneOrMore };
    }
}
