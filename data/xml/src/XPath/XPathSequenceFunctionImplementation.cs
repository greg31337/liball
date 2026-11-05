using System;
using System.Collections.Generic;
using System.Numerics;

namespace xml;

internal class XPathSequenceSortEntry
{
    internal XdmSequenceItem Item;
    internal XdmSequence Key;

    internal XPathSequenceSortEntry(XdmSequenceItem item, XdmSequence key)
    {
        Item = item;
        Key = key;
    }
}

internal class XPathSequenceFunctionImplementation : IXPathFunction
{
    internal string LocalName;
    internal SourceRange Range;
    internal XPathStaticContext StaticContext;
    internal XdmSequenceOperations Sequences;
    internal XdmTypeConverter Converter;
    internal XdmTypeSystem Types;
    internal XdmFactory Factory;
    internal XdmNodeAccessor Nodes;
    internal XdmMapOperations Maps;
    internal XdmArrayOperations Arrays;
    internal XdmFunctionOperations Functions;

    internal XPathSequenceFunctionImplementation(string localName, SourceRange range, XPathStaticContext staticContext)
    {
        LocalName = localName;
        Range = CopyXPathSequenceRange(range);
        StaticContext = staticContext;
        Sequences = new XdmSequenceOperations();
        Converter = new XdmTypeConverter();
        Types = new XdmTypeSystem();
        Factory = new XdmFactory();
        Nodes = new XdmNodeAccessor();
        Maps = new XdmMapOperations();
        Arrays = new XdmArrayOperations();
        Functions = new XdmFunctionOperations();
    }

    public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
    {
        if (LocalName == "reverse")
        {
            return EvaluateXPathReverse(arguments[0]);
        }

        if (LocalName == "unordered")
        {
            return Sequences.FreezeXdmSequenceStorage(arguments[0]);
        }

        if (LocalName == "insert-before")
        {
            return EvaluateXPathInsertBefore(arguments);
        }

        if (LocalName == "remove")
        {
            return EvaluateXPathRemove(arguments);
        }

        if (LocalName == "subsequence")
        {
            return EvaluateXPathSubsequence(arguments);
        }

        if (LocalName == "distinct-values")
        {
            return EvaluateXPathDistinctValues(arguments, context);
        }

        if (LocalName == "index-of")
        {
            return EvaluateXPathIndexOf(arguments, context);
        }

        if (LocalName == "deep-equal")
        {
            IXPathCollation collation = ResolveXPathSequenceCollation(arguments, 2, false);
            return CreateXPathSequenceBooleanResult(DeepEqualXPathSequences(arguments[0], arguments[1], collation, context));
        }

        if (LocalName == "sort")
        {
            return EvaluateXPathSort(arguments, context);
        }

        if (LocalName == "for-each")
        {
            return EvaluateXPathForEach(arguments, context);
        }

        if (LocalName == "filter")
        {
            return EvaluateXPathFilter(arguments, context);
        }

        if (LocalName == "fold-left")
        {
            return EvaluateXPathFoldLeft(arguments, context);
        }

        if (LocalName == "fold-right")
        {
            return EvaluateXPathFoldRight(arguments, context);
        }

        if (LocalName == "for-each-pair")
        {
            return EvaluateXPathForEachPair(arguments, context);
        }

        throw new InvalidOperationException("The XPath sequence function is not implemented.");
    }

    internal XdmSequence EvaluateXPathForEach(XdmSequence[] arguments, XPathDynamicContext context)
    {
        XdmFunctionItem action = ReadXPathSequenceFunction(arguments[1], 1, "The for-each action must be a unary function item.");
        XdmSequenceBuilder result = new XdmSequenceBuilder();
        int index = 0;
        while (index < arguments[0].Count)
        {
            XdmSequence item = CreateXPathSingletonSequence(Sequences.ReadXdmSequenceItem(arguments[0], index));
            result.AppendXdmSequence(Functions.InvokeXdmFunctionItem(action, [item], context, StaticContext));
            index = index + 1;
        }

        return result.BuildImmutableXdmSequence();
    }

    internal XdmSequence EvaluateXPathFilter(XdmSequence[] arguments, XPathDynamicContext context)
    {
        XdmFunctionItem predicate = ReadXPathSequenceFunction(arguments[1], 1, "The filter predicate must be a unary function item.");
        XdmSequenceBuilder result = new XdmSequenceBuilder();
        int index = 0;
        while (index < arguments[0].Count)
        {
            XdmSequenceItem item = Sequences.ReadXdmSequenceItem(arguments[0], index);
            XdmSequence predicateResult = Functions.InvokeXdmFunctionItem(predicate, [CreateXPathSingletonSequence(item)], context, StaticContext);
            XdmSequence booleanResult = ConvertXPathAtomicTypeSequence(predicateResult, XdmAtomicType.Boolean, XdmOccurrence.ExactlyOne);
            if (ReadXPathSequenceAtomic(booleanResult, 0).BooleanValue)
            {
                AppendXPathSequenceItem(result, item);
            }

            index = index + 1;
        }

        return result.BuildImmutableXdmSequence();
    }

