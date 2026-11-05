using System;
using System.Numerics;

namespace xml;

internal class XPathOperatorEvaluator
{
    internal XPathRuntime Runtime;
    internal XdmTypeConverter Converter;
    internal XdmTypeSystem Types;
    internal XdmSequenceOperations Sequences;
    internal XdmNumericDatatype Numerics;
    internal XdmDurationDatatype Durations;
    internal XdmDateTimeDatatype DateTimes;

    internal XPathOperatorEvaluator(XPathRuntime runtime)
    {
        Runtime = runtime;
        Converter = new XdmTypeConverter();
        Types = new XdmTypeSystem();
        Sequences = new XdmSequenceOperations();
        Numerics = new XdmNumericDatatype();
        Durations = new XdmDurationDatatype();
        DateTimes = new XdmDateTimeDatatype();
    }

    internal XdmSequence EvaluateXPathOperator(XPathPlanInstruction instruction)
    {
        if (instruction.Opcode == XPathPlanOpcode.Or || instruction.Opcode == XPathPlanOpcode.And)
        {
            return EvaluateXPathLogicalOperator(instruction);
        }

        if (instruction.Opcode == XPathPlanOpcode.StringConcatenation)
        {
            return EvaluateXPathStringConcatenation(instruction);
        }

        if (instruction.Opcode == XPathPlanOpcode.Range)
        {
            return EvaluateXPathRange(instruction);
        }

        if (instruction.Opcode == XPathPlanOpcode.Union || instruction.Opcode == XPathPlanOpcode.Intersect || instruction.Opcode == XPathPlanOpcode.Except)
        {
            return EvaluateXPathNodeSetOperator(instruction);
        }

        if (instruction.Opcode == XPathPlanOpcode.UnaryPlus || instruction.Opcode == XPathPlanOpcode.UnaryMinus)
        {
            return EvaluateXPathUnaryArithmetic(instruction);
        }

        return EvaluateXPathBinaryArithmetic(instruction);
    }

    internal XdmSequence EvaluateXPathLogicalOperator(XPathPlanInstruction instruction)
    {
        XPathPlanInstruction? leftInstruction = instruction.Left;
        XPathPlanInstruction? rightInstruction = instruction.Right;
        if (leftInstruction == null || rightInstruction == null)
        {
            throw new InvalidOperationException("An XPath logical instruction requires two operands.");
        }

        bool left = EvaluateXPathEffectiveBooleanValue(Runtime.EvaluateXPathInstruction(leftInstruction), leftInstruction.Range);
        if (instruction.Opcode == XPathPlanOpcode.And && !left)
        {
            return CreateXPathOperatorBoolean(false);
        }

        if (instruction.Opcode == XPathPlanOpcode.Or && left)
        {
            return CreateXPathOperatorBoolean(true);
        }

        bool right = EvaluateXPathEffectiveBooleanValue(Runtime.EvaluateXPathInstruction(rightInstruction), rightInstruction.Range);
        return CreateXPathOperatorBoolean(right);
    }

    internal XdmSequence EvaluateXPathStringConcatenation(XPathPlanInstruction instruction)
    {
        XPathPlanInstruction? leftInstruction = instruction.Left;
        XPathPlanInstruction? rightInstruction = instruction.Right;
        if (leftInstruction == null || rightInstruction == null)
        {
            throw new InvalidOperationException("An XPath string concatenation instruction requires two operands.");
        }

        string left = ConvertXPathConcatenationOperand(Runtime.EvaluateXPathInstruction(leftInstruction), leftInstruction.Range);
        string right = ConvertXPathConcatenationOperand(Runtime.EvaluateXPathInstruction(rightInstruction), rightInstruction.Range);
        XdmAtomicValue result = Runtime.Factory.CreateXdmStringAtomicValue(left + right, XdmAtomicType.String);
        return Sequences.CreateXdmAtomicSequence(result);
    }

