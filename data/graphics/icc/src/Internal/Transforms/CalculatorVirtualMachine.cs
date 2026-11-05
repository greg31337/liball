namespace icc;

internal class CalculatorEnvironment
{
    internal uint[] Signatures;
    internal double[] Values;

    internal CalculatorEnvironment(uint[] signatures, double[] values)
    {
        Signatures = signatures;
        Values = values;
    }

    internal bool FindValue(uint signature, double[] result)
    {
        int index = 0;
        while (index < Signatures.Length && index < Values.Length)
        {
            if (Signatures[index] == signature)
            {
                result[0] = Values[index];
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}

internal class CalculatorExecutionState
{
    internal IccResourceLimits Limits;
    internal CalculatorEnvironment Environment;
    internal int ExecutedInstructions;
    internal System.Collections.Generic.Dictionary<IccProcessingElement, bool> ActiveElements;

    internal CalculatorExecutionState(IccResourceLimits limits, CalculatorEnvironment environment)
    {
        Limits = limits;
        Environment = environment;
        ExecutedInstructions = 0;
        ActiveElements = new System.Collections.Generic.Dictionary<IccProcessingElement, bool>();
    }
}

internal class CalculatorFrame
{
    internal double[] Inputs;
    internal double[] Outputs;
    internal double[] Temporary;
    internal double[] Stack;
    internal int StackDepth;

    internal CalculatorFrame(System.ReadOnlySpan<double> inputs, int outputChannels, int storageValues, int stackValues)
    {
        Inputs = inputs.ToArray();
        Outputs = new double[outputChannels];
        Temporary = new double[storageValues];
        Stack = new double[stackValues];
        StackDepth = 0;
    }
}

internal static class CalculatorVirtualMachine
{
    internal static void Execute(IccProcessingElement element, System.ReadOnlySpan<double> inputs, System.Span<double> outputs, IccResourceLimits limits, CalculatorEnvironment environment)
    {
        if (element == null || limits == null || environment == null)
        {
            throw new IccTransformException("Calculator execution requires an element, resource limits, and an environment.");
        }
        if (limits.MaximumCalculatorInstructions < 1 || limits.MaximumCalculatorStackDepth < 1 || limits.MaximumCalculatorStorageValues < 0 || limits.MaximumNestingDepth < 1)
        {
            throw new IccTransformException("Calculator execution limits are invalid.");
        }
        if (environment.Signatures == null || environment.Values == null || environment.Signatures.Length != environment.Values.Length)
        {
            throw new IccTransformException("Calculator environment signatures and values must have equal lengths.");
        }
        if (inputs.Length != element.InputChannels || outputs.Length < element.OutputChannels)
        {
            throw new IccTransformException($"Calculator execution requires {element.InputChannels} inputs and {element.OutputChannels} outputs.");
        }

        CalculatorExecutionState state = new CalculatorExecutionState(limits, environment);
        double[] result = ExecuteCalculatorElement(element, inputs, state, 0);
        new System.ReadOnlySpan<double>(result).CopyTo(outputs);
    }

    internal static double[] ExecuteCalculatorElement(IccProcessingElement element, System.ReadOnlySpan<double> inputs, CalculatorExecutionState state, int recursionDepth)
    {
        IccCalculatorProgram? program = element.Calculator;
        if (element.Kind != IccProcessingElementKind.Calculator || element.Signature != IccProcessingElementSignatures.Calculator || program == null || program.Instructions == null || program.SubElements == null)
        {
            throw new IccTransformException("A calculator invocation requires complete calculator element data.");
        }
        if (recursionDepth >= state.Limits.MaximumNestingDepth || state.ActiveElements.ContainsKey(element))
        {
            throw new IccTransformException("Calculator invocation is circular or exceeds the configured recursion depth.");
        }
        if (program.Instructions.Length > state.Limits.MaximumCalculatorInstructions)
        {
            throw new IccTransformException($"Calculator program exceeds the configured instruction limit of {state.Limits.MaximumCalculatorInstructions}.");
        }

        string validationError = CalculatorElementCodec.ValidateProgram(program, element, state.Limits.MaximumCalculatorStackDepth, state.Limits.MaximumCalculatorStorageValues, state.Limits.MaximumNestingDepth);
        if (validationError.Length > 0 || program.UnknownOperationCount != 0)
        {
            string detail = validationError.Length > 0 ? validationError : "the calculator contains an unknown operation";
            throw new IccTransformException($"Calculator program is not executable: {detail}.");
        }

        state.ActiveElements.Add(element, true);
        int stackValues = System.Math.Max(program.RequiredStackDepth, 1);
        CalculatorFrame frame = new CalculatorFrame(inputs, element.OutputChannels, program.RequiredStorageValues, stackValues);
        ExecuteSequence(program.Instructions, 0, program.Instructions.Length, element, program, frame, state, recursionDepth);
        state.ActiveElements.Remove(element);
        return frame.Outputs;
    }

    internal static void ExecuteSequence(IccCalculatorInstruction[] instructions, int start, int count, IccProcessingElement element, IccCalculatorProgram program, CalculatorFrame frame, CalculatorExecutionState state, int recursionDepth)
    {
        int end = start + count;
        int index = start;
        while (index < end)
        {
            IccCalculatorInstruction instruction = instructions[index];
            uint signature = instruction.Signature;
            if (signature == CalculatorOperationSignatures.If)
            {
                ChargeInstruction(state);
                double condition = PopValue(frame);
                bool hasElse = index + 1 < end && instructions[index + 1].Signature == CalculatorOperationSignatures.Else;
                int trueCount = (int)instruction.Operand;
                int falseCount = hasElse ? (int)instructions[index + 1].Operand : 0;
                int streamStart = index + (hasElse ? 2 : 1);
                if (hasElse)
                {
                    ChargeInstruction(state);
                }
                if (condition >= 0.5 || System.Double.IsNaN(condition))
                {
                    ExecuteSequence(instructions, streamStart, trueCount, element, program, frame, state, recursionDepth);
                }
                else if (hasElse)
                {
                    ExecuteSequence(instructions, streamStart + trueCount, falseCount, element, program, frame, state, recursionDepth);
                }

                index = streamStart + trueCount + falseCount;
                continue;
            }
            if (signature == CalculatorOperationSignatures.Select)
            {
                ChargeInstruction(state);
                double selectorValue = PopValue(frame);
                int markerStart = index + 1;
                int markerEnd = markerStart;
                while (markerEnd < end && instructions[markerEnd].Signature == CalculatorOperationSignatures.Case)
                {
                    markerEnd = markerEnd + 1;
                }
                bool hasDefault = markerEnd < end && instructions[markerEnd].Signature == CalculatorOperationSignatures.Default;
                if (hasDefault)
                {
                    markerEnd = markerEnd + 1;
                }

                int markerCount = markerEnd - markerStart;
                int chargedMarker = 0;
                while (chargedMarker < markerCount)
                {
                    ChargeInstruction(state);
                    chargedMarker = chargedMarker + 1;
                }

                int selected = GetSelectionIndex(selectorValue);
                int caseCount = markerCount - (hasDefault ? 1 : 0);
                int streamPosition = markerEnd;
                int marker = 0;
                while (marker < markerCount)
                {
                    int branchCount = (int)instructions[markerStart + marker].Operand;
                    bool executeBranch = marker == selected || (marker == caseCount && hasDefault && (selected < 0 || selected >= caseCount));
                    if (executeBranch)
                    {
                        ExecuteSequence(instructions, streamPosition, branchCount, element, program, frame, state, recursionDepth);
                    }

                    streamPosition = streamPosition + branchCount;
                    marker = marker + 1;
                }

                index = streamPosition;
                continue;
            }
            if (signature == CalculatorOperationSignatures.Else || signature == CalculatorOperationSignatures.Case || signature == CalculatorOperationSignatures.Default)
            {
                throw new IccTransformException("A calculator control marker was reached outside its control operation.");
            }

            ChargeInstruction(state);
            int previousDepth = frame.StackDepth;
            ExecuteInstruction(instruction, element, program, frame, state, recursionDepth);
            CalculatorStackEffect effect = CalculatorElementCodec.GetStackEffect(instruction, program);
            int expectedDepth = previousDepth - effect.Consumed + effect.Produced;
            if (frame.StackDepth != expectedDepth)
            {
                throw new IccTransformException($"Calculator operation '{FourCcCodec.Format(signature)}' violated its verified stack effect.");
            }

            index = index + 1;
        }
    }

    internal static void ExecuteInstruction(IccCalculatorInstruction instruction, IccProcessingElement element, IccCalculatorProgram program, CalculatorFrame frame, CalculatorExecutionState state, int recursionDepth)
    {
        uint signature = instruction.Signature;
        if (signature == CalculatorOperationSignatures.NoOperation)
        {
            return;
        }
        if (signature == CalculatorOperationSignatures.StackCount)
        {
            PushValue(frame, frame.StackDepth);
            return;
        }
        if (signature == CalculatorOperationSignatures.Data)
        {
            PushValue(frame, instruction.FloatingPointValue);
            return;
        }
        if (signature == CalculatorOperationSignatures.Pi)
        {
            PushValue(frame, System.Math.PI);
            return;
        }
        if (signature == CalculatorOperationSignatures.PositiveInfinity)
        {
            PushValue(frame, System.Double.PositiveInfinity);
            return;
        }
        if (signature == CalculatorOperationSignatures.NegativeInfinity)
        {
            PushValue(frame, System.Double.NegativeInfinity);
            return;
        }
        if (signature == CalculatorOperationSignatures.NotANumber)
        {
            PushValue(frame, System.Double.NaN);
            return;
        }
        if (signature == CalculatorOperationSignatures.Input)
        {
            PushArrayRange(frame, frame.Inputs, instruction.FirstSelector, instruction.SecondSelector + 1);
            return;
        }
        if (signature == CalculatorOperationSignatures.Output)
        {
            PopArrayRange(frame, frame.Outputs, instruction.FirstSelector, instruction.SecondSelector + 1);
            return;
        }
        if (signature == CalculatorOperationSignatures.TemporaryGet)
        {
            PushArrayRange(frame, frame.Temporary, instruction.FirstSelector, instruction.SecondSelector + 1);
            return;
        }
        if (signature == CalculatorOperationSignatures.TemporaryPut)
        {
            PopArrayRange(frame, frame.Temporary, instruction.FirstSelector, instruction.SecondSelector + 1);
            return;
        }
        if (signature == CalculatorOperationSignatures.TemporarySave)
        {
            SaveArrayRange(frame, frame.Temporary, instruction.FirstSelector, instruction.SecondSelector + 1);
            return;
        }
        if (signature == CalculatorOperationSignatures.Environment)
        {
            PushEnvironment(frame, state.Environment, instruction.Operand);
            return;
        }
        if (CalculatorElementCodec.IsSubElementOperation(signature))
        {
            ApplySubElement(frame, program.SubElements[instruction.FirstSelector], state, recursionDepth + 1);
            return;
        }
        if (signature == CalculatorOperationSignatures.Copy)
        {
            CopyStackValues(frame, instruction.FirstSelector + 1, instruction.SecondSelector + 1);
            return;
        }
        if (signature == CalculatorOperationSignatures.RotateLeft || signature == CalculatorOperationSignatures.RotateRight)
        {
            RotateStackValues(frame, instruction.FirstSelector + 1, instruction.SecondSelector + 1, signature == CalculatorOperationSignatures.RotateRight);
            return;
        }
        if (signature == CalculatorOperationSignatures.PositionDuplicate)
        {
            DuplicateStackPosition(frame, instruction.FirstSelector, instruction.SecondSelector + 1);
            return;
        }
        if (signature == CalculatorOperationSignatures.Flip)
        {
            FlipStackValues(frame, instruction.FirstSelector + 2);
            return;
        }
        if (signature == CalculatorOperationSignatures.Pop)
        {
            frame.StackDepth = frame.StackDepth - instruction.FirstSelector - 1;
            return;
        }
        if (CalculatorElementCodec.IsSequenceOperation(signature))
        {
            ApplySequenceOperation(frame, signature, instruction.FirstSelector + 2);
            return;
        }
        if (signature == CalculatorOperationSignatures.Solve)
        {
            SolveMatrix(frame, instruction.FirstSelector + 1, instruction.SecondSelector + 1);
            return;
        }
        if (signature == CalculatorOperationSignatures.Transpose)
        {
            TransposeMatrix(frame, instruction.FirstSelector + 1, instruction.SecondSelector + 1);
            return;
        }
        if (CalculatorElementCodec.IsScalarVectorOperation(signature))
        {
            ApplyScalarOperation(frame, signature, instruction.FirstSelector + 1);
            return;
        }
        if (CalculatorElementCodec.IsBinaryVectorOperation(signature))
        {
            ApplyBinaryOperation(frame, signature, instruction.FirstSelector + 1);
            return;
        }
        if (signature == CalculatorOperationSignatures.CartesianToPolar || signature == CalculatorOperationSignatures.PolarToCartesian)
        {
            ApplyCoordinateOperation(frame, signature, instruction.FirstSelector + 1);
            return;
        }
        if (signature == CalculatorOperationSignatures.ToLab || signature == CalculatorOperationSignatures.ToXyz)
        {
            ApplyLabOperation(frame, signature, instruction.FirstSelector + 1);
            return;
        }
        if (CalculatorElementCodec.IsUnaryVectorOperation(signature))
        {
            ApplyUnaryOperation(frame, signature, instruction.FirstSelector + 1);
            return;
        }

        throw new IccTransformException($"Calculator operation '{FourCcCodec.Format(signature)}' is unsupported.");
    }

    internal static void ChargeInstruction(CalculatorExecutionState state)
    {
        if (state.ExecutedInstructions >= state.Limits.MaximumCalculatorInstructions)
        {
            throw new IccTransformException($"Calculator execution exceeded the configured instruction limit of {state.Limits.MaximumCalculatorInstructions}.");
        }

        state.ExecutedInstructions = state.ExecutedInstructions + 1;
    }

    internal static void PushValue(CalculatorFrame frame, double value)
    {
        if (frame.StackDepth >= frame.Stack.Length)
        {
            throw new IccTransformException("Calculator operand stack overflowed its verified bound.");
        }

        frame.Stack[frame.StackDepth] = value;
        frame.StackDepth = frame.StackDepth + 1;
    }

    internal static double PopValue(CalculatorFrame frame)
    {
        if (frame.StackDepth < 1)
        {
            throw new IccTransformException("Calculator operand stack underflowed.");
        }

        frame.StackDepth = frame.StackDepth - 1;
        return frame.Stack[frame.StackDepth];
    }

    internal static void PushArrayRange(CalculatorFrame frame, double[] values, int start, int count)
    {
        int index = 0;
        while (index < count)
        {
            PushValue(frame, values[start + index]);
            index = index + 1;
        }
    }

    internal static void PopArrayRange(CalculatorFrame frame, double[] values, int start, int count)
    {
        int source = frame.StackDepth - count;
        int index = 0;
        while (index < count)
        {
            values[start + index] = frame.Stack[source + index];
            index = index + 1;
        }

        frame.StackDepth = source;
    }

    internal static void SaveArrayRange(CalculatorFrame frame, double[] values, int start, int count)
    {
        int source = frame.StackDepth - count;
        int index = 0;
        while (index < count)
        {
            values[start + index] = frame.Stack[source + index];
            index = index + 1;
        }
    }

    internal static void PushEnvironment(CalculatorFrame frame, CalculatorEnvironment environment, uint signature)
    {
        if (signature == 0x74727565)
        {
            PushValue(frame, 1.0);
            PushValue(frame, 1.0);
            return;
        }
        if (signature == 0x6e646566)
        {
            PushValue(frame, 0.0);
            PushValue(frame, 0.0);
            return;
        }

        double[] result = new double[1];
        bool found = environment.FindValue(signature, result);
        PushValue(frame, found ? result[0] : 0.0);
        PushValue(frame, found ? 1.0 : 0.0);
    }

    internal static void CopyStackValues(CalculatorFrame frame, int count, int copies)
    {
        int source = frame.StackDepth - count;
        int copy = 0;
        while (copy < copies)
        {
            int index = 0;
            while (index < count)
            {
                PushValue(frame, frame.Stack[source + index]);
                index = index + 1;
            }

            copy = copy + 1;
        }
    }

    internal static void RotateStackValues(CalculatorFrame frame, int count, int positions, bool right)
    {
        int start = frame.StackDepth - count;
        double[] copy = new double[count];
        int shift = positions % count;
        int index = 0;
        while (index < count)
        {
            copy[index] = frame.Stack[start + index];
            index = index + 1;
        }

        index = 0;
        while (index < count)
        {
            int source = right ? (index + count - shift) % count : (index + shift) % count;
            frame.Stack[start + index] = copy[source];
            index = index + 1;
        }
    }

    internal static void DuplicateStackPosition(CalculatorFrame frame, int position, int copies)
    {
        double value = frame.Stack[frame.StackDepth - position - 1];
        int index = 0;
        while (index < copies)
        {
            PushValue(frame, value);
            index = index + 1;
        }
    }

    internal static void FlipStackValues(CalculatorFrame frame, int count)
    {
        int start = frame.StackDepth - count;
        int index = 0;
        while (index < count / 2)
        {
            double value = frame.Stack[start + index];
            frame.Stack[start + index] = frame.Stack[frame.StackDepth - index - 1];
            frame.Stack[frame.StackDepth - index - 1] = value;
            index = index + 1;
        }
    }

    internal static void ApplySequenceOperation(CalculatorFrame frame, uint signature, int count)
    {
        int start = frame.StackDepth - count;
        double result = frame.Stack[start];
        int index = 1;
        if (signature == CalculatorOperationSignatures.And)
        {
            result = result >= 0.5 ? 1.0 : 0.0;
        }
        else if (signature == CalculatorOperationSignatures.Or)
        {
            result = result >= 0.5 ? 1.0 : 0.0;
        }
        while (index < count)
        {
            double value = frame.Stack[start + index];
            if (signature == CalculatorOperationSignatures.Sum)
            {
                result = result + value;
            }
            else if (signature == CalculatorOperationSignatures.Product)
            {
                result = result * value;
            }
            else if (signature == CalculatorOperationSignatures.Minimum)
            {
                result = System.Math.Min(result, value);
            }
            else if (signature == CalculatorOperationSignatures.Maximum)
            {
                result = System.Math.Max(result, value);
            }
            else if (signature == CalculatorOperationSignatures.And)
            {
                result = result >= 0.5 && value >= 0.5 ? 1.0 : 0.0;
            }
            else
            {
                result = result >= 0.5 || value >= 0.5 ? 1.0 : 0.0;
            }

            index = index + 1;
        }

        frame.Stack[start] = result;
        frame.StackDepth = start + 1;
    }

    internal static void ApplyScalarOperation(CalculatorFrame frame, uint signature, int count)
    {
        int start = frame.StackDepth - count - 1;
        double scalar = frame.Stack[start + count];
        int index = 0;
        while (index < count)
        {
            double value = frame.Stack[start + index];
            if (signature == CalculatorOperationSignatures.Gamma)
            {
                frame.Stack[start + index] = System.Math.Pow(value, scalar);
            }
            else if (signature == CalculatorOperationSignatures.ScalarAdd)
            {
                frame.Stack[start + index] = value + scalar;
            }
            else if (signature == CalculatorOperationSignatures.ScalarSubtract)
            {
                frame.Stack[start + index] = value - scalar;
            }
            else if (signature == CalculatorOperationSignatures.ScalarMultiply)
            {
                frame.Stack[start + index] = value * scalar;
            }
            else
            {
                frame.Stack[start + index] = value / scalar;
            }

            index = index + 1;
        }

        frame.StackDepth = frame.StackDepth - 1;
    }

    internal static void ApplyBinaryOperation(CalculatorFrame frame, uint signature, int count)
    {
        int start = frame.StackDepth - (count * 2);
        int index = 0;
        while (index < count)
        {
            double first = frame.Stack[start + index];
            double second = frame.Stack[start + count + index];
            frame.Stack[start + index] = ComputeBinaryValue(signature, first, second);
            index = index + 1;
        }

        frame.StackDepth = frame.StackDepth - count;
    }

    internal static double ComputeBinaryValue(uint signature, double first, double second)
    {
        if (signature == CalculatorOperationSignatures.Add)
        {
            return first + second;
        }
        if (signature == CalculatorOperationSignatures.Subtract)
        {
            return first - second;
        }
        if (signature == CalculatorOperationSignatures.Multiply)
        {
            return first * second;
        }
        if (signature == CalculatorOperationSignatures.Divide)
        {
            return first / second;
        }
        if (signature == CalculatorOperationSignatures.Modulus)
        {
            return first % second;
        }
        if (signature == CalculatorOperationSignatures.Power)
        {
            return System.Math.Pow(first, second);
        }
        if (signature == CalculatorOperationSignatures.ArcTangent2)
        {
            return System.Math.Atan2(second, first);
        }
        if (signature == CalculatorOperationSignatures.LessThan)
        {
            return first < second ? 1.0 : 0.0;
        }
        if (signature == CalculatorOperationSignatures.LessThanOrEqual)
        {
            return first <= second ? 1.0 : 0.0;
        }
        if (signature == CalculatorOperationSignatures.Equal)
        {
            return first == second ? 1.0 : 0.0;
        }
        if (signature == CalculatorOperationSignatures.NotEqual)
        {
            return first != second ? 1.0 : 0.0;
        }
        if (signature == CalculatorOperationSignatures.Near)
        {
            return second - 0.00000001 <= first && first <= second + 0.00000001 ? 1.0 : 0.0;
        }
        if (signature == CalculatorOperationSignatures.GreaterThanOrEqual)
        {
            return first >= second ? 1.0 : 0.0;
        }
        if (signature == CalculatorOperationSignatures.GreaterThan)
        {
            return first > second ? 1.0 : 0.0;
        }
        if (signature == CalculatorOperationSignatures.VectorMinimum)
        {
            return System.Math.Min(first, second);
        }
        if (signature == CalculatorOperationSignatures.VectorMaximum)
        {
            return System.Math.Max(first, second);
        }
        if (signature == CalculatorOperationSignatures.VectorAnd)
        {
            return first >= 0.5 && second >= 0.5 ? 1.0 : 0.0;
        }
        if (signature == CalculatorOperationSignatures.VectorXor)
        {
            return (first >= 0.5) != (second >= 0.5) ? 1.0 : 0.0;
        }

        return first >= 0.5 || second >= 0.5 ? 1.0 : 0.0;
    }

    internal static void ApplyUnaryOperation(CalculatorFrame frame, uint signature, int count)
    {
        int start = frame.StackDepth - count;
        int index = 0;
        while (index < count)
        {
            frame.Stack[start + index] = ComputeUnaryValue(signature, frame.Stack[start + index]);
            index = index + 1;
        }
    }

    internal static double ComputeUnaryValue(uint signature, double value)
    {
        if (signature == CalculatorOperationSignatures.Negate)
        {
            return -value;
        }
        if (signature == CalculatorOperationSignatures.Square)
        {
            return value * value;
        }
        if (signature == CalculatorOperationSignatures.SquareRoot)
        {
            return System.Math.Sqrt(value);
        }
        if (signature == CalculatorOperationSignatures.Cube)
        {
            return value * value * value;
        }
        if (signature == CalculatorOperationSignatures.CubeRoot)
        {
            return System.Math.Cbrt(value);
        }
        if (signature == CalculatorOperationSignatures.Sign)
        {
            return value < 0.0 ? -1.0 : (value > 0.0 ? 1.0 : 0.0);
        }
        if (signature == CalculatorOperationSignatures.Absolute)
        {
            return value < 0.0 ? -value : value;
        }
        if (signature == CalculatorOperationSignatures.Floor)
        {
            return System.Math.Floor(value);
        }
        if (signature == CalculatorOperationSignatures.Ceiling)
        {
            return System.Math.Ceiling(value);
        }
        if (signature == CalculatorOperationSignatures.Truncate)
        {
            return System.Math.Truncate(value);
        }
        if (signature == CalculatorOperationSignatures.Round)
        {
            return RoundCalculatorValue(value);
        }
        if (signature == CalculatorOperationSignatures.Exponential)
        {
            return System.Math.Exp(value);
        }
        if (signature == CalculatorOperationSignatures.Logarithm)
        {
            return System.Math.Log10(value);
        }
        if (signature == CalculatorOperationSignatures.NaturalLogarithm)
        {
            return System.Math.Log(value);
        }
        if (signature == CalculatorOperationSignatures.Sine)
        {
            return System.Math.Sin(value);
        }
        if (signature == CalculatorOperationSignatures.Cosine)
        {
            return System.Math.Cos(value);
        }
        if (signature == CalculatorOperationSignatures.Tangent)
        {
            return System.Math.Tan(value);
        }
        if (signature == CalculatorOperationSignatures.ArcSine)
        {
            return System.Math.Asin(value);
        }
        if (signature == CalculatorOperationSignatures.ArcCosine)
        {
            return System.Math.Acos(value);
        }
        if (signature == CalculatorOperationSignatures.ArcTangent)
        {
            return System.Math.Atan(value);
        }
        if (signature == CalculatorOperationSignatures.RealNumber)
        {
            return System.Double.IsFinite(value) ? 1.0 : 0.0;
        }

        return value < 0.5 ? 1.0 : 0.0;
    }

    internal static double RoundCalculatorValue(double value)
    {
        return value < 0.0 ? System.Math.Truncate(value - 0.5) : System.Math.Truncate(value + 0.5);
    }

    internal static int GetSelectionIndex(double value)
    {
        double rounded = RoundCalculatorValue(value);
        if (!System.Double.IsFinite(rounded) || rounded < int.MinValue || rounded > int.MaxValue)
        {
            return -1;
        }

        return (int)rounded;
    }

    internal static void ApplyCoordinateOperation(CalculatorFrame frame, uint signature, int count)
    {
        int start = frame.StackDepth - (count * 2);
        int index = 0;
        while (index < count)
        {
            double first = frame.Stack[start + index];
            double second = frame.Stack[start + count + index];
            if (signature == CalculatorOperationSignatures.CartesianToPolar)
            {
                double angle = System.Math.Atan2(second, first) * 180.0 / System.Math.PI;
                if (angle < 0.0)
                {
                    angle = angle + 360.0;
                }
                frame.Stack[start + index] = System.Math.Sqrt((first * first) + (second * second));
                frame.Stack[start + count + index] = angle;
            }
            else
            {
                double radians = second * System.Math.PI / 180.0;
                frame.Stack[start + index] = first * System.Math.Cos(radians);
                frame.Stack[start + count + index] = first * System.Math.Sin(radians);
            }

            index = index + 1;
        }
    }

    internal static void ApplyLabOperation(CalculatorFrame frame, uint signature, int count)
    {
        int start = frame.StackDepth - (count * 3);
        int index = 0;
        while (index < count)
        {
            double first = frame.Stack[start + index];
            double second = frame.Stack[start + count + index];
            double third = frame.Stack[start + (count * 2) + index];
            if (signature == CalculatorOperationSignatures.ToLab)
            {
                double fx = LabForward(first);
                double fy = LabForward(second);
                double fz = LabForward(third);
                frame.Stack[start + index] = ((116.0 * fy) - 16.0) / 100.0;
                frame.Stack[start + count + index] = ((500.0 * (fx - fy)) + 128.0) / 255.0;
                frame.Stack[start + (count * 2) + index] = ((200.0 * (fy - fz)) + 128.0) / 255.0;
            }
            else
            {
                double fy = ((100.0 * first) + 16.0) / 116.0;
                double fx = (((255.0 * second) - 128.0) / 500.0) + fy;
                double fz = fy - (((255.0 * third) - 128.0) / 200.0);
                frame.Stack[start + index] = LabInverse(fx);
                frame.Stack[start + count + index] = LabInverse(fy);
                frame.Stack[start + (count * 2) + index] = LabInverse(fz);
            }

            index = index + 1;
        }
    }

    internal static double LabForward(double value)
    {
        double delta = 6.0 / 29.0;
        double threshold = delta * delta * delta;
        return value > threshold ? System.Math.Cbrt(value) : (value / (3.0 * delta * delta)) + (4.0 / 29.0);
    }

    internal static double LabInverse(double value)
    {
        double delta = 6.0 / 29.0;
        return value > delta ? value * value * value : 3.0 * delta * delta * (value - (4.0 / 29.0));
    }

    internal static void TransposeMatrix(CalculatorFrame frame, int rows, int columns)
    {
        int count = rows * columns;
        int start = frame.StackDepth - count;
        double[] result = new double[count];
        int row = 0;
        while (row < rows)
        {
            int column = 0;
            while (column < columns)
            {
                result[(column * rows) + row] = frame.Stack[start + (row * columns) + column];
                column = column + 1;
            }

            row = row + 1;
        }

        new System.ReadOnlySpan<double>(result).CopyTo(new System.Span<double>(frame.Stack, start, count));
    }

    internal static void SolveMatrix(CalculatorFrame frame, int rows, int columns)
    {
        int matrixCount = rows * columns;
        int start = frame.StackDepth - matrixCount - rows;
        double[] equation = new double[columns * columns];
        double[] vector = new double[columns];
        if (rows >= columns)
        {
            BuildNormalEquation(frame.Stack, start, rows, columns, equation, vector);
        }
        else
        {
            equation = new double[rows * rows];
            double[] intermediate = new double[rows];
            BuildMinimumNormEquation(frame.Stack, start, rows, columns, equation, intermediate);
            bool intermediateSolved = GaussianSolve(equation, intermediate, rows);
            if (intermediateSolved)
            {
                MultiplyTranspose(frame.Stack, start, rows, columns, intermediate, vector);
            }
            else
            {
                int clearIndex = 0;
                while (clearIndex < columns)
                {
                    vector[clearIndex] = 0.0;
                    clearIndex = clearIndex + 1;
                }
                frame.StackDepth = start;
                PushArrayRange(frame, vector, 0, columns);
                PushValue(frame, 0.0);
                return;
            }

            frame.StackDepth = start;
            PushArrayRange(frame, vector, 0, columns);
            PushValue(frame, 1.0);
            return;
        }

        bool solved = GaussianSolve(equation, vector, columns);
        if (!solved)
        {
            int index = 0;
            while (index < columns)
            {
                vector[index] = 0.0;
                index = index + 1;
            }
        }

        frame.StackDepth = start;
        PushArrayRange(frame, vector, 0, columns);
        PushValue(frame, solved ? 1.0 : 0.0);
    }

    internal static void BuildNormalEquation(double[] stack, int start, int rows, int columns, double[] equation, double[] vector)
    {
        int column = 0;
        while (column < columns)
        {
            int other = 0;
            while (other < columns)
            {
                double sum = 0.0;
                int row = 0;
                while (row < rows)
                {
                    sum = sum + (stack[start + (row * columns) + column] * stack[start + (row * columns) + other]);
                    row = row + 1;
                }
                equation[(column * columns) + other] = sum;
                other = other + 1;
            }

            double value = 0.0;
            int vectorRow = 0;
            while (vectorRow < rows)
            {
                value = value + (stack[start + (vectorRow * columns) + column] * stack[start + (rows * columns) + vectorRow]);
                vectorRow = vectorRow + 1;
            }
            vector[column] = value;
            column = column + 1;
        }
    }

    internal static void BuildMinimumNormEquation(double[] stack, int start, int rows, int columns, double[] equation, double[] vector)
    {
        int row = 0;
        while (row < rows)
        {
            int other = 0;
            while (other < rows)
            {
                double sum = 0.0;
                int column = 0;
                while (column < columns)
                {
                    sum = sum + (stack[start + (row * columns) + column] * stack[start + (other * columns) + column]);
                    column = column + 1;
                }
                equation[(row * rows) + other] = sum;
                other = other + 1;
            }
            vector[row] = stack[start + (rows * columns) + row];
            row = row + 1;
        }
    }

    internal static void MultiplyTranspose(double[] stack, int start, int rows, int columns, double[] source, double[] destination)
    {
        int column = 0;
        while (column < columns)
        {
            double sum = 0.0;
            int row = 0;
            while (row < rows)
            {
                sum = sum + (stack[start + (row * columns) + column] * source[row]);
                row = row + 1;
            }
            destination[column] = sum;
            column = column + 1;
        }
    }

    internal static bool GaussianSolve(double[] matrix, double[] vector, int size)
    {
        int pivot = 0;
        while (pivot < size)
        {
            int best = pivot;
            double bestValue = System.Math.Abs(matrix[(pivot * size) + pivot]);
            int row = pivot + 1;
            while (row < size)
            {
                double candidate = System.Math.Abs(matrix[(row * size) + pivot]);
                if (candidate > bestValue)
                {
                    best = row;
                    bestValue = candidate;
                }
                row = row + 1;
            }
            if (!System.Double.IsFinite(bestValue) || bestValue <= 0.000000000000001)
            {
                return false;
            }
            if (best != pivot)
            {
                int column = pivot;
                while (column < size)
                {
                    double swap = matrix[(pivot * size) + column];
                    matrix[(pivot * size) + column] = matrix[(best * size) + column];
                    matrix[(best * size) + column] = swap;
                    column = column + 1;
                }
                double vectorSwap = vector[pivot];
                vector[pivot] = vector[best];
                vector[best] = vectorSwap;
            }

            row = pivot + 1;
            while (row < size)
            {
                double factor = matrix[(row * size) + pivot] / matrix[(pivot * size) + pivot];
                int column = pivot;
                while (column < size)
                {
                    matrix[(row * size) + column] = matrix[(row * size) + column] - (factor * matrix[(pivot * size) + column]);
                    column = column + 1;
                }
                vector[row] = vector[row] - (factor * vector[pivot]);
                row = row + 1;
            }

            pivot = pivot + 1;
        }

        int solveRow = size - 1;
        while (solveRow >= 0)
        {
            double sum = vector[solveRow];
            int column = solveRow + 1;
            while (column < size)
            {
                sum = sum - (matrix[(solveRow * size) + column] * vector[column]);
                column = column + 1;
            }
            vector[solveRow] = sum / matrix[(solveRow * size) + solveRow];
            if (!System.Double.IsFinite(vector[solveRow]))
            {
                return false;
            }
            solveRow = solveRow - 1;
        }

        return true;
    }

    internal static void ApplySubElement(CalculatorFrame frame, IccProcessingElement subElement, CalculatorExecutionState state, int recursionDepth)
    {
        int start = frame.StackDepth - subElement.InputChannels;
        double[] inputs = new double[subElement.InputChannels];
        new System.ReadOnlySpan<double>(frame.Stack, start, inputs.Length).CopyTo(inputs);
        double[] outputs = new double[subElement.OutputChannels];
        if (subElement.Kind == IccProcessingElementKind.Calculator)
        {
            outputs = ExecuteCalculatorElement(subElement, inputs, state, recursionDepth);
        }
        else if (subElement.Kind == IccProcessingElementKind.CurveSet)
        {
            ApplyCurveSubElement(subElement, inputs, outputs);
        }
        else if (subElement.Kind == IccProcessingElementKind.Matrix)
        {
            ApplyMatrixSubElement(subElement, inputs, outputs);
        }
        else if (subElement.Kind == IccProcessingElementKind.Clut || subElement.Kind == IccProcessingElementKind.ExtendedClut || subElement.Kind == IccProcessingElementKind.TintArray)
        {
            ApplyClutSubElement(subElement, inputs, outputs);
        }
        else if (subElement.Kind == IccProcessingElementKind.XyzToJab || subElement.Kind == IccProcessingElementKind.JabToXyz)
        {
            IccCamMath.ExecuteElement(subElement, inputs, outputs);
        }
        else if ((subElement.Kind == IccProcessingElementKind.BeginAcs || subElement.Kind == IccProcessingElementKind.EndAcs) && subElement.InputChannels == subElement.OutputChannels)
        {
            new System.ReadOnlySpan<double>(inputs).CopyTo(outputs);
        }
        else
        {
            throw new IccTransformException($"Calculator sub-element kind {subElement.Kind} does not yet have executable semantics.");
        }

        frame.StackDepth = start;
        PushArrayRange(frame, outputs, 0, outputs.Length);
    }

    internal static void ApplyCurveSubElement(IccProcessingElement element, double[] inputs, double[] outputs)
    {
        if (element.Curves == null || element.Curves.Length != inputs.Length || outputs.Length != inputs.Length)
        {
            throw new IccTransformException("Calculator curve sub-element data are inconsistent.");
        }
        int index = 0;
        while (index < inputs.Length)
        {
            outputs[index] = CurveMath.EvaluateCurve(element.Curves[index], inputs[index]);
            index = index + 1;
        }
    }

    internal static void ApplyMatrixSubElement(IccProcessingElement element, double[] inputs, double[] outputs)
    {
        IccMatrix? matrix = element.Matrix;
        if (matrix == null || matrix.Values == null || matrix.Rows != outputs.Length || (matrix.Columns != inputs.Length && matrix.Columns != inputs.Length + 1)
            || matrix.Values.Length != (long)matrix.Rows * matrix.Columns)
        {
            throw new IccTransformException("Calculator matrix sub-element data are inconsistent.");
        }
        int row = 0;
        while (row < outputs.Length)
        {
            double sum = matrix.Columns == inputs.Length + 1 ? matrix.Values[(row * matrix.Columns) + inputs.Length] : 0.0;
            int column = 0;
            while (column < inputs.Length)
            {
                sum = sum + (matrix.Values[(row * matrix.Columns) + column] * inputs[column]);
                column = column + 1;
            }
            outputs[row] = sum;
            row = row + 1;
        }
    }

    internal static void ApplyClutSubElement(IccProcessingElement element, double[] inputs, double[] outputs)
    {
        IccClut? clut = element.Clut;
        int maximumInputs = element.Kind == IccProcessingElementKind.ExtendedClut ? 16 : 15;
        if (clut == null || clut.InputChannels != inputs.Length || clut.OutputChannels != outputs.Length || inputs.Length > maximumInputs)
        {
            throw new IccTransformException("Calculator CLUT sub-element data are inconsistent or exceed executable dimensions.");
        }
        IccInterpolationKind interpolation = element.Kind == IccProcessingElementKind.TintArray ? IccInterpolationKind.Linear : clut.Interpolation;
        int[] indexes = new int[ClutInterpolator.GetMultilinearIndexScratchLength(inputs.Length)];
        double[] weights = new double[ClutInterpolator.GetMultilinearWeightScratchLength(inputs.Length)];
        ClutInterpolator.InterpolateClut(clut, inputs, outputs, indexes, weights, interpolation);
    }
}
