using System;
using System.Numerics;

namespace xml;

internal class XPathComparisonEvaluator
{
    internal XPathRuntime Runtime;
    internal XdmTypeConverter Converter;
    internal XdmTypeSystem Types;
    internal XdmSequenceOperations Sequences;

    internal XPathComparisonEvaluator(XPathRuntime runtime)
    {
        Runtime = runtime;
        Converter = new XdmTypeConverter();
        Types = new XdmTypeSystem();
        Sequences = new XdmSequenceOperations();
    }

    internal XdmSequence EvaluateXPathComparison(XPathPlanInstruction instruction)
    {
        if (instruction.Opcode == XPathPlanOpcode.GeneralComparison)
        {
            return EvaluateXPathGeneralComparison(instruction);
        }

        if (instruction.Opcode == XPathPlanOpcode.ValueComparison)
        {
            return EvaluateXPathValueComparison(instruction);
        }

        return EvaluateXPathNodeComparison(instruction);
    }

    internal XdmSequence EvaluateXPathGeneralComparison(XPathPlanInstruction instruction)
    {
        XPathPlanInstruction? leftInstruction = instruction.Left;
        XPathPlanInstruction? rightInstruction = instruction.Right;
        if (leftInstruction == null || rightInstruction == null)
        {
            throw new InvalidOperationException("An XPath comparison instruction requires two operands.");
        }

        XdmSequence? shortcut = EvaluateXPathRelationalRangeComparison(leftInstruction, rightInstruction, instruction);
        if (shortcut != null)
        {
            return shortcut;
        }

        XdmSequence left = Runtime.EvaluateXPathInstruction(leftInstruction);
        XdmSequence right = Runtime.EvaluateXPathInstruction(rightInstruction);
        string operation = instruction.Operator == null ? "" : instruction.Operator;
        int backwardsBoolean = Runtime.Plan.Environment.CompatibilityMode == XPathCompatibilityMode.BackwardsCompatible ? EvaluateXPathBackwardsBooleanComparison(left, right, operation, instruction.Range) : -1;
        if (backwardsBoolean >= 0)
        {
            return CreateXPathComparisonBoolean(backwardsBoolean == 1);
        }

        XdmSequence leftValues = Converter.AtomizeXdmSequence(left);
        XdmSequence rightValues = Converter.AtomizeXdmSequence(right);
        if (leftValues.Count == 0 || rightValues.Count == 0)
        {
            return CreateXPathComparisonBoolean(false);
        }

        bool backwards = Runtime.Plan.Environment.CompatibilityMode == XPathCompatibilityMode.BackwardsCompatible;
        bool relational = IsXPathRelationalComparison(operation);
        int leftIndex = 0;
        while (leftIndex < leftValues.Count)
        {
            XdmAtomicValue leftValue = ReadXPathComparisonAtomic(leftValues, leftIndex, instruction.Range);
            if (backwards && relational)
            {
                leftValue = ConvertXPathComparisonNumber(leftValue);
            }

            int rightIndex = 0;
            while (rightIndex < rightValues.Count)
            {
                XdmAtomicValue rightValue = ReadXPathComparisonAtomic(rightValues, rightIndex, instruction.Range);
                if (backwards && relational)
                {
                    rightValue = ConvertXPathComparisonNumber(rightValue);
                }

                if (CompareXPathAtomicPair(leftValue, rightValue, operation, true, instruction.Range))
                {
                    return CreateXPathComparisonBoolean(true);
                }

                rightIndex = rightIndex + 1;
            }

            leftIndex = leftIndex + 1;
        }

        return CreateXPathComparisonBoolean(false);
    }