    internal string ConvertXPathConcatenationOperand(XdmSequence operand, SourceRange range)
    {
        XdmSequence atomized = Converter.AtomizeXdmSequence(operand);
        if (atomized.Count == 0)
        {
            return "";
        }

        if (atomized.Count != 1)
        {
            throw Runtime.CreateXPathRuntimeError("A string concatenation operand must atomize to zero or one value.", XPathErrorCodes.XPTY0004, range);
        }

        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(atomized, 0);
        if (item.Kind != XdmItemKind.Atomic || item.AtomicValue == null)
        {
            throw Runtime.CreateXPathRuntimeError("A string concatenation operand must be atomizable.", XPathErrorCodes.XPTY0004, range);
        }

        return Converter.ReadXdmAtomicStringValue(item.AtomicValue);
    }

    internal XdmSequence EvaluateXPathRange(XPathPlanInstruction instruction)
    {
        XPathPlanInstruction? leftInstruction = instruction.Left;
        XPathPlanInstruction? rightInstruction = instruction.Right;
        if (leftInstruction == null || rightInstruction == null)
        {
            throw new InvalidOperationException("An XPath range instruction requires two operands.");
        }

        XdmAtomicValue? left = ConvertXPathRangeOperand(Runtime.EvaluateXPathInstruction(leftInstruction), leftInstruction.Range);
        XdmAtomicValue? right = ConvertXPathRangeOperand(Runtime.EvaluateXPathInstruction(rightInstruction), rightInstruction.Range);
        if (left == null || right == null)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        BigInteger first = Numerics.ReadXdmIntegerValue(left);
        BigInteger last = Numerics.ReadXdmIntegerValue(right);
        if (first > last)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        BigInteger count = last - first + BigInteger.One;
        long maximumItems = Runtime.Context.Limits.MaximumNodeCount > 0 ? Runtime.Context.Limits.MaximumNodeCount : 1000000;
        if (count > new BigInteger(maximumItems) || count > new BigInteger(int.MaxValue))
        {
            throw Runtime.Limiter.CreateXPathRuntimeLimit("sequence item count", maximumItems, instruction.Range);
        }

        XdmSequenceBuilder builder = new XdmSequenceBuilder();
        BigInteger current = first;
        while (current <= last)
        {
            Runtime.Limiter.CountXPathWork(instruction.Range);
            builder.AddXdmAtomicValue(Numerics.CreateXdmIntegerValue(current, XdmAtomicType.Integer, null));
            current = current + BigInteger.One;
        }

        return builder.BuildImmutableXdmSequence();
    }

    internal XdmAtomicValue? ConvertXPathRangeOperand(XdmSequence operand, SourceRange range)
    {
        XdmSequence atomized = Converter.AtomizeXdmSequence(operand);
        if (atomized.Count == 0)
        {
            return null;
        }

        if (atomized.Count != 1)
        {
            throw Runtime.CreateXPathRuntimeError("A range operand must atomize to zero or one integer.", XPathErrorCodes.XPTY0004, range);
        }

        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(atomized, 0);
        if (item.Kind != XdmItemKind.Atomic || item.AtomicValue == null)
        {
            throw Runtime.CreateXPathRuntimeError("A range operand must be atomic.", XPathErrorCodes.XPTY0004, range);
        }

        XdmAtomicValue value = item.AtomicValue;
        if (value.Type == XdmAtomicType.UntypedAtomic)
        {
            value = ConvertXPathOperatorAtomic(value, XdmAtomicType.Integer, range);
        }

        if (!Types.IsXdmAtomicSubtypeOf(value.Type, XdmAtomicType.Integer))
        {
            throw Runtime.CreateXPathRuntimeError("A range operand must be an integer.", XPathErrorCodes.XPTY0004, range);
        }

        return value;
    }