    internal XdmSequence EvaluateXPathFoldLeft(XdmSequence[] arguments, XPathDynamicContext context)
    {
        XdmFunctionItem action = ReadXPathSequenceFunction(arguments[2], 2, "The fold-left action must be a binary function item.");
        XdmSequence accumulated = arguments[1];
        int index = 0;
        while (index < arguments[0].Count)
        {
            XdmSequence item = CreateXPathSingletonSequence(Sequences.ReadXdmSequenceItem(arguments[0], index));
            accumulated = Functions.InvokeXdmFunctionItem(action, [accumulated, item], context, StaticContext);
            index = index + 1;
        }

        return Sequences.FreezeXdmSequenceStorage(accumulated);
    }

    internal XdmSequence EvaluateXPathFoldRight(XdmSequence[] arguments, XPathDynamicContext context)
    {
        XdmFunctionItem action = ReadXPathSequenceFunction(arguments[2], 2, "The fold-right action must be a binary function item.");
        XdmSequence accumulated = arguments[1];
        int index = arguments[0].Count - 1;
        while (index >= 0)
        {
            XdmSequence item = CreateXPathSingletonSequence(Sequences.ReadXdmSequenceItem(arguments[0], index));
            accumulated = Functions.InvokeXdmFunctionItem(action, [item, accumulated], context, StaticContext);
            index = index - 1;
        }

        return Sequences.FreezeXdmSequenceStorage(accumulated);
    }

    internal XdmSequence EvaluateXPathForEachPair(XdmSequence[] arguments, XPathDynamicContext context)
    {
        XdmFunctionItem action = ReadXPathSequenceFunction(arguments[2], 2, "The for-each-pair action must be a binary function item.");
        int count = arguments[0].Count < arguments[1].Count ? arguments[0].Count : arguments[1].Count;
        XdmSequenceBuilder result = new XdmSequenceBuilder();
        int index = 0;
        while (index < count)
        {
            XdmSequence left = CreateXPathSingletonSequence(Sequences.ReadXdmSequenceItem(arguments[0], index));
            XdmSequence right = CreateXPathSingletonSequence(Sequences.ReadXdmSequenceItem(arguments[1], index));
            result.AppendXdmSequence(Functions.InvokeXdmFunctionItem(action, [left, right], context, StaticContext));
            index = index + 1;
        }

        return result.BuildImmutableXdmSequence();
    }

    internal XdmFunctionItem ReadXPathSequenceFunction(XdmSequence supplied, int arity, string message)
    {
        if (supplied.Count != 1)
        {
            throw CreateXPathSequenceError(message, XPathErrorCodes.XPTY0004);
        }

        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(supplied, 0);
        XdmFunctionItem? result = null;
        if (item.Kind == XdmItemKind.Function)
        {
            result = item.Function;
        }
        else if (item.Kind == XdmItemKind.Map && item.Map != null)
        {
            result = Functions.CreateXdmMapFunctionItem(item.Map);
        }
        else if (item.Kind == XdmItemKind.Array && item.Array != null)
        {
            result = Functions.CreateXdmArrayFunctionItem(item.Array);
        }

        if (result == null || result.Arity != arity)
        {
            throw CreateXPathSequenceError(message, XPathErrorCodes.XPTY0004);
        }

        return result;
    }

    internal XdmSequence CreateXPathSingletonSequence(XdmSequenceItem item)
    {
        XdmSequenceBuilder result = new XdmSequenceBuilder();
        AppendXPathSequenceItem(result, item);
        return result.BuildImmutableXdmSequence();
    }

    internal XdmSequence EvaluateXPathReverse(XdmSequence supplied)
    {
        XdmSequenceBuilder result = new XdmSequenceBuilder();
        int index = supplied.Count - 1;
        while (index >= 0)
        {
            AppendXPathSequenceItem(result, Sequences.ReadXdmSequenceItem(supplied, index));
            index = index - 1;
        }

        return result.BuildImmutableXdmSequence();
    }

    internal XdmSequence EvaluateXPathInsertBefore(XdmSequence[] arguments)
    {
        BigInteger suppliedPosition = ReadXPathSequenceInteger(arguments[1]);
        int position = 0;
        if (suppliedPosition > BigInteger.One)
        {
            position = suppliedPosition > new BigInteger(arguments[0].Count) ? arguments[0].Count : (int)suppliedPosition - 1;
        }

        XdmSequenceBuilder result = new XdmSequenceBuilder();
        int index = 0;
        while (index < position)
        {
            AppendXPathSequenceItem(result, Sequences.ReadXdmSequenceItem(arguments[0], index));
            index = index + 1;
        }

        result.AppendXdmSequence(arguments[2]);
        while (index < arguments[0].Count)
        {
            AppendXPathSequenceItem(result, Sequences.ReadXdmSequenceItem(arguments[0], index));
            index = index + 1;
        }

        return result.BuildImmutableXdmSequence();
    }