    internal XdmSequence? EvaluateXPathRelationalRangeComparison(XPathPlanInstruction leftInstruction, XPathPlanInstruction rightInstruction, XPathPlanInstruction comparison)
    {
        if (Runtime.Plan.Environment.CompatibilityMode == XPathCompatibilityMode.BackwardsCompatible)
        {
            return null;
        }

        string operation = comparison.Operator == null ? "" : comparison.Operator;
        if (!IsXPathRelationalComparison(operation))
        {
            return null;
        }

        bool rangeOnLeft = leftInstruction.Opcode == XPathPlanOpcode.Range;
        XPathPlanInstruction? range = rangeOnLeft ? leftInstruction : rightInstruction.Opcode == XPathPlanOpcode.Range ? rightInstruction : null;
        if (range == null || range.Left == null || range.Right == null)
        {
            return null;
        }

        XdmAtomicValue? firstValue = Runtime.Operators.ConvertXPathRangeOperand(Runtime.EvaluateXPathInstruction(range.Left), range.Left.Range);
        XdmAtomicValue? lastValue = Runtime.Operators.ConvertXPathRangeOperand(Runtime.EvaluateXPathInstruction(range.Right), range.Right.Range);
        if (firstValue == null || lastValue == null)
        {
            return CreateXPathComparisonBoolean(false);
        }

        BigInteger first = new XdmNumericDatatype().ReadXdmIntegerValue(firstValue);
        BigInteger last = new XdmNumericDatatype().ReadXdmIntegerValue(lastValue);
        if (first > last)
        {
            return CreateXPathComparisonBoolean(false);
        }

        XdmAtomicValue endpoint;
        if (rangeOnLeft)
        {
            endpoint = operation == "<" || operation == "<=" ? firstValue : lastValue;
        }
        else
        {
            endpoint = operation == "<" || operation == "<=" ? lastValue : firstValue;
        }

        XPathPlanInstruction otherInstruction = rangeOnLeft ? rightInstruction : leftInstruction;
        XdmSequence other = Converter.AtomizeXdmSequence(Runtime.EvaluateXPathInstruction(otherInstruction));
        int index = 0;
        while (index < other.Count)
        {
            XdmAtomicValue candidate = ReadXPathComparisonAtomic(other, index, comparison.Range);
            bool matches = rangeOnLeft
                ? CompareXPathAtomicPair(endpoint, candidate, operation, true, comparison.Range)
                : CompareXPathAtomicPair(candidate, endpoint, operation, true, comparison.Range);
            if (matches)
            {
                return CreateXPathComparisonBoolean(true);
            }

            index = index + 1;
        }

        return CreateXPathComparisonBoolean(false);
    }

    internal int EvaluateXPathBackwardsBooleanComparison(XdmSequence left, XdmSequence right, string operation, SourceRange range)
    {
        bool leftBoolean = IsXPathSingletonBoolean(left);
        bool rightBoolean = IsXPathSingletonBoolean(right);
        if (!leftBoolean && !rightBoolean)
        {
            return -1;
        }

        bool leftValue = leftBoolean ? ReadXPathComparisonAtomic(left, 0, range).BooleanValue : Converter.EffectiveBooleanValueXdmSequence(left);
        bool rightValue = rightBoolean ? ReadXPathComparisonAtomic(right, 0, range).BooleanValue : Converter.EffectiveBooleanValueXdmSequence(right);
        XdmAtomicValue first = Runtime.Factory.CreateXdmBooleanAtomicValue(leftValue ? "true" : "false");
        XdmAtomicValue second = Runtime.Factory.CreateXdmBooleanAtomicValue(rightValue ? "true" : "false");
        return CompareXPathAtomicPair(first, second, operation, false, range) ? 1 : 0;
    }

    internal XdmSequence EvaluateXPathValueComparison(XPathPlanInstruction instruction)
    {
        XPathPlanInstruction? leftInstruction = instruction.Left;
        XPathPlanInstruction? rightInstruction = instruction.Right;
        if (leftInstruction == null || rightInstruction == null)
        {
            throw new InvalidOperationException("An XPath comparison instruction requires two operands.");
        }

        XdmSequence left = Converter.AtomizeXdmSequence(Runtime.EvaluateXPathInstruction(leftInstruction));
        XdmSequence right = Converter.AtomizeXdmSequence(Runtime.EvaluateXPathInstruction(rightInstruction));
        if (left.Count == 0 || right.Count == 0)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        if (left.Count != 1 || right.Count != 1)
        {
            throw Runtime.CreateXPathRuntimeError("A value comparison operand must be empty or a singleton atomic value.", XPathErrorCodes.XPTY0004, instruction.Range);
        }

        XdmAtomicValue leftValue = ConvertXPathValueUntypedAtomic(ReadXPathComparisonAtomic(left, 0, instruction.Range));
        XdmAtomicValue rightValue = ConvertXPathValueUntypedAtomic(ReadXPathComparisonAtomic(right, 0, instruction.Range));
        string operation = instruction.Operator == null ? "" : instruction.Operator;
        return CreateXPathComparisonBoolean(CompareXPathAtomicPair(leftValue, rightValue, operation, false, instruction.Range));
    }