    internal XdmSequence EvaluateXPathNodeSetOperator(XPathPlanInstruction instruction)
    {
        XPathPlanInstruction? leftInstruction = instruction.Left;
        XPathPlanInstruction? rightInstruction = instruction.Right;
        if (leftInstruction == null || rightInstruction == null)
        {
            throw new InvalidOperationException("An XPath node-set instruction requires two operands.");
        }

        XdmNode[] left = Runtime.Paths.ReadXPathNodes(Runtime.EvaluateXPathInstruction(leftInstruction), XPathErrorCodes.XPTY0004, leftInstruction.Range);
        XdmNode[] right = Runtime.Paths.ReadXPathNodes(Runtime.EvaluateXPathInstruction(rightInstruction), XPathErrorCodes.XPTY0004, rightInstruction.Range);
        System.Collections.Generic.List<XdmNode> result = new System.Collections.Generic.List<XdmNode>();
        int index = 0;
        while (index < left.Length)
        {
            bool include = instruction.Opcode != XPathPlanOpcode.Intersect || ContainsXPathOperatorNode(right, left[index]);
            if (instruction.Opcode == XPathPlanOpcode.Except && ContainsXPathOperatorNode(right, left[index]))
            {
                include = false;
            }

            if (include)
            {
                result.Add(left[index]);
            }

            index = index + 1;
        }

        if (instruction.Opcode == XPathPlanOpcode.Union)
        {
            index = 0;
            while (index < right.Length)
            {
                result.Add(right[index]);
                index = index + 1;
            }
        }

        return Runtime.Paths.CreateNormalizedXPathNodeSequence(result, instruction.Range);
    }