    internal XdmSequence EvaluateXPathRemove(XdmSequence[] arguments)
    {
        BigInteger position = ReadXPathSequenceInteger(arguments[1]);
        XdmSequenceBuilder result = new XdmSequenceBuilder();
        int index = 0;
        while (index < arguments[0].Count)
        {
            if (position != new BigInteger(index + 1))
            {
                AppendXPathSequenceItem(result, Sequences.ReadXdmSequenceItem(arguments[0], index));
            }

            index = index + 1;
        }

        return result.BuildImmutableXdmSequence();
    }

    internal XdmSequence EvaluateXPathSubsequence(XdmSequence[] arguments)
    {
        double start = RoundXPathSequencePosition(ReadXPathSequenceDouble(arguments[1]));
        double end = double.PositiveInfinity;
        if (arguments.Length == 3)
        {
            end = start + RoundXPathSequencePosition(ReadXPathSequenceDouble(arguments[2]));
        }

        XdmSequenceBuilder result = new XdmSequenceBuilder();
        int index = 0;
        while (index < arguments[0].Count)
        {
            double position = index + 1;
            if (start <= position && position < end)
            {
                AppendXPathSequenceItem(result, Sequences.ReadXdmSequenceItem(arguments[0], index));
            }

            index = index + 1;
        }

        return result.BuildImmutableXdmSequence();
    }

    internal XdmSequence EvaluateXPathDistinctValues(XdmSequence[] arguments, XPathDynamicContext context)
    {
        XdmSequence values = ConvertXPathAtomicSequence(arguments[0], XdmOccurrence.ZeroOrMore);
        IXPathCollation collation = ResolveXPathSequenceCollation(arguments, 1, false);
        List<XdmAtomicValue> distinct = new List<XdmAtomicValue>();
        int index = 0;
        while (index < values.Count)
        {
            XdmAtomicValue candidate = ReadXPathSequenceAtomic(values, index);
            bool present = false;
            int earlier = 0;
            while (earlier < distinct.Count && !present)
            {
                present = EqualXPathAtomicValues(candidate, distinct[earlier], collation, context.ImplicitTimezoneMinutes, true);
                earlier = earlier + 1;
            }

            if (!present)
            {
                distinct.Add(candidate);
            }

            index = index + 1;
        }

        XdmSequenceBuilder result = new XdmSequenceBuilder();
        index = 0;
        while (index < distinct.Count)
        {
            result.AddXdmAtomicValue(distinct[index]);
            index = index + 1;
        }

        return result.BuildImmutableXdmSequence();
    }

    internal XdmSequence EvaluateXPathIndexOf(XdmSequence[] arguments, XPathDynamicContext context)
    {
        XdmSequence values = ConvertXPathAtomicSequence(arguments[0], XdmOccurrence.ZeroOrMore);
        XdmSequence searchSequence = ConvertXPathAtomicSequence(arguments[1], XdmOccurrence.ExactlyOne);
        XdmAtomicValue search = ReadXPathSequenceAtomic(searchSequence, 0);
        IXPathCollation collation = ResolveXPathSequenceCollation(arguments, 2, false);
        XdmSequenceBuilder result = new XdmSequenceBuilder();
        int index = 0;
        while (index < values.Count)
        {
            XdmAtomicValue candidate = ReadXPathSequenceAtomic(values, index);
            if (EqualXPathAtomicValues(candidate, search, collation, context.ImplicitTimezoneMinutes, false))
            {
                result.AddXdmAtomicValue(Factory.CreateXdmIntegerAtomicValue((index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), XdmAtomicType.Integer));
            }

            index = index + 1;
        }

        return result.BuildImmutableXdmSequence();
    }

    internal XdmSequence EvaluateXPathSort(XdmSequence[] arguments, XPathDynamicContext context)
    {
        IXPathCollation collation = ResolveXPathSequenceCollation(arguments, 1, true);
        XdmFunctionItem? keyFunction = arguments.Length == 3 ? ReadXPathSortFunction(arguments[2]) : null;
        XPathSequenceSortEntry[] entries = new XPathSequenceSortEntry[arguments[0].Count];
        int index = 0;
        while (index < arguments[0].Count)
        {
            XdmSequenceItem item = Sequences.ReadXdmSequenceItem(arguments[0], index);
            XdmSequence key = keyFunction == null ? AtomizeXPathSortItem(item) : InvokeXPathSortKey(keyFunction, item, context);
            entries[index] = new XPathSequenceSortEntry(item, key);
            index = index + 1;
        }

        index = 1;
        while (index < entries.Length)
        {
            XPathSequenceSortEntry entry = entries[index];
            int insertion = index;
            while (insertion > 0 && CompareXPathSortKeys(entry.Key, entries[insertion - 1].Key, collation, context) < 0)
            {
                entries[insertion] = entries[insertion - 1];
                insertion = insertion - 1;
            }

            entries[insertion] = entry;
            index = index + 1;
        }

        XdmSequenceBuilder result = new XdmSequenceBuilder();
        index = 0;
        while (index < entries.Length)
        {
            AppendXPathSequenceItem(result, entries[index].Item);
            index = index + 1;
        }

        return result.BuildImmutableXdmSequence();
    }