    internal XdmSequence EvaluateXPathNodeComparison(XPathPlanInstruction instruction)
    {
        XPathPlanInstruction? leftInstruction = instruction.Left;
        XPathPlanInstruction? rightInstruction = instruction.Right;
        if (leftInstruction == null || rightInstruction == null)
        {
            throw new InvalidOperationException("An XPath comparison instruction requires two operands.");
        }

        XdmSequence left = Runtime.EvaluateXPathInstruction(leftInstruction);
        XdmSequence right = Runtime.EvaluateXPathInstruction(rightInstruction);
        if (left.Count == 0 || right.Count == 0)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        if (left.Count != 1 || right.Count != 1)
        {
            throw Runtime.CreateXPathRuntimeError("A node comparison operand must be empty or a singleton node.", XPathErrorCodes.XPTY0004, instruction.Range);
        }

        XdmSequenceItem leftItem = Sequences.ReadXdmSequenceItem(left, 0);
        XdmSequenceItem rightItem = Sequences.ReadXdmSequenceItem(right, 0);
        if (leftItem.Kind != XdmItemKind.Node || leftItem.Node == null || rightItem.Kind != XdmItemKind.Node || rightItem.Node == null)
        {
            throw Runtime.CreateXPathRuntimeError("A node comparison operand must be a node.", XPathErrorCodes.XPTY0004, instruction.Range);
        }

        XdmNode leftNode = Runtime.Paths.Nodes.WrapXmlNode(leftItem.Node);
        XdmNode rightNode = Runtime.Paths.Nodes.WrapXmlNode(rightItem.Node);
        string operation = instruction.Operator == null ? "" : instruction.Operator;
        if (operation == "is")
        {
            return CreateXPathComparisonBoolean(Runtime.Paths.HasSameXPathNodeIdentity(leftNode, rightNode));
        }

        Runtime.Paths.RequireXPathStreamOrdering(leftNode, instruction.Range);
        Runtime.Paths.RequireXPathStreamOrdering(rightNode, instruction.Range);
        int order = Runtime.Paths.CompareXPathNodeOrder(leftNode, rightNode);
        return CreateXPathComparisonBoolean(operation == "<<" ? order < 0 : order > 0);
    }

    internal bool CompareXPathAtomicPair(XdmAtomicValue suppliedLeft, XdmAtomicValue suppliedRight, string operation, bool general, SourceRange range)
    {
        XdmAtomicValue left = suppliedLeft;
        XdmAtomicValue right = suppliedRight;
        if (general)
        {
            if (Runtime.Plan.Environment.CompatibilityMode == XPathCompatibilityMode.BackwardsCompatible)
            {
                if (Types.IsXdmNumericAtomicType(left.Type) || Types.IsXdmNumericAtomicType(right.Type))
                {
                    left = ConvertXPathComparisonNumber(left);
                    right = ConvertXPathComparisonNumber(right);
                }
                else if (Types.IsXdmStringDerivedAtomicType(left.Type) || Types.IsXdmStringDerivedAtomicType(right.Type) || left.Type == XdmAtomicType.UntypedAtomic && right.Type == XdmAtomicType.UntypedAtomic)
                {
                    left = ConvertXPathComparisonAtomic(left, XdmAtomicType.String, range);
                    right = ConvertXPathComparisonAtomic(right, XdmAtomicType.String, range);
                }
                else if (left.Type == XdmAtomicType.UntypedAtomic)
                {
                    left = ConvertXPathComparisonAtomic(left, right.Type, range);
                }
                else if (right.Type == XdmAtomicType.UntypedAtomic)
                {
                    right = ConvertXPathComparisonAtomic(right, left.Type, range);
                }
            }
            else if (left.Type == XdmAtomicType.UntypedAtomic && right.Type == XdmAtomicType.UntypedAtomic)
            {
                left = ConvertXPathComparisonAtomic(left, XdmAtomicType.String, range);
                right = ConvertXPathComparisonAtomic(right, XdmAtomicType.String, range);
            }
            else if (left.Type == XdmAtomicType.UntypedAtomic)
            {
                left = ConvertXPathGeneralUntypedAtomic(left, right.Type, range);
            }
            else if (right.Type == XdmAtomicType.UntypedAtomic)
            {
                right = ConvertXPathGeneralUntypedAtomic(right, left.Type, range);
            }
        }

        return CompareXPathConvertedAtomicPair(left, right, operation, range);
    }