    internal bool ContainsXPathOperatorNode(XdmNode[] nodes, XdmNode candidate)
    {
        int index = 0;
        while (index < nodes.Length)
        {
            if (Runtime.Paths.HasSameXPathNodeIdentity(nodes[index], candidate))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal XdmSequence EvaluateXPathUnaryArithmetic(XPathPlanInstruction instruction)
    {
        XPathPlanInstruction? operandInstruction = instruction.Operand;
        if (operandInstruction == null)
        {
            throw new InvalidOperationException("An XPath unary arithmetic instruction requires an operand.");
        }

        XdmAtomicValue? operand = ConvertXPathArithmeticOperand(Runtime.EvaluateXPathInstruction(operandInstruction), operandInstruction.Range);
        if (operand == null)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        if (!Types.IsXdmNumericAtomicType(operand.Type))
        {
            throw Runtime.CreateXPathRuntimeError("A unary arithmetic operand must be numeric.", XPathErrorCodes.XPTY0004, instruction.Range);
        }

        XdmAtomicValue result = instruction.Opcode == XPathPlanOpcode.UnaryPlus ? Numerics.PositiveXdmNumeric(operand) : Numerics.NegateXdmNumeric(operand);
        return Sequences.CreateXdmAtomicSequence(result);
    }

    internal XdmSequence EvaluateXPathBinaryArithmetic(XPathPlanInstruction instruction)
    {
        XPathPlanInstruction? leftInstruction = instruction.Left;
        XPathPlanInstruction? rightInstruction = instruction.Right;
        if (leftInstruction == null || rightInstruction == null)
        {
            throw new InvalidOperationException("An XPath binary arithmetic instruction requires two operands.");
        }

        XdmAtomicValue? left = ConvertXPathArithmeticOperand(Runtime.EvaluateXPathInstruction(leftInstruction), leftInstruction.Range);
        XdmAtomicValue? right = ConvertXPathArithmeticOperand(Runtime.EvaluateXPathInstruction(rightInstruction), rightInstruction.Range);
        if (left == null || right == null)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        XdmAtomicValue result;
        if (!Types.IsXdmNumericAtomicType(left.Type) || !Types.IsXdmNumericAtomicType(right.Type))
        {
            result = EvaluateXPathTemporalArithmetic(instruction.Opcode, left, right, instruction.Range);
        }
        else if (instruction.Opcode == XPathPlanOpcode.Addition)
        {
            result = Numerics.AddXdmNumeric(left, right);
        }
        else if (instruction.Opcode == XPathPlanOpcode.Subtraction)
        {
            result = Numerics.SubtractXdmNumeric(left, right);
        }
        else if (instruction.Opcode == XPathPlanOpcode.Multiplication)
        {
            result = Numerics.MultiplyXdmNumeric(left, right);
        }
        else if (instruction.Opcode == XPathPlanOpcode.Division)
        {
            ValidateXPathDivisionOperands(instruction.Opcode, left, right, instruction.Range);
            result = Numerics.DivideXdmNumeric(left, right);
        }
        else if (instruction.Opcode == XPathPlanOpcode.IntegerDivision)
        {
            ValidateXPathDivisionOperands(instruction.Opcode, left, right, instruction.Range);
            result = Numerics.IntegerDivideXdmNumeric(left, right);
        }
        else
        {
            ValidateXPathDivisionOperands(instruction.Opcode, left, right, instruction.Range);
            result = Numerics.ModuloXdmNumeric(left, right);
        }

        return Sequences.CreateXdmAtomicSequence(result);
    }

    internal XdmAtomicValue? ConvertXPathArithmeticOperand(XdmSequence operand, SourceRange range)
    {
        XdmSequence atomized = Converter.AtomizeXdmSequence(operand);
        bool backwards = Runtime.Plan.Environment.CompatibilityMode == XPathCompatibilityMode.BackwardsCompatible;
        if (atomized.Count == 0)
        {
            return backwards ? Runtime.Factory.CreateXdmFloatingAtomicValue("NaN", XdmAtomicType.Double) : null;
        }

        if (atomized.Count > 1 && !backwards)
        {
            throw Runtime.CreateXPathRuntimeError("An arithmetic operand must atomize to zero or one value.", XPathErrorCodes.XPTY0004, range);
        }

        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(atomized, 0);
        if (item.Kind != XdmItemKind.Atomic || item.AtomicValue == null)
        {
            throw Runtime.CreateXPathRuntimeError("An arithmetic operand must be atomic.", XPathErrorCodes.XPTY0004, range);
        }

        XdmAtomicValue value = item.AtomicValue;
        if (backwards)
        {
            bool convertible = value.Type == XdmAtomicType.Boolean || value.Type == XdmAtomicType.UntypedAtomic || Types.IsXdmStringDerivedAtomicType(value.Type) || Types.IsXdmNumericAtomicType(value.Type);
            if (!convertible)
            {
                throw Runtime.CreateXPathRuntimeError("The arithmetic operand cannot be converted to a number.", XPathErrorCodes.XPTY0004, range);
            }

            return Runtime.Comparisons.ConvertXPathComparisonNumber(value);
        }

        if (value.Type == XdmAtomicType.UntypedAtomic)
        {
            value = ConvertXPathOperatorAtomic(value, XdmAtomicType.Double, range);
        }

        if (!Types.IsXdmNumericAtomicType(value.Type) && !IsXPathOrderedDurationType(value.Type) && !IsXPathTemporalType(value.Type))
        {
            throw Runtime.CreateXPathRuntimeError("The atomic value is not supported by XPath arithmetic.", XPathErrorCodes.XPTY0004, range);
        }

        return value;
    }

    internal XdmAtomicValue EvaluateXPathTemporalArithmetic(XPathPlanOpcode opcode, XdmAtomicValue left, XdmAtomicValue right, SourceRange range)
    {
        bool leftDuration = IsXPathOrderedDurationType(left.Type);
        bool rightDuration = IsXPathOrderedDurationType(right.Type);
        bool leftTemporal = IsXPathTemporalType(left.Type);
        bool rightTemporal = IsXPathTemporalType(right.Type);
        bool leftNumeric = Types.IsXdmNumericAtomicType(left.Type);
        bool rightNumeric = Types.IsXdmNumericAtomicType(right.Type);
        if (opcode == XPathPlanOpcode.Addition)
        {
            if (leftDuration && rightDuration && left.Type == right.Type)
            {
                return Durations.AddXdmDurations(left, right);
            }

            if (leftTemporal && rightDuration && IsXPathTemporalDurationPair(left.Type, right.Type))
            {
                return DateTimes.AddXdmDurationToDateTime(left, right);
            }

            if (leftDuration && rightTemporal && IsXPathTemporalDurationPair(right.Type, left.Type))
            {
                return DateTimes.AddXdmDurationToDateTime(right, left);
            }
        }
        else if (opcode == XPathPlanOpcode.Subtraction)
        {
            if (leftDuration && rightDuration && left.Type == right.Type)
            {
                return Durations.SubtractXdmDurations(left, right);
            }

            if (leftTemporal && rightTemporal && IsXPathComparableTemporalPair(left.Type, right.Type))
            {
                return DateTimes.SubtractXdmDateTimeValues(left, right, Runtime.Context.Snapshot.ImplicitTimezoneMinutes);
            }

            if (leftTemporal && rightDuration && IsXPathTemporalDurationPair(left.Type, right.Type))
            {
                return DateTimes.SubtractXdmDurationFromDateTime(left, right);
            }
        }
        else if (opcode == XPathPlanOpcode.Multiplication)
        {
            if (leftDuration && rightNumeric)
            {
                return Durations.MultiplyXdmDuration(left, right);
            }

            if (leftNumeric && rightDuration)
            {
                return Durations.MultiplyXdmDuration(right, left);
            }
        }
        else if (opcode == XPathPlanOpcode.Division)
        {
            if (leftDuration && rightNumeric)
            {
                return Durations.DivideXdmDuration(left, right);
            }

            if (leftDuration && rightDuration && left.Type == right.Type)
            {
                return Durations.DivideXdmDurationByDuration(left, right);
            }
        }

        throw Runtime.CreateXPathRuntimeError("The supplied atomic types are not supported by this arithmetic operator.", XPathErrorCodes.XPTY0004, range);
    }

    internal bool IsXPathOrderedDurationType(XdmAtomicType type)
    {
        return type == XdmAtomicType.YearMonthDuration || type == XdmAtomicType.DayTimeDuration;
    }

    internal bool IsXPathTemporalType(XdmAtomicType type)
    {
        return type == XdmAtomicType.DateTime || type == XdmAtomicType.DateTimeStamp || type == XdmAtomicType.Date || type == XdmAtomicType.Time;
    }

    internal bool IsXPathTemporalDurationPair(XdmAtomicType temporal, XdmAtomicType duration)
    {
        if (duration == XdmAtomicType.DayTimeDuration)
        {
            return IsXPathTemporalType(temporal);
        }

        return duration == XdmAtomicType.YearMonthDuration && temporal != XdmAtomicType.Time && IsXPathTemporalType(temporal);
    }

    internal bool IsXPathComparableTemporalPair(XdmAtomicType left, XdmAtomicType right)
    {
        bool dateTimes = (left == XdmAtomicType.DateTime || left == XdmAtomicType.DateTimeStamp) && (right == XdmAtomicType.DateTime || right == XdmAtomicType.DateTimeStamp);
        return dateTimes || left == right && (left == XdmAtomicType.Date || left == XdmAtomicType.Time);
    }

    internal XdmAtomicValue ConvertXPathOperatorAtomic(XdmAtomicValue value, XdmAtomicType target, SourceRange range)
    {
        XdmConversionResult converted = Converter.TryCastXdmAtomic(value, target, new XPathStaticContext());
        if (converted.Success && converted.Value != null)
        {
            return converted.Value;
        }

        string code = converted.ErrorCode == null ? XPathErrorCodes.FORG0001 : converted.ErrorCode;
        string message = converted.ErrorMessage == null ? "The operand cannot be converted to the required atomic type." : converted.ErrorMessage;
        throw Runtime.CreateXPathRuntimeError(message, code, range);
    }

    internal void ValidateXPathDivisionOperands(XPathPlanOpcode opcode, XdmAtomicValue left, XdmAtomicValue right, SourceRange range)
    {
        bool floating = left.Type == XdmAtomicType.Float || left.Type == XdmAtomicType.Double || right.Type == XdmAtomicType.Float || right.Type == XdmAtomicType.Double;
        bool zero = IsXPathOperatorNumericZero(right);
        if (opcode == XPathPlanOpcode.IntegerDivision && zero || !floating && zero && (opcode == XPathPlanOpcode.Division || opcode == XPathPlanOpcode.Modulo))
        {
            throw Runtime.CreateXPathRuntimeError("Division by zero is not defined for these numeric operands.", XPathErrorCodes.FOAR0001, range);
        }

        if (opcode == XPathPlanOpcode.IntegerDivision && (IsXPathOperatorNonFinite(left) || double.IsNaN(ReadXPathOperatorFloating(right))))
        {
            throw Runtime.CreateXPathRuntimeError("The floating-point operands do not have a finite integer quotient.", XPathErrorCodes.FOAR0002, range);
        }

        if (opcode == XPathPlanOpcode.IntegerDivision && floating && HasXPathOperatorInfiniteQuotient(left, right))
        {
            throw Runtime.CreateXPathRuntimeError("The floating-point integer quotient overflowed.", XPathErrorCodes.FOAR0002, range);
        }
    }

    internal bool HasXPathOperatorInfiniteQuotient(XdmAtomicValue left, XdmAtomicValue right)
    {
        XdmFloatingDatatype floating = new XdmFloatingDatatype();
        XdmAtomicType type = floating.CommonXdmFloatingType(left, right);
        double dividend = floating.ReadXdmFloatingValue(left, type);
        double divisor = floating.ReadXdmFloatingValue(right, type);
        double quotient = type == XdmAtomicType.Float ? (float)dividend / (float)divisor : dividend / divisor;
        return double.IsInfinity(quotient);
    }

    internal bool IsXPathOperatorNumericZero(XdmAtomicValue value)
    {
        if (value.Type == XdmAtomicType.Float || value.Type == XdmAtomicType.Double)
        {
            return ReadXPathOperatorFloating(value) == 0;
        }

        return Numerics.ReadXdmDecimalStorage(value).Coefficient == BigInteger.Zero;
    }

    internal bool IsXPathOperatorNonFinite(XdmAtomicValue value)
    {
        if (value.Type != XdmAtomicType.Float && value.Type != XdmAtomicType.Double)
        {
            return false;
        }

        double number = ReadXPathOperatorFloating(value);
        return double.IsNaN(number) || double.IsInfinity(number);
    }

    internal double ReadXPathOperatorFloating(XdmAtomicValue value)
    {
        if (value.Type == XdmAtomicType.Float)
        {
            return (float)value.FloatingValue;
        }

        return value.Type == XdmAtomicType.Double ? value.FloatingValue : 0;
    }

    internal bool EvaluateXPathEffectiveBooleanValue(XdmSequence value, SourceRange range)
    {
        Sequences.ValidateXdmSequenceStorage(value);
        if (value.Count == 0)
        {
            return false;
        }

        XdmSequenceItem first = Sequences.ReadXdmSequenceItem(value, 0);
        if (first.Kind == XdmItemKind.Node)
        {
            return true;
        }

        if (value.Count != 1 || first.Kind != XdmItemKind.Atomic || first.AtomicValue == null)
        {
            throw Runtime.CreateXPathRuntimeError("The sequence has no effective Boolean value.", XPathErrorCodes.FORG0006, range);
        }

        XdmAtomicValue atomic = first.AtomicValue;
        if (atomic.Type == XdmAtomicType.Boolean)
        {
            return atomic.BooleanValue;
        }

        if (atomic.Type == XdmAtomicType.UntypedAtomic || atomic.Type == XdmAtomicType.AnyUri || Types.IsXdmStringDerivedAtomicType(atomic.Type))
        {
            return Converter.ReadXdmAtomicStringValue(atomic).Length != 0;
        }

        if (Types.IsXdmNumericAtomicType(atomic.Type))
        {
            if (atomic.Type == XdmAtomicType.Float || atomic.Type == XdmAtomicType.Double)
            {
                double number = ReadXPathOperatorFloating(atomic);
                return number != 0 && !double.IsNaN(number);
            }

            return Numerics.ReadXdmDecimalStorage(atomic).Coefficient != BigInteger.Zero;
        }

        throw Runtime.CreateXPathRuntimeError("The sequence has no effective Boolean value.", XPathErrorCodes.FORG0006, range);
    }

    internal XdmSequence CreateXPathOperatorBoolean(bool value)
    {
        XdmAtomicValue result = Runtime.Factory.CreateXdmBooleanAtomicValue(value ? "true" : "false");
        return Sequences.CreateXdmAtomicSequence(result);
    }
}