    internal XdmFunctionItem ReadXPathSortFunction(XdmSequence supplied)
    {
        if (supplied.Count != 1)
        {
            throw CreateXPathSequenceError("The sort key must be one function item.", XPathErrorCodes.XPTY0004);
        }

        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(supplied, 0);
        XdmFunctionItem? result = null;
        if (item.Kind == XdmItemKind.Function)
        {
            result = item.Function;
        }
        else if (item.Kind == XdmItemKind.Map && item.Map != null)
        {
            result = Functions.CreateXdmMapFunctionItem(item.Map);
        }
        else if (item.Kind == XdmItemKind.Array && item.Array != null)
        {
            result = Functions.CreateXdmArrayFunctionItem(item.Array);
        }

        if (result == null || result.Arity != 1)
        {
            throw CreateXPathSequenceError("The sort key must be a unary function item.", XPathErrorCodes.XPTY0004);
        }

        return result;
    }

    internal XdmSequence AtomizeXPathSortItem(XdmSequenceItem item)
    {
        XdmSequenceBuilder supplied = new XdmSequenceBuilder();
        AppendXPathSequenceItem(supplied, item);
        return Converter.AtomizeXdmSequence(supplied.BuildImmutableXdmSequence());
    }

    internal XdmSequence InvokeXPathSortKey(XdmFunctionItem function, XdmSequenceItem item, XPathDynamicContext context)
    {
        XdmSequenceBuilder argument = new XdmSequenceBuilder();
        AppendXPathSequenceItem(argument, item);
        XdmSequence result = Functions.InvokeXdmFunctionItem(function, [argument.BuildImmutableXdmSequence()], context, StaticContext);
        return Converter.AtomizeXdmSequence(result);
    }

    internal int CompareXPathSortKeys(XdmSequence left, XdmSequence right, IXPathCollation collation, XPathDynamicContext context)
    {
        int limit = left.Count < right.Count ? left.Count : right.Count;
        int index = 0;
        while (index < limit)
        {
            XdmAtomicValue leftValue = ReadXPathSequenceAtomic(left, index);
            XdmAtomicValue rightValue = ReadXPathSequenceAtomic(right, index);
            if (!EqualXPathAtomicValues(leftValue, rightValue, collation, context.ImplicitTimezoneMinutes, true))
            {
                return CompareXPathOrderedAtomicValues(leftValue, rightValue, collation, context.ImplicitTimezoneMinutes);
            }

            index = index + 1;
        }

        if (left.Count == right.Count)
        {
            return 0;
        }

        return left.Count < right.Count ? -1 : 1;
    }