    internal bool CompareXPathConvertedAtomicPair(XdmAtomicValue left, XdmAtomicValue right, string operation, SourceRange range)
    {
        bool equality = IsXPathEqualityComparison(operation);
        if (IsXPathStringComparisonType(left.Type) && IsXPathStringComparisonType(right.Type))
        {
            int stringOrder = CompareXPathStrings(Converter.ReadXdmAtomicStringValue(left), Converter.ReadXdmAtomicStringValue(right), range);
            return ApplyXPathComparisonOperation(operation, stringOrder, false);
        }

        if (Types.IsXdmNumericAtomicType(left.Type) && Types.IsXdmNumericAtomicType(right.Type))
        {
            int numericOrder = new XdmNumericDatatype().CompareXdmNumeric(left, right);
            bool unordered = numericOrder == XdmFloatingDatatype.UnorderedComparison;
            return ApplyXPathComparisonOperation(operation, numericOrder, unordered);
        }

        XdmAtomicType leftPrimitive = ReadXPathPrimitiveAtomicType(left.Type);
        XdmAtomicType rightPrimitive = ReadXPathPrimitiveAtomicType(right.Type);
        if (leftPrimitive != rightPrimitive)
        {
            throw CreateXPathComparisonTypeError(left, right, range);
        }

        if (leftPrimitive == XdmAtomicType.Boolean)
        {
            return ApplyXPathComparisonOperation(operation, new XdmNumericDatatype().CompareXdmBoolean(left, right), false);
        }

        if (leftPrimitive == XdmAtomicType.Duration)
        {
            if (equality)
            {
                bool equal = new XdmDurationDatatype().EqualXdmDuration(left, right);
                return operation == "=" || operation == "eq" ? equal : !equal;
            }

            bool orderedDurations = left.Type == right.Type && (left.Type == XdmAtomicType.YearMonthDuration || left.Type == XdmAtomicType.DayTimeDuration);
            if (!orderedDurations)
            {
                throw CreateXPathComparisonTypeError(left, right, range);
            }

            return ApplyXPathComparisonOperation(operation, new XdmDurationDatatype().CompareXdmDuration(left, right), false);
        }

        if (leftPrimitive >= XdmAtomicType.DateTime && leftPrimitive <= XdmAtomicType.GDay)
        {
            XdmDateTimeDatatype dates = new XdmDateTimeDatatype();
            if (equality)
            {
                bool equal = dates.EqualXdmDateTime(left, right, Runtime.Context.Snapshot.ImplicitTimezoneMinutes);
                return operation == "=" || operation == "eq" ? equal : !equal;
            }

            bool orderedDates = leftPrimitive == XdmAtomicType.DateTime || leftPrimitive == XdmAtomicType.Date || leftPrimitive == XdmAtomicType.Time;
            if (!orderedDates)
            {
                throw CreateXPathComparisonTypeError(left, right, range);
            }

            return ApplyXPathComparisonOperation(operation, dates.CompareXdmDateTime(left, right, Runtime.Context.Snapshot.ImplicitTimezoneMinutes), false);
        }

        if (leftPrimitive == XdmAtomicType.HexBinary || leftPrimitive == XdmAtomicType.Base64Binary)
        {
            if (left.Type != right.Type)
            {
                throw CreateXPathComparisonTypeError(left, right, range);
            }

            return ApplyXPathComparisonOperation(operation, new XdmBinaryDatatype().CompareXdmBinary(left, right), false);
        }

        if (leftPrimitive == XdmAtomicType.QName || leftPrimitive == XdmAtomicType.Notation)
        {
            if (!equality || left.Type != right.Type)
            {
                throw CreateXPathComparisonTypeError(left, right, range);
            }

            bool equal = leftPrimitive == XdmAtomicType.QName ? new XdmQNameDatatype().EqualXdmQName(left, right) : new XdmQNameDatatype().EqualXdmNotation(left, right);
            return operation == "=" || operation == "eq" ? equal : !equal;
        }

        throw CreateXPathComparisonTypeError(left, right, range);
    }

    internal XdmAtomicValue ConvertXPathValueUntypedAtomic(XdmAtomicValue value)
    {
        return value.Type == XdmAtomicType.UntypedAtomic ? new XdmStringDatatype().ParseXdmStringAtomic(Converter.ReadXdmAtomicStringValue(value), XdmAtomicType.String) : value;
    }

    internal XdmAtomicValue ConvertXPathGeneralUntypedAtomic(XdmAtomicValue value, XdmAtomicType otherType, SourceRange range)
    {
        XdmAtomicType target = ReadXPathPrimitiveAtomicType(otherType);
        if (Types.IsXdmNumericAtomicType(otherType))
        {
            target = XdmAtomicType.Double;
        }
        else if (otherType == XdmAtomicType.DayTimeDuration || otherType == XdmAtomicType.YearMonthDuration)
        {
            target = otherType;
        }

        return ConvertXPathComparisonAtomic(value, target, range);
    }