    internal bool DeepEqualXPathSequences(XdmSequence left, XdmSequence right, IXPathCollation collation, XPathDynamicContext context)
    {
        RejectXPathDeepFunctionItems(left);
        RejectXPathDeepFunctionItems(right);
        if (left.Count != right.Count)
        {
            return false;
        }

        int index = 0;
        while (index < left.Count)
        {
            if (!DeepEqualXPathItems(Sequences.ReadXdmSequenceItem(left, index), Sequences.ReadXdmSequenceItem(right, index), collation, context))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal void RejectXPathDeepFunctionItems(XdmSequence sequence)
    {
        int index = 0;
        while (index < sequence.Count)
        {
            XdmSequenceItem item = Sequences.ReadXdmSequenceItem(sequence, index);
            if (item.Kind == XdmItemKind.Function)
            {
                throw CreateXPathSequenceError("Ordinary function items cannot be compared using fn:deep-equal.", XPathErrorCodes.FOTY0015);
            }

            index = index + 1;
        }
    }

    internal bool DeepEqualXPathItems(XdmSequenceItem left, XdmSequenceItem right, IXPathCollation collation, XPathDynamicContext context)
    {
        if (left.Kind != right.Kind)
        {
            return false;
        }

        if (left.Kind == XdmItemKind.Atomic && left.AtomicValue != null && right.AtomicValue != null)
        {
            return EqualXPathAtomicValues(left.AtomicValue, right.AtomicValue, collation, context.ImplicitTimezoneMinutes, true);
        }

        if (left.Kind == XdmItemKind.Node && left.Node != null && right.Node != null)
        {
            return DeepEqualXPathNodes(Nodes.WrapXmlNode(left.Node), Nodes.WrapXmlNode(right.Node), collation, context);
        }

        if (left.Kind == XdmItemKind.Map && left.Map != null && right.Map != null)
        {
            return DeepEqualXPathMaps(left.Map, right.Map, collation, context);
        }

        if (left.Kind == XdmItemKind.Array && left.Array != null && right.Array != null)
        {
            return DeepEqualXPathArrays(left.Array, right.Array, collation, context);
        }

        return false;
    }

    internal bool DeepEqualXPathMaps(XdmMapValue left, XdmMapValue right, IXPathCollation collation, XPathDynamicContext context)
    {
        Maps.ValidateXdmMapStorage(left);
        Maps.ValidateXdmMapStorage(right);
        if (left.Count != right.Count)
        {
            return false;
        }

        XdmMapEntry[] leftEntries = left.Entries == null ? [] : left.Entries;
        XdmMapEntry[] rightEntries = right.Entries == null ? [] : right.Entries;
        int leftIndex = 0;
        while (leftIndex < leftEntries.Length)
        {
            XdmAtomicValue? key = leftEntries[leftIndex].Key;
            XdmSequence? value = leftEntries[leftIndex].Value;
            bool matched = false;
            int rightIndex = 0;
            while (rightIndex < rightEntries.Length && !matched)
            {
                XdmAtomicValue? otherKey = rightEntries[rightIndex].Key;
                XdmSequence? otherValue = rightEntries[rightIndex].Value;
                if (key != null && otherKey != null && Maps.SameXdmMapKey(key, otherKey))
                {
                    matched = value != null && otherValue != null && DeepEqualXPathSequences(value, otherValue, collation, context);
                }

                rightIndex = rightIndex + 1;
            }

            if (!matched)
            {
                return false;
            }

            leftIndex = leftIndex + 1;
        }

        return true;
    }

    internal bool DeepEqualXPathArrays(XdmArrayValue left, XdmArrayValue right, IXPathCollation collation, XPathDynamicContext context)
    {
        Arrays.ValidateXdmArrayStorage(left);
        Arrays.ValidateXdmArrayStorage(right);
        if (left.Count != right.Count)
        {
            return false;
        }

        XdmSequence[] leftMembers = left.Members == null ? [] : left.Members;
        XdmSequence[] rightMembers = right.Members == null ? [] : right.Members;
        int index = 0;
        while (index < leftMembers.Length)
        {
            if (!DeepEqualXPathSequences(leftMembers[index], rightMembers[index], collation, context))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool DeepEqualXPathNodes(XdmNode left, XdmNode right, IXPathCollation collation, XPathDynamicContext context)
    {
        if (left.Kind != right.Kind)
        {
            return false;
        }

        if (left.Kind == XdmNodeKind.Document)
        {
            return DeepEqualXPathSequences(CreateXPathDeepChildSequence(left, false), CreateXPathDeepChildSequence(right, false), collation, context);
        }

        if (left.Kind == XdmNodeKind.Element)
        {
            return DeepEqualXPathElements(left, right, collation, context);
        }

        if (left.Kind == XdmNodeKind.Attribute)
        {
            return EqualXPathNodeNames(left, right) && DeepEqualXPathSequences(Nodes.GetXdmNodeTypedValue(left), Nodes.GetXdmNodeTypedValue(right), collation, context);
        }

        if (left.Kind == XdmNodeKind.ProcessingInstruction)
        {
            return EqualXPathNodeNames(left, right) && collation.CompareXPathCollation(Nodes.GetXdmNodeStringValue(left), Nodes.GetXdmNodeStringValue(right)) == 0;
        }

        if (left.Kind == XdmNodeKind.Namespace)
        {
            return EqualXPathNodeNames(left, right) && new XPathCodepointCollation().CompareXPathCollation(Nodes.GetXdmNodeStringValue(left), Nodes.GetXdmNodeStringValue(right)) == 0;
        }

        return collation.CompareXPathCollation(Nodes.GetXdmNodeStringValue(left), Nodes.GetXdmNodeStringValue(right)) == 0;
    }

    internal bool DeepEqualXPathElements(XdmNode left, XdmNode right, IXPathCollation collation, XPathDynamicContext context)
    {
        if (!EqualXPathNodeNames(left, right))
        {
            return false;
        }

        int leftContent = ReadXPathElementContentKind(left);
        int rightContent = ReadXPathElementContentKind(right);
        if (leftContent != rightContent)
        {
            return false;
        }

        XdmNode[] leftAttributes = Nodes.GetXdmNodeAttributes(left);
        XdmNode[] rightAttributes = Nodes.GetXdmNodeAttributes(right);
        if (leftAttributes.Length != rightAttributes.Length)
        {
            return false;
        }

        int leftIndex = 0;
        while (leftIndex < leftAttributes.Length)
        {
            bool matched = false;
            int rightIndex = 0;
            while (rightIndex < rightAttributes.Length && !matched)
            {
                if (EqualXPathNodeNames(leftAttributes[leftIndex], rightAttributes[rightIndex]))
                {
                    matched = DeepEqualXPathNodes(leftAttributes[leftIndex], rightAttributes[rightIndex], collation, context);
                }

                rightIndex = rightIndex + 1;
            }

            if (!matched)
            {
                return false;
            }

            leftIndex = leftIndex + 1;
        }

        if (leftContent == 0)
        {
            return DeepEqualXPathSequences(Nodes.GetXdmNodeTypedValue(left), Nodes.GetXdmNodeTypedValue(right), collation, context);
        }

        if (leftContent == 1)
        {
            return true;
        }

        return DeepEqualXPathSequences(CreateXPathDeepChildSequence(left, leftContent == 2), CreateXPathDeepChildSequence(right, rightContent == 2), collation, context);
    }

    internal int ReadXPathElementContentKind(XdmNode node)
    {
        XmlSchemaTypeInfo? annotation = Nodes.GetXdmNodeTypeAnnotation(node);
        if (annotation == null)
        {
            return 3;
        }

        CompiledXmlSchemaComplexType? complex = annotation.TypeDefinition as CompiledXmlSchemaComplexType;
        if (complex != null)
        {
            if (complex.ContentKind == XmlSchemaContentKind.Simple)
            {
                return 0;
            }

            if (complex.ContentKind == XmlSchemaContentKind.Empty)
            {
                return 1;
            }

            return complex.ContentKind == XmlSchemaContentKind.ElementOnly ? 2 : 3;
        }

        if (annotation.AtomicType != XdmAtomicType.AnyAtomicType || annotation.ActualValue != null || annotation.ActualValues != null)
        {
            return 0;
        }

        return 3;
    }

    internal XdmSequence CreateXPathDeepChildSequence(XdmNode node, bool elementsOnly)
    {
        XdmSequenceBuilder result = new XdmSequenceBuilder();
        XdmNode[] children = Nodes.GetXdmNodeChildren(node);
        int index = 0;
        while (index < children.Length)
        {
            XdmNode child = children[index];
            if (child.Kind == XdmNodeKind.Element || !elementsOnly && child.Kind == XdmNodeKind.Text)
            {
                result.AddXdmNode(child.Node);
            }

            index = index + 1;
        }

        return result.BuildImmutableXdmSequence();
    }

    internal bool EqualXPathNodeNames(XdmNode left, XdmNode right)
    {
        XmlName? leftName = Nodes.GetXdmNodeName(left);
        XmlName? rightName = Nodes.GetXdmNodeName(right);
        if (leftName == null || rightName == null)
        {
            return leftName == rightName;
        }

        return string.Equals(leftName.LocalName, rightName.LocalName, StringComparison.Ordinal) && string.Equals(leftName.NamespaceUri, rightName.NamespaceUri, StringComparison.Ordinal);
    }

    internal bool EqualXPathAtomicValues(XdmAtomicValue suppliedLeft, XdmAtomicValue suppliedRight, IXPathCollation collation, int implicitTimezoneMinutes, bool nanEqual)
    {
        XdmAtomicValue left = ConvertXPathSequenceUntypedAtomic(suppliedLeft);
        XdmAtomicValue right = ConvertXPathSequenceUntypedAtomic(suppliedRight);
        if (IsXPathSequenceStringType(left.Type) && IsXPathSequenceStringType(right.Type))
        {
            return collation.CompareXPathCollation(Converter.ReadXdmAtomicStringValue(left), Converter.ReadXdmAtomicStringValue(right)) == 0;
        }

        if (Types.IsXdmNumericAtomicType(left.Type) && Types.IsXdmNumericAtomicType(right.Type))
        {
            int order = new XdmNumericDatatype().CompareXdmNumeric(left, right);
            if (order == XdmFloatingDatatype.UnorderedComparison)
            {
                return nanEqual && IsXPathSequenceNan(left) && IsXPathSequenceNan(right);
            }

            return order == 0;
        }

        XdmAtomicType leftPrimitive = ReadXPathSequencePrimitiveType(left.Type);
        XdmAtomicType rightPrimitive = ReadXPathSequencePrimitiveType(right.Type);
        if (leftPrimitive != rightPrimitive)
        {
            return false;
        }

        if (leftPrimitive == XdmAtomicType.Boolean)
        {
            return left.BooleanValue == right.BooleanValue;
        }

        if (leftPrimitive == XdmAtomicType.Duration)
        {
            return new XdmDurationDatatype().EqualXdmDuration(left, right);
        }

        if (leftPrimitive >= XdmAtomicType.DateTime && leftPrimitive <= XdmAtomicType.GDay)
        {
            return new XdmDateTimeDatatype().EqualXdmDateTime(left, right, implicitTimezoneMinutes);
        }

        if (left.Type == right.Type && (left.Type == XdmAtomicType.HexBinary || left.Type == XdmAtomicType.Base64Binary))
        {
            return new XdmBinaryDatatype().EqualXdmBinary(left, right);
        }

        if (left.Type == XdmAtomicType.QName && right.Type == XdmAtomicType.QName)
        {
            return new XdmQNameDatatype().EqualXdmQName(left, right);
        }

        if (left.Type == XdmAtomicType.Notation && right.Type == XdmAtomicType.Notation)
        {
            return new XdmQNameDatatype().EqualXdmNotation(left, right);
        }

        return false;
    }

    internal int CompareXPathOrderedAtomicValues(XdmAtomicValue suppliedLeft, XdmAtomicValue suppliedRight, IXPathCollation collation, int implicitTimezoneMinutes)
    {
        XdmAtomicValue left = ConvertXPathSequenceUntypedAtomic(suppliedLeft);
        XdmAtomicValue right = ConvertXPathSequenceUntypedAtomic(suppliedRight);
        if (IsXPathSequenceNan(left))
        {
            return -1;
        }

        if (IsXPathSequenceNan(right))
        {
            return 1;
        }

        if (IsXPathSequenceStringType(left.Type) && IsXPathSequenceStringType(right.Type))
        {
            return collation.CompareXPathCollation(Converter.ReadXdmAtomicStringValue(left), Converter.ReadXdmAtomicStringValue(right));
        }

        if (Types.IsXdmNumericAtomicType(left.Type) && Types.IsXdmNumericAtomicType(right.Type))
        {
            return new XdmNumericDatatype().CompareXdmNumeric(left, right);
        }

        XdmAtomicType leftPrimitive = ReadXPathSequencePrimitiveType(left.Type);
        XdmAtomicType rightPrimitive = ReadXPathSequencePrimitiveType(right.Type);
        if (leftPrimitive != rightPrimitive)
        {
            throw CreateXPathSequenceError("The sort keys contain values that are not comparable.", XPathErrorCodes.XPTY0004);
        }

        if (leftPrimitive == XdmAtomicType.Boolean)
        {
            return new XdmNumericDatatype().CompareXdmBoolean(left, right);
        }

        if (left.Type == right.Type && (left.Type == XdmAtomicType.YearMonthDuration || left.Type == XdmAtomicType.DayTimeDuration))
        {
            return new XdmDurationDatatype().CompareXdmDuration(left, right);
        }

        if (leftPrimitive == XdmAtomicType.DateTime || leftPrimitive == XdmAtomicType.Date || leftPrimitive == XdmAtomicType.Time)
        {
            return new XdmDateTimeDatatype().CompareXdmDateTime(left, right, implicitTimezoneMinutes);
        }

        throw CreateXPathSequenceError("The sort keys contain values that are not comparable.", XPathErrorCodes.XPTY0004);
    }

    internal XdmAtomicValue ConvertXPathSequenceUntypedAtomic(XdmAtomicValue value)
    {
        if (value.Type != XdmAtomicType.UntypedAtomic)
        {
            return value;
        }

        return Factory.CreateXdmStringAtomicValue(Converter.ReadXdmAtomicStringValue(value), XdmAtomicType.String);
    }

    internal XdmAtomicType ReadXPathSequencePrimitiveType(XdmAtomicType type)
    {
        XdmAtomicTypeDefinition? primitive = Types.GetXdmPrimitiveAtomicTypeDefinition(type);
        return primitive == null ? type : primitive.Type;
    }

    internal bool IsXPathSequenceStringType(XdmAtomicType type)
    {
        return type == XdmAtomicType.AnyUri || Types.IsXdmStringDerivedAtomicType(type);
    }

    internal bool IsXPathSequenceNan(XdmAtomicValue value)
    {
        return (value.Type == XdmAtomicType.Float || value.Type == XdmAtomicType.Double) && double.IsNaN(value.FloatingValue);
    }

    internal IXPathCollation ResolveXPathSequenceCollation(XdmSequence[] arguments, int index, bool optional)
    {
        string uri = StaticContext.DefaultCollation == null ? XPathNamespaces.CodepointCollation : StaticContext.DefaultCollation;
        if (arguments.Length > index)
        {
            string? supplied = optional ? ReadXPathSequenceOptionalString(arguments[index]) : ReadXPathSequenceRequiredString(arguments[index]);
            if (supplied != null)
            {
                uri = supplied;
            }
        }

        uri = ResolveXPathSequenceCollationUri(uri);
        XPathCollationBinding[] bindings = StaticContext.CollationBindings == null ? [] : StaticContext.CollationBindings;
        int bindingIndex = 0;
        while (bindingIndex < bindings.Length)
        {
            XPathCollationBinding? binding = bindings[bindingIndex];
            if (binding != null && binding.Uri == uri && binding.Implementation != null)
            {
                return binding.Implementation;
            }

            bindingIndex = bindingIndex + 1;
        }

        if (uri == XPathNamespaces.CodepointCollation)
        {
            return new XPathCodepointCollation();
        }

        if (uri == XPathAsciiCaseInsensitiveCollation.Uri)
        {
            return new XPathAsciiCaseInsensitiveCollation();
        }

        throw CreateXPathSequenceError("The requested XPath collation is not supported.", XPathErrorCodes.FOCH0002);
    }

    internal string ResolveXPathSequenceCollationUri(string value)
    {
        if (Uri.IsWellFormedUriString(value, UriKind.Absolute))
        {
            return new Uri(value, UriKind.Absolute).AbsoluteUri;
        }

        string? baseUri = StaticContext.BaseUri;
        if (baseUri == null || !Uri.IsWellFormedUriString(value, UriKind.Relative))
        {
            throw CreateXPathSequenceError("The collation URI cannot be resolved.", XPathErrorCodes.FOCH0002);
        }

        return new Uri(new Uri(baseUri, UriKind.Absolute), value).AbsoluteUri;
    }

    internal string? ReadXPathSequenceOptionalString(XdmSequence supplied)
    {
        XdmSequence converted = ConvertXPathAtomicTypeSequence(supplied, XdmAtomicType.String, XdmOccurrence.ZeroOrOne);
        return converted.Count == 0 ? null : Converter.ReadXdmAtomicStringValue(ReadXPathSequenceAtomic(converted, 0));
    }

    internal string ReadXPathSequenceRequiredString(XdmSequence supplied)
    {
        XdmSequence converted = ConvertXPathAtomicTypeSequence(supplied, XdmAtomicType.String, XdmOccurrence.ExactlyOne);
        return Converter.ReadXdmAtomicStringValue(ReadXPathSequenceAtomic(converted, 0));
    }

    internal BigInteger ReadXPathSequenceInteger(XdmSequence supplied)
    {
        XdmSequence converted = ConvertXPathAtomicTypeSequence(supplied, XdmAtomicType.Integer, XdmOccurrence.ExactlyOne);
        return new XdmNumericDatatype().ReadXdmIntegerValue(ReadXPathSequenceAtomic(converted, 0));
    }

    internal double ReadXPathSequenceDouble(XdmSequence supplied)
    {
        XdmSequence converted = ConvertXPathAtomicTypeSequence(supplied, XdmAtomicType.Double, XdmOccurrence.ExactlyOne);
        return ReadXPathSequenceAtomic(converted, 0).FloatingValue;
    }

    internal double RoundXPathSequencePosition(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value == 0)
        {
            return value;
        }

        return Math.Floor(value + 0.5);
    }

    internal XdmSequence ConvertXPathAtomicSequence(XdmSequence supplied, XdmOccurrence occurrence)
    {
        return ConvertXPathAtomicTypeSequence(supplied, XdmAtomicType.AnyAtomicType, occurrence);
    }

    internal XdmSequence ConvertXPathAtomicTypeSequence(XdmSequence supplied, XdmAtomicType type, XdmOccurrence occurrence)
    {
        XdmSequenceType expected = new XdmSequenceType();
        expected.ItemKind = XdmItemKind.Atomic;
        expected.AtomicType = type;
        expected.Occurrence = occurrence;
        return Converter.ApplyXdmFunctionConversion(supplied, expected, StaticContext);
    }

    internal XdmAtomicValue ReadXPathSequenceAtomic(XdmSequence sequence, int index)
    {
        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(sequence, index);
        if (item.Kind != XdmItemKind.Atomic || item.AtomicValue == null)
        {
            throw CreateXPathSequenceError("The sequence item is not atomic.", XPathErrorCodes.XPTY0004);
        }

        return item.AtomicValue;
    }

    internal void AppendXPathSequenceItem(XdmSequenceBuilder builder, XdmSequenceItem item)
    {
        if (item.Kind == XdmItemKind.Node && item.Node != null)
        {
            builder.AddXdmNode(item.Node);
        }
        else if (item.Kind == XdmItemKind.Atomic && item.AtomicValue != null)
        {
            builder.AddXdmAtomicValue(item.AtomicValue);
        }
        else if (item.Kind == XdmItemKind.Function && item.Function != null)
        {
            builder.AddXdmFunction(item.Function);
        }
        else if (item.Kind == XdmItemKind.Map && item.Map != null)
        {
            builder.AddXdmMap(item.Map);
        }
        else if (item.Kind == XdmItemKind.Array && item.Array != null)
        {
            builder.AddXdmArray(item.Array);
        }
        else
        {
            throw new InvalidOperationException("The XPath sequence item is incomplete.");
        }
    }

    internal XdmSequence CreateXPathSequenceBooleanResult(bool value)
    {
        return Sequences.CreateXdmAtomicSequence(Factory.CreateXdmBooleanAtomicValue(value ? "true" : "false"));
    }

    internal XPathDynamicException CreateXPathSequenceError(string message, string code)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XPathDynamic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = CopyXPathSequenceRange(Range);
        return new XPathDynamicException(message, diagnostic, diagnostic.Range.Start);
    }

    internal SourceRange CopyXPathSequenceRange(SourceRange source)
    {
        SourceRange result = new SourceRange();
        result.Start = CopyXPathSequenceLocation(source.Start);
        result.End = CopyXPathSequenceLocation(source.End);
        return result;
    }

    internal SourceLocation? CopyXPathSequenceLocation(SourceLocation? source)
    {
        if (source == null)
        {
            return null;
        }

        SourceLocation result = new SourceLocation();
        result.SourceUri = source.SourceUri;
        result.CharacterOffset = source.CharacterOffset;
        result.ByteOffset = source.ByteOffset;
        result.Line = source.Line;
        result.Column = source.Column;
        return result;
    }
}