    internal XdmAtomicValue ConvertXPathComparisonAtomic(XdmAtomicValue value, XdmAtomicType target, SourceRange range)
    {
        XPathStaticContext context = new XPathStaticContext();
        context.Namespaces = Runtime.Plan.Environment.Namespaces;
        context.DefaultElementNamespace = Runtime.Plan.Environment.DefaultElementNamespace;
        XdmConversionResult converted = Converter.TryCastXdmAtomic(value, target, context);
        if (converted.Success && converted.Value != null)
        {
            return converted.Value;
        }

        string message = converted.ErrorMessage == null ? "An untyped comparison operand cannot be converted to the required type." : converted.ErrorMessage;
        throw Runtime.CreateXPathRuntimeError(message, XPathErrorCodes.FORG0001, range);
    }

    internal XdmAtomicValue ConvertXPathComparisonNumber(XdmAtomicValue value)
    {
        XPathStaticContext context = new XPathStaticContext();
        XdmConversionResult converted = Converter.TryCastXdmAtomic(value, XdmAtomicType.Double, context);
        return converted.Success && converted.Value != null ? converted.Value : Runtime.Factory.CreateXdmFloatingAtomicValue("NaN", XdmAtomicType.Double);
    }

    internal int CompareXPathStrings(string left, string right, SourceRange range)
    {
        IXPathCollation? collation = Runtime.Plan.Environment.FindXPathCollationImplementation(Runtime.Plan.Environment.DefaultCollation);
        if (collation == null)
        {
            throw Runtime.CreateXPathRuntimeError("The default XPath collation has no registered implementation.", XPathErrorCodes.FOCH0002, range);
        }

        return collation.CompareXPathCollation(left, right);
    }

    internal bool ApplyXPathComparisonOperation(string operation, int comparison, bool unordered)
    {
        if (operation == "=" || operation == "eq")
        {
            return !unordered && comparison == 0;
        }

        if (operation == "!=" || operation == "ne")
        {
            return unordered || comparison != 0;
        }

        if (unordered)
        {
            return false;
        }

        if (operation == "<" || operation == "lt")
        {
            return comparison < 0;
        }

        if (operation == "<=" || operation == "le")
        {
            return comparison <= 0;
        }

        if (operation == ">" || operation == "gt")
        {
            return comparison > 0;
        }

        return comparison >= 0;
    }

    internal XdmAtomicType ReadXPathPrimitiveAtomicType(XdmAtomicType type)
    {
        XdmAtomicTypeDefinition? primitive = Types.GetXdmPrimitiveAtomicTypeDefinition(type);
        return primitive == null ? type : primitive.Type;
    }

    internal bool IsXPathStringComparisonType(XdmAtomicType type)
    {
        return type == XdmAtomicType.AnyUri || Types.IsXdmStringDerivedAtomicType(type);
    }

    internal bool IsXPathEqualityComparison(string operation)
    {
        return operation == "=" || operation == "!=" || operation == "eq" || operation == "ne";
    }

    internal bool IsXPathRelationalComparison(string operation)
    {
        return !IsXPathEqualityComparison(operation);
    }

    internal bool IsXPathSingletonBoolean(XdmSequence value)
    {
        if (value.Count != 1)
        {
            return false;
        }

        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(value, 0);
        return item.Kind == XdmItemKind.Atomic && item.AtomicValue != null && item.AtomicValue.Type == XdmAtomicType.Boolean;
    }

    internal XdmAtomicValue ReadXPathComparisonAtomic(XdmSequence value, int position, SourceRange range)
    {
        XdmSequenceItem item = Sequences.ReadValidatedXdmSequenceItem(value, position);
        if (item.Kind != XdmItemKind.Atomic || item.AtomicValue == null)
        {
            throw Runtime.CreateXPathRuntimeError("A comparison operand could not be atomized to an atomic value.", XPathErrorCodes.XPTY0004, range);
        }

        return item.AtomicValue;
    }

    internal XdmSequence CreateXPathComparisonBoolean(bool value)
    {
        XdmAtomicValue result = Runtime.Factory.CreateXdmBooleanAtomicValue(value ? "true" : "false");
        return Sequences.CreateXdmAtomicSequence(result);
    }

    internal XPathDynamicException CreateXPathComparisonTypeError(XdmAtomicValue left, XdmAtomicValue right, SourceRange range)
    {
        string message = "The comparison is not defined between " + left.Type.ToString() + " and " + right.Type.ToString() + ".";
        return Runtime.CreateXPathRuntimeError(message, XPathErrorCodes.XPTY0004, range);
    }

}
