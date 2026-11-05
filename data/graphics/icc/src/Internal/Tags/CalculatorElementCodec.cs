namespace icc;

internal class CalculatorStackAnalysis
{
    internal int MinimumDepth;
    internal int MaximumDepth;
    internal int PeakDepth;
    internal bool StackIsKnown;
    internal string Error;

    internal CalculatorStackAnalysis()
    {
        MinimumDepth = 0;
        MaximumDepth = 0;
        PeakDepth = 0;
        StackIsKnown = true;
        Error = string.Empty;
    }
}

internal class CalculatorStackEffect
{
    internal int Consumed;
    internal int Produced;

    internal CalculatorStackEffect()
    {
        Consumed = 0;
        Produced = 0;
    }
}

internal class CalculatorWriteContext
{
    internal int MaximumDepth;
    internal System.Collections.Generic.Dictionary<IccProcessingElement, bool> ActiveElements;

    internal CalculatorWriteContext(int maximumDepth)
    {
        MaximumDepth = maximumDepth;
        ActiveElements = new System.Collections.Generic.Dictionary<IccProcessingElement, bool>();
    }
}

internal static class CalculatorOperationSignatures
{
    internal const uint NoOperation = 0x6e6f6f70;
    internal const uint StackCount = 0x73636e74;
    internal const uint Data = 0x64617461;
    internal const uint Pi = 0x70692020;
    internal const uint PositiveInfinity = 0x2b494e46;
    internal const uint NegativeInfinity = 0x2d494e46;
    internal const uint NotANumber = 0x4e614e20;
    internal const uint Input = 0x696e2020;
    internal const uint Output = 0x6f757420;
    internal const uint TemporaryGet = 0x74676574;
    internal const uint TemporaryPut = 0x74707574;
    internal const uint TemporarySave = 0x74736176;
    internal const uint Environment = 0x656e7620;
    internal const uint ApplyCurves = 0x63757276;
    internal const uint ApplyMatrix = 0x6d747820;
    internal const uint ApplyClut = 0x636c7574;
    internal const uint ApplyTint = 0x74696e74;
    internal const uint ApplyToJab = 0x744a6162;
    internal const uint ApplyFromJab = 0x664a6162;
    internal const uint ApplyCalculator = 0x63616c63;
    internal const uint ApplyElement = 0x656c656d;
    internal const uint Copy = 0x636f7079;
    internal const uint RotateLeft = 0x726f746c;
    internal const uint RotateRight = 0x726f7472;
    internal const uint PositionDuplicate = 0x706f7364;
    internal const uint Flip = 0x666c6970;
    internal const uint Pop = 0x706f7020;
    internal const uint Sum = 0x73756d20;
    internal const uint Product = 0x70726f64;
    internal const uint Minimum = 0x6d696e20;
    internal const uint Maximum = 0x6d617820;
    internal const uint And = 0x616e6420;
    internal const uint Or = 0x6f722020;
    internal const uint Negate = 0x6e656720;
    internal const uint Add = 0x61646420;
    internal const uint Subtract = 0x73756220;
    internal const uint Multiply = 0x6d756c20;
    internal const uint Divide = 0x64697620;
    internal const uint Modulus = 0x6d6f6420;
    internal const uint Power = 0x706f7720;
    internal const uint Gamma = 0x67616d61;
    internal const uint ScalarAdd = 0x73616464;
    internal const uint ScalarSubtract = 0x73737562;
    internal const uint ScalarMultiply = 0x736d756c;
    internal const uint ScalarDivide = 0x73646976;
    internal const uint Square = 0x73712020;
    internal const uint SquareRoot = 0x73717274;
    internal const uint Cube = 0x63622020;
    internal const uint CubeRoot = 0x63627274;
    internal const uint Sign = 0x7369676e;
    internal const uint Absolute = 0x61627320;
    internal const uint Floor = 0x666c6f72;
    internal const uint Ceiling = 0x6365696c;
    internal const uint Truncate = 0x74726e63;
    internal const uint Round = 0x726f6e64;
    internal const uint Exponential = 0x65787020;
    internal const uint Logarithm = 0x6c6f6720;
    internal const uint NaturalLogarithm = 0x6c6e2020;
    internal const uint Sine = 0x73696e20;
    internal const uint Cosine = 0x636f7320;
    internal const uint Tangent = 0x74616e20;
    internal const uint ArcSine = 0x6173696e;
    internal const uint ArcCosine = 0x61636f73;
    internal const uint ArcTangent = 0x6174616e;
    internal const uint ArcTangent2 = 0x61746e32;
    internal const uint CartesianToPolar = 0x63746f70;
    internal const uint PolarToCartesian = 0x70746f63;
    internal const uint RealNumber = 0x726e756d;
    internal const uint LessThan = 0x6c742020;
    internal const uint LessThanOrEqual = 0x6c652020;
    internal const uint Equal = 0x65712020;
    internal const uint NotEqual = 0x6e657120;
    internal const uint Near = 0x6e656172;
    internal const uint GreaterThanOrEqual = 0x67652020;
    internal const uint GreaterThan = 0x67742020;
    internal const uint Not = 0x6e6f7420;
    internal const uint ToLab = 0x744c6162;
    internal const uint ToXyz = 0x7458595a;
    internal const uint VectorMinimum = 0x766d696e;
    internal const uint VectorMaximum = 0x766d6178;
    internal const uint VectorAnd = 0x76616e64;
    internal const uint VectorXor = 0x76786f72;
    internal const uint VectorOr = 0x766f7220;
    internal const uint Solve = 0x736f6c76;
    internal const uint Transpose = 0x7472616e;
    internal const uint If = 0x69662020;
    internal const uint Else = 0x656c7365;
    internal const uint Select = 0x73656c20;
    internal const uint Case = 0x63617365;
    internal const uint Default = 0x64666c74;
}

internal static class CalculatorElementCodec
{
    internal const uint FunctionSignature = 0x66756e63;

    internal static string DecodeCalculator(System.ReadOnlyMemory<byte> encoded, IccProcessingElement element, long absoluteOffset, TagDecodeState state, int nestingDepth)
    {
        if (nestingDepth >= state.MaximumNestingDepth)
        {
            return $"decoding the calculator element would exceed the configured nesting depth of {state.MaximumNestingDepth}.";
        }

        if (encoded.Length < 36)
        {
            return "a calculator element requires a header, a main-function position, and a complete function.";
        }

        uint countValue = MultiProcessElementTagCodec.ReadUInt32At(encoded, 12);
        if (countValue > int.MaxValue || countValue > (uint)state.MaximumCalculatorInstructions || countValue > (uint)((encoded.Length - 24) / 8))
        {
            return $"the calculator sub-element count does not fit its position table or exceeds the configured maximum of {state.MaximumCalculatorInstructions}.";
        }

        int subElementCount = (int)countValue;
        int positionCount = subElementCount + 1;
        long dataStartValue = 16L + ((long)positionCount * 8L);
        if (dataStartValue > encoded.Length || dataStartValue > int.MaxValue)
        {
            return "the calculator position table extends beyond the element.";
        }

        int dataStart = (int)dataStartValue;
        MultiProcessElementPosition[] positions = new MultiProcessElementPosition[positionCount];
        int index = 0;
        while (index < positionCount)
        {
            int positionOffset = 16 + (index * 8);
            uint offsetValue = MultiProcessElementTagCodec.ReadUInt32At(encoded, positionOffset);
            uint sizeValue = MultiProcessElementTagCodec.ReadUInt32At(encoded, positionOffset + 4);
            if (offsetValue > int.MaxValue || sizeValue > int.MaxValue)
            {
                return $"calculator position {index} exceeds the supported managed range.";
            }

            MultiProcessElementPosition position = new MultiProcessElementPosition();
            position.Offset = (int)offsetValue;
            position.Size = (int)sizeValue;
            if (position.Offset < dataStart || position.Offset % 4 != 0 || position.Size < 12 || position.Size > encoded.Length - position.Offset || position.Size > int.MaxValue - 3)
            {
                return $"calculator position {index} is unaligned, incomplete, or outside the element.";
            }

            int paddedSize = MultiProcessElementsTagCodec.AlignFour(position.Size);
            if (paddedSize > encoded.Length - position.Offset || !MultiProcessElementsTagCodec.HasZeroBytes(encoded, position.Offset + position.Size, paddedSize - position.Size))
            {
                return $"calculator position {index} has missing or non-zero alignment padding.";
            }

            positions[index] = position;
            index = index + 1;
        }

        string topologyError = MultiProcessElementsTagCodec.ValidatePositionTopology(positions, dataStart, encoded.Length);
        if (topologyError.Length > 0)
        {
            return $"the calculator element storage is invalid: {topologyError}";
        }

        IccCalculatorProgram program = new IccCalculatorProgram();
        string functionError = DecodeFunction(encoded.Slice(positions[0].Offset, positions[0].Size), program, state);
        if (functionError.Length > 0)
        {
            return functionError;
        }

        IccProcessingElement[] subElements = new IccProcessingElement[subElementCount];
        System.Collections.Generic.List<MultiProcessElementPosition> decodedPositions = new System.Collections.Generic.List<MultiProcessElementPosition>();
        System.Collections.Generic.List<IccProcessingElement> decodedElements = new System.Collections.Generic.List<IccProcessingElement>();
        index = 0;
        while (index < subElementCount)
        {
            MultiProcessElementPosition position = positions[index + 1];
            IccProcessingElement? shared = MultiProcessElementsTagCodec.FindDecodedElement(decodedPositions, decodedElements, position);
            if (shared != null)
            {
                subElements[index] = shared;
            }
            else
            {
                MultiProcessElementDecodeResult result = MultiProcessElementsTagCodec.DecodeElementHeader(encoded, position, absoluteOffset, state, nestingDepth + 1);
                if (result.Error.Length > 0)
                {
                    return $"calculator sub-element {index}: {result.Error}";
                }

                subElements[index] = result.Element;
                decodedPositions.Add(position);
                decodedElements.Add(result.Element);
            }

            index = index + 1;
        }

        program.SubElements = subElements;
        string validationError = ValidateProgram(program, element, state.MaximumCalculatorStackDepth, state.MaximumCalculatorStorageValues, state.MaximumNestingDepth);
        if (validationError.Length > 0)
        {
            return validationError;
        }

        bool subElementsExecutable = true;
        index = 0;
        while (index < subElements.Length)
        {
            IccProcessingElement subElement = subElements[index];
            if (subElement.Kind == IccProcessingElementKind.Unknown || !Icc2ProcessingElementTagCodec.IsBodyCodecAvailable(subElement.Kind)
                || (subElement.Kind == IccProcessingElementKind.Calculator && (subElement.Calculator == null || !subElement.Calculator.IsExecutable)))
            {
                subElementsExecutable = false;
            }

            index = index + 1;
        }

        program.IsExecutable = program.UnknownOperationCount == 0 && subElementsExecutable;
        element.Calculator = program;
        return string.Empty;
    }

    internal static string DecodeFunction(System.ReadOnlyMemory<byte> encoded, IccCalculatorProgram program, TagDecodeState state)
    {
        if (encoded.Length < 12 || MultiProcessElementTagCodec.ReadUInt32At(encoded, 0) != FunctionSignature || !MultiProcessElementsTagCodec.HasZeroBytes(encoded, 4, 4))
        {
            return "the calculator main function has an invalid signature, reserved field, or size.";
        }

        uint countValue = MultiProcessElementTagCodec.ReadUInt32At(encoded, 8);
        if (countValue > int.MaxValue || countValue > (uint)state.MaximumCalculatorInstructions)
        {
            return $"the calculator instruction count exceeds the configured maximum of {state.MaximumCalculatorInstructions}.";
        }

        long expectedSize = 12L + ((long)countValue * 8L);
        if (expectedSize != encoded.Length)
        {
            return $"the calculator main function requires exactly {expectedSize} bytes for its declared instruction count.";
        }

        int count = (int)countValue;
        IccCalculatorInstruction[] instructions = new IccCalculatorInstruction[count];
        byte[] bytecode = new byte[count * 8];
        if (bytecode.Length > 0)
        {
            encoded.Slice(12).Span.CopyTo(bytecode);
        }

        int unknownCount = 0;
        int index = 0;
        while (index < count)
        {
            int offset = 12 + (index * 8);
            uint signature = MultiProcessElementTagCodec.ReadUInt32At(encoded, offset);
            uint operand = MultiProcessElementTagCodec.ReadUInt32At(encoded, offset + 4);
            IccCalculatorInstruction instruction = CreateInstruction(signature, operand);
            if (!instruction.IsKnown)
            {
                unknownCount = unknownCount + 1;
            }

            instructions[index] = instruction;
            index = index + 1;
        }

        program.Bytecode = bytecode;
        program.Instructions = instructions;
        program.InstructionCount = count;
        program.UnknownOperationCount = unknownCount;
        return string.Empty;
    }

    internal static IccCalculatorInstruction CreateInstruction(uint signature, uint operand)
    {
        IccCalculatorInstruction instruction = new IccCalculatorInstruction();
        instruction.Signature = signature;
        instruction.Operand = operand;
        instruction.FloatingPointValue = System.BitConverter.UInt32BitsToSingle(operand);
        instruction.FirstSelector = (int)(operand >> 16);
        instruction.SecondSelector = (int)(operand & 0xffff);
        instruction.IsKnown = IsKnownOperation(signature);
        instruction.IsEnvironmentReference = signature == CalculatorOperationSignatures.Environment;
        instruction.IsTemporaryStorageReference = signature == CalculatorOperationSignatures.TemporaryGet
            || signature == CalculatorOperationSignatures.TemporaryPut
            || signature == CalculatorOperationSignatures.TemporarySave;
        instruction.IsSubElementReference = IsSubElementOperation(signature);
        return instruction;
    }

    internal static string ValidateProgram(IccCalculatorProgram program, IccProcessingElement element, int maximumStackDepth, int maximumStorageValues, int maximumNestingDepth)
    {
        IccCalculatorInstruction[] instructions = program.Instructions;
        int requiredStorage = 0;
        int unknownCount = 0;
        int index = 0;
        while (index < instructions.Length)
        {
            IccCalculatorInstruction instruction = instructions[index];
            if (instruction == null)
            {
                return $"calculator instruction {index} is null.";
            }

            uint signature = instruction.Signature;
            uint operand = instruction.Operand;
            int first = (int)(operand >> 16);
            int second = (int)(operand & 0xffff);
            IccCalculatorInstruction decodedInstruction = CreateInstruction(signature, operand);
            instruction.FloatingPointValue = decodedInstruction.FloatingPointValue;
            instruction.FirstSelector = decodedInstruction.FirstSelector;
            instruction.SecondSelector = decodedInstruction.SecondSelector;
            instruction.IsKnown = decodedInstruction.IsKnown;
            instruction.IsEnvironmentReference = decodedInstruction.IsEnvironmentReference;
            instruction.IsTemporaryStorageReference = decodedInstruction.IsTemporaryStorageReference;
            instruction.IsSubElementReference = decodedInstruction.IsSubElementReference;
            if (!instruction.IsKnown)
            {
                unknownCount = unknownCount + 1;
            }
            if (RequiresZeroOperand(signature) && operand != 0)
            {
                return $"calculator operation '{FourCcCodec.Format(signature)}' requires a zero operand.";
            }

            if (RequiresZeroSecondSelector(signature) && second != 0)
            {
                return $"calculator operation '{FourCcCodec.Format(signature)}' requires its reserved selector to be zero.";
            }

            if (signature == CalculatorOperationSignatures.Input && !RangeFits(first, second, element.InputChannels))
            {
                return "a calculator input operation addresses channels outside the element input vector.";
            }

            if (signature == CalculatorOperationSignatures.Output && !RangeFits(first, second, element.OutputChannels))
            {
                return "a calculator output operation addresses channels outside the element output vector.";
            }

            if (instruction.IsTemporaryStorageReference)
            {
                long storage = (long)first + second + 1L;
                if (storage > maximumStorageValues)
                {
                    return $"a calculator temporary-storage operation requires {storage} values, exceeding the configured maximum of {maximumStorageValues}.";
                }

                if (storage > requiredStorage)
                {
                    requiredStorage = (int)storage;
                }
            }

            if (instruction.IsSubElementReference)
            {
                if (second != 0 || first >= program.SubElements.Length)
                {
                    return $"calculator operation '{FourCcCodec.Format(signature)}' has a non-zero reserved selector or references missing sub-element {first}.";
                }

                IccProcessingElement subElement = program.SubElements[first];
                if (subElement == null || !SubElementKindMatches(signature, subElement.Kind))
                {
                    return $"calculator operation '{FourCcCodec.Format(signature)}' references an incompatible sub-element kind.";
                }
            }

            index = index + 1;
        }

        program.RequiredStorageValues = requiredStorage;
        program.UnknownOperationCount = unknownCount;
        CalculatorStackAnalysis analysis = AnalyzeSequence(instructions, 0, instructions.Length, 0, 0, true, element, program, maximumStackDepth, maximumNestingDepth, 0);
        if (analysis.Error.Length > 0)
        {
            return analysis.Error;
        }

        program.RequiredStackDepth = analysis.PeakDepth;
        return string.Empty;
    }

    internal static CalculatorStackAnalysis AnalyzeSequence(IccCalculatorInstruction[] instructions, int start, int count, int minimumDepth, int maximumDepth, bool stackIsKnown, IccProcessingElement element, IccCalculatorProgram program, int maximumStackDepth, int maximumNestingDepth, int nestingDepth)
    {
        CalculatorStackAnalysis state = new CalculatorStackAnalysis();
        state.MinimumDepth = minimumDepth;
        state.MaximumDepth = maximumDepth;
        state.PeakDepth = maximumDepth;
        state.StackIsKnown = stackIsKnown;
        if (nestingDepth > maximumNestingDepth)
        {
            state.Error = $"calculator conditional nesting exceeds the configured maximum of {maximumNestingDepth}.";
            return state;
        }

        int end = start + count;
        int index = start;
        while (index < end)
        {
            IccCalculatorInstruction instruction = instructions[index];
            uint signature = instruction.Signature;
            if (signature == CalculatorOperationSignatures.Else || signature == CalculatorOperationSignatures.Case || signature == CalculatorOperationSignatures.Default)
            {
                state.Error = $"calculator control operation '{FourCcCodec.Format(signature)}' appears outside its required control header.";
                return state;
            }

            if (signature == CalculatorOperationSignatures.If)
            {
                if (!ConsumeStack(state, 1, 0, maximumStackDepth, index))
                {
                    return state;
                }

                bool hasElse = index + 1 < end && instructions[index + 1].Signature == CalculatorOperationSignatures.Else;
                int headerCount = hasElse ? 2 : 1;
                uint trueCountValue = instruction.Operand;
                uint falseCountValue = hasElse ? instructions[index + 1].Operand : 0;
                if (trueCountValue > int.MaxValue || falseCountValue > int.MaxValue)
                {
                    state.Error = "a calculator conditional stream count exceeds the supported managed range.";
                    return state;
                }

                int trueCount = (int)trueCountValue;
                int falseCount = (int)falseCountValue;
                int streamStart = index + headerCount;
                long streamEnd = (long)streamStart + trueCount + falseCount;
                if (streamEnd > end)
                {
                    state.Error = "a calculator conditional stream extends beyond its containing operation sequence.";
                    return state;
                }

                CalculatorStackAnalysis whenTrue = AnalyzeSequence(instructions, streamStart, trueCount, state.MinimumDepth, state.MaximumDepth, state.StackIsKnown, element, program, maximumStackDepth, maximumNestingDepth, nestingDepth + 1);
                if (whenTrue.Error.Length > 0)
                {
                    return whenTrue;
                }

                CalculatorStackAnalysis whenFalse;
                if (hasElse)
                {
                    whenFalse = AnalyzeSequence(instructions, streamStart + trueCount, falseCount, state.MinimumDepth, state.MaximumDepth, state.StackIsKnown, element, program, maximumStackDepth, maximumNestingDepth, nestingDepth + 1);
                    if (whenFalse.Error.Length > 0)
                    {
                        return whenFalse;
                    }
                }
                else
                {
                    whenFalse = new CalculatorStackAnalysis();
                    whenFalse.MinimumDepth = state.MinimumDepth;
                    whenFalse.MaximumDepth = state.MaximumDepth;
                    whenFalse.PeakDepth = state.PeakDepth;
                    whenFalse.StackIsKnown = state.StackIsKnown;
                }

                if (hasElse && whenTrue.StackIsKnown && whenFalse.StackIsKnown
                    && (whenTrue.MinimumDepth != whenFalse.MinimumDepth || whenTrue.MaximumDepth != whenFalse.MaximumDepth))
                {
                    state.Error = "calculator if and else streams shall have identical stack effects.";
                    return state;
                }

                state.MinimumDepth = System.Math.Min(whenTrue.MinimumDepth, whenFalse.MinimumDepth);
                state.MaximumDepth = System.Math.Max(whenTrue.MaximumDepth, whenFalse.MaximumDepth);
                state.PeakDepth = System.Math.Max(state.PeakDepth, System.Math.Max(whenTrue.PeakDepth, whenFalse.PeakDepth));
                state.StackIsKnown = whenTrue.StackIsKnown && whenFalse.StackIsKnown;
                index = (int)streamEnd;
                continue;
            }

            if (signature == CalculatorOperationSignatures.Select)
            {
                if (instruction.Operand != 0)
                {
                    state.Error = "a calculator select operation requires a zero reserved operand.";
                    return state;
                }

                if (!ConsumeStack(state, 1, 0, maximumStackDepth, index))
                {
                    return state;
                }

                int markerStart = index + 1;
                int markerEnd = markerStart;
                bool hasDefault = false;
                while (markerEnd < end && instructions[markerEnd].Signature == CalculatorOperationSignatures.Case)
                {
                    markerEnd = markerEnd + 1;
                }

                if (markerEnd == markerStart)
                {
                    state.Error = "a calculator select operation shall be followed by at least one case operation.";
                    return state;
                }

                if (markerEnd < end && instructions[markerEnd].Signature == CalculatorOperationSignatures.Default)
                {
                    hasDefault = true;
                    markerEnd = markerEnd + 1;
                }

                int streamPosition = markerEnd;
                int branchMinimum = state.MinimumDepth;
                int branchMaximum = state.MaximumDepth;
                int branchPeak = state.PeakDepth;
                bool branchKnown = state.StackIsKnown;
                int expectedBranchMinimum = -1;
                int expectedBranchMaximum = -1;
                int marker = markerStart;
                while (marker < markerEnd)
                {
                    uint branchCountValue = instructions[marker].Operand;
                    if (branchCountValue > int.MaxValue || (long)streamPosition + branchCountValue > end)
                    {
                        state.Error = "a calculator selection stream extends beyond its containing operation sequence.";
                        return state;
                    }

                    int branchCount = (int)branchCountValue;
                    CalculatorStackAnalysis branch = AnalyzeSequence(instructions, streamPosition, branchCount, state.MinimumDepth, state.MaximumDepth, state.StackIsKnown, element, program, maximumStackDepth, maximumNestingDepth, nestingDepth + 1);
                    if (branch.Error.Length > 0)
                    {
                        return branch;
                    }
                    if (branch.StackIsKnown && expectedBranchMinimum >= 0
                        && (branch.MinimumDepth != expectedBranchMinimum || branch.MaximumDepth != expectedBranchMaximum))
                    {
                        state.Error = "calculator case and default streams shall have identical stack effects.";
                        return state;
                    }
                    if (branch.StackIsKnown && expectedBranchMinimum < 0)
                    {
                        expectedBranchMinimum = branch.MinimumDepth;
                        expectedBranchMaximum = branch.MaximumDepth;
                    }

                    if (marker == markerStart && hasDefault)
                    {
                        branchMinimum = branch.MinimumDepth;
                        branchMaximum = branch.MaximumDepth;
                        branchKnown = branch.StackIsKnown;
                    }
                    else
                    {
                        branchMinimum = System.Math.Min(branchMinimum, branch.MinimumDepth);
                        branchMaximum = System.Math.Max(branchMaximum, branch.MaximumDepth);
                        branchKnown = branchKnown && branch.StackIsKnown;
                    }

                    branchPeak = System.Math.Max(branchPeak, branch.PeakDepth);
                    streamPosition = streamPosition + branchCount;
                    marker = marker + 1;
                }

                state.MinimumDepth = branchMinimum;
                state.MaximumDepth = branchMaximum;
                state.PeakDepth = branchPeak;
                state.StackIsKnown = branchKnown;
                index = streamPosition;
                continue;
            }

            if (!instruction.IsKnown)
            {
                state.StackIsKnown = false;
                index = index + 1;
                continue;
            }

            CalculatorStackEffect effect = GetStackEffect(instruction, program);
            if (state.StackIsKnown && !ConsumeStack(state, effect.Consumed, effect.Produced, maximumStackDepth, index))
            {
                return state;
            }

            index = index + 1;
        }

        return state;
    }

    internal static bool ConsumeStack(CalculatorStackAnalysis state, int consumed, int produced, int maximumStackDepth, int instructionIndex)
    {
        if (!state.StackIsKnown)
        {
            return true;
        }

        if (consumed > state.MinimumDepth)
        {
            state.Error = $"calculator instruction {instructionIndex} can underflow the operand stack.";
            return false;
        }

        long newMinimum = (long)state.MinimumDepth - consumed + produced;
        long newMaximum = (long)state.MaximumDepth - consumed + produced;
        if (newMaximum > maximumStackDepth || newMaximum > int.MaxValue)
        {
            state.Error = $"calculator instruction {instructionIndex} requires a stack depth of {newMaximum}, exceeding the configured maximum of {maximumStackDepth}.";
            return false;
        }

        state.MinimumDepth = (int)newMinimum;
        state.MaximumDepth = (int)newMaximum;
        if (state.MaximumDepth > state.PeakDepth)
        {
            state.PeakDepth = state.MaximumDepth;
        }

        return true;
    }

    internal static CalculatorStackEffect GetStackEffect(IccCalculatorInstruction instruction, IccCalculatorProgram program)
    {
        CalculatorStackEffect effect = new CalculatorStackEffect();
        uint signature = instruction.Signature;
        int first = instruction.FirstSelector;
        int second = instruction.SecondSelector;
        int vector = first + 1;
        if (signature == CalculatorOperationSignatures.Data || signature == CalculatorOperationSignatures.StackCount || signature == CalculatorOperationSignatures.Pi || signature == CalculatorOperationSignatures.PositiveInfinity || signature == CalculatorOperationSignatures.NegativeInfinity || signature == CalculatorOperationSignatures.NotANumber)
        {
            effect.Produced = 1;
        }
        else if (signature == CalculatorOperationSignatures.Environment)
        {
            effect.Produced = 2;
        }
        else if (signature == CalculatorOperationSignatures.Input || signature == CalculatorOperationSignatures.TemporaryGet)
        {
            effect.Produced = second + 1;
        }
        else if (signature == CalculatorOperationSignatures.Output || signature == CalculatorOperationSignatures.TemporaryPut)
        {
            effect.Consumed = second + 1;
        }
        else if (signature == CalculatorOperationSignatures.TemporarySave)
        {
            effect.Consumed = second + 1;
            effect.Produced = second + 1;
        }
        else if (IsSubElementOperation(signature))
        {
            IccProcessingElement subElement = program.SubElements[instruction.FirstSelector];
            effect.Consumed = subElement.InputChannels;
            effect.Produced = subElement.OutputChannels;
        }
        else if (signature == CalculatorOperationSignatures.Copy)
        {
            effect.Consumed = vector;
            effect.Produced = LimitToInt((long)vector * (second + 2));
        }
        else if (signature == CalculatorOperationSignatures.RotateLeft || signature == CalculatorOperationSignatures.RotateRight)
        {
            effect.Consumed = vector;
            effect.Produced = vector;
        }
        else if (signature == CalculatorOperationSignatures.PositionDuplicate)
        {
            effect.Consumed = vector;
            effect.Produced = vector + second + 1;
        }
        else if (signature == CalculatorOperationSignatures.Flip)
        {
            effect.Consumed = first + 2;
            effect.Produced = first + 2;
        }
        else if (signature == CalculatorOperationSignatures.Pop)
        {
            effect.Consumed = vector;
        }
        else if (IsSequenceOperation(signature))
        {
            effect.Consumed = first + 2;
            effect.Produced = 1;
        }
        else if (signature == CalculatorOperationSignatures.Solve)
        {
            effect.Consumed = LimitToInt(((long)first + 1L) * (second + 1L) + first + 1L);
            effect.Produced = second + 2;
        }
        else if (signature == CalculatorOperationSignatures.Transpose)
        {
            effect.Consumed = LimitToInt(((long)first + 1L) * (second + 1L));
            effect.Produced = effect.Consumed;
        }
        else if (IsScalarVectorOperation(signature))
        {
            effect.Consumed = first + 2;
            effect.Produced = vector;
        }
        else if (IsBinaryVectorOperation(signature))
        {
            effect.Consumed = vector * 2;
            effect.Produced = vector;
        }
        else if (signature == CalculatorOperationSignatures.CartesianToPolar || signature == CalculatorOperationSignatures.PolarToCartesian)
        {
            effect.Consumed = vector * 2;
            effect.Produced = vector * 2;
        }
        else if (signature == CalculatorOperationSignatures.ToLab || signature == CalculatorOperationSignatures.ToXyz)
        {
            effect.Consumed = vector * 3;
            effect.Produced = vector * 3;
        }
        else if (IsUnaryVectorOperation(signature))
        {
            effect.Consumed = vector;
            effect.Produced = vector;
        }

        return effect;
    }

    internal static byte[] SerializeCalculator(IccProcessingElement element, int elementIndex)
    {
        CalculatorWriteContext context = new CalculatorWriteContext(32);
        return SerializeCalculatorWithContext(element, elementIndex, context, 0);
    }

    internal static byte[] SerializeCalculatorWithContext(IccProcessingElement element, int elementIndex, CalculatorWriteContext context, int nestingDepth)
    {
        IccCalculatorProgram? program = element.Calculator;
        if (element.Signature != IccProcessingElementSignatures.Calculator || element.Kind != IccProcessingElementKind.Calculator || element.InputChannels < 1 || element.InputChannels > ushort.MaxValue || element.OutputChannels < 1 || element.OutputChannels > ushort.MaxValue || program == null || program.Instructions == null || program.SubElements == null)
        {
            throw new IccProfileWriteException($"Processing element {elementIndex} calculator data, signature, kind, or channels are invalid.");
        }

        if (nestingDepth >= context.MaximumDepth || context.ActiveElements.ContainsKey(element))
        {
            throw new IccProfileWriteException($"Processing element {elementIndex} calculator sub-elements are circular or exceed the supported nesting depth.");
        }

        PopulateInstructionsFromBytecodeForWrite(program, elementIndex);
        if (program.Instructions.Length > 100000)
        {
            throw new IccProfileWriteException($"Processing element {elementIndex} exceeds the supported calculator instruction count.");
        }

        string validationError = ValidateProgram(program, element, 65535, 65536, context.MaximumDepth);
        if (validationError.Length > 0)
        {
            throw new IccProfileWriteException($"Processing element {elementIndex}: {validationError}");
        }

        context.ActiveElements.Add(element, true);
        byte[] function = SerializeFunction(program);
        int positionCount = program.SubElements.Length + 1;
        long dataStartValue = 16L + ((long)positionCount * 8L);
        if (dataStartValue > int.MaxValue)
        {
            throw new IccProfileWriteException($"Processing element {elementIndex} calculator position table exceeds the supported managed size.");
        }

        MultiProcessElementPosition[] positions = new MultiProcessElementPosition[positionCount];
        System.Collections.Generic.List<MultiProcessStoredBlock> blocks = new System.Collections.Generic.List<MultiProcessStoredBlock>();
        int nextOffset = (int)dataStartValue;
        MultiProcessStoredBlock functionBlock = new MultiProcessStoredBlock();
        functionBlock.Offset = nextOffset;
        functionBlock.Bytes = function;
        blocks.Add(functionBlock);
        positions[0] = new MultiProcessElementPosition();
        positions[0].Offset = nextOffset;
        positions[0].Size = function.Length;
        nextOffset = nextOffset + MultiProcessElementsTagCodec.AlignFour(function.Length);
        int index = 0;
        while (index < program.SubElements.Length)
        {
            IccProcessingElement subElement = program.SubElements[index];
            if (subElement == null)
            {
                throw new IccProfileWriteException($"Processing element {elementIndex} calculator sub-element {index} is null.");
            }

            byte[] bytes;
            if (subElement.Kind == IccProcessingElementKind.Calculator)
            {
                bytes = SerializeCalculatorWithContext(subElement, index, context, nestingDepth + 1);
            }
            else if (Icc2ProcessingElementTagCodec.IsBodyCodecAvailable(subElement.Kind))
            {
                bytes = Icc2ProcessingElementTagCodec.SerializeKnownElement(subElement, index);
            }
            else
            {
                MultiProcessElementsTagCodec.ValidateElementForWriteVersion5(subElement, index);
                bytes = subElement.RawData.Bytes;
            }

            int blockOffset = MultiProcessElementsTagCodec.FindWriteBlockOffset(blocks, bytes);
            if (blockOffset < 0)
            {
                if (bytes.Length > int.MaxValue - 3 || MultiProcessElementsTagCodec.AlignFour(bytes.Length) > int.MaxValue - nextOffset)
                {
                    throw new IccProfileWriteException($"Processing element {elementIndex} calculator data exceeds the supported managed size.");
                }

                MultiProcessStoredBlock block = new MultiProcessStoredBlock();
                block.Offset = nextOffset;
                block.Bytes = bytes;
                blocks.Add(block);
                blockOffset = nextOffset;
                nextOffset = nextOffset + MultiProcessElementsTagCodec.AlignFour(bytes.Length);
            }

            positions[index + 1] = new MultiProcessElementPosition();
            positions[index + 1].Offset = blockOffset;
            positions[index + 1].Size = bytes.Length;
            index = index + 1;
        }

        byte[] encoded = new byte[nextOffset];
        BigEndianWriter writer = new BigEndianWriter(encoded, 0);
        MultiProcessElementTagCodec.WriteElementHeader(writer, element.Signature, element.InputChannels, element.OutputChannels);
        writer.WriteUInt32((uint)program.SubElements.Length);
        index = 0;
        while (index < positions.Length)
        {
            writer.WriteUInt32((uint)positions[index].Offset);
            writer.WriteUInt32((uint)positions[index].Size);
            index = index + 1;
        }

        index = 0;
        while (index < blocks.Count)
        {
            MultiProcessStoredBlock block = blocks[index];
            new System.ReadOnlySpan<byte>(block.Bytes).CopyTo(new System.Span<byte>(encoded, block.Offset, block.Bytes.Length));
            index = index + 1;
        }

        context.ActiveElements.Remove(element);
        return encoded;
    }

    internal static byte[] SerializeFunction(IccCalculatorProgram program)
    {
        long size = 12L + ((long)program.Instructions.Length * 8L);
        if (size > int.MaxValue)
        {
            throw new IccProfileWriteException("The calculator main function exceeds the supported managed size.");
        }

        byte[] encoded = new byte[(int)size];
        BigEndianWriter writer = new BigEndianWriter(encoded, 0);
        writer.WriteUInt32(FunctionSignature);
        writer.WriteZeroBytes(4);
        writer.WriteUInt32((uint)program.Instructions.Length);
        int index = 0;
        while (index < program.Instructions.Length)
        {
            IccCalculatorInstruction instruction = program.Instructions[index];
            writer.WriteUInt32(instruction.Signature);
            writer.WriteUInt32(instruction.Operand);
            index = index + 1;
        }

        program.InstructionCount = program.Instructions.Length;
        program.Bytecode = new byte[program.Instructions.Length * 8];
        if (program.Bytecode.Length > 0)
        {
            new System.ReadOnlySpan<byte>(encoded, 12, program.Bytecode.Length).CopyTo(program.Bytecode);
        }

        return encoded;
    }

    internal static void PopulateInstructionsFromBytecodeForWrite(IccCalculatorProgram program, int elementIndex)
    {
        if (program.Instructions.Length > 0)
        {
            return;
        }

        if (program.Bytecode == null || program.Bytecode.Length == 0)
        {
            if (program.InstructionCount != 0)
            {
                throw new IccProfileWriteException($"Processing element {elementIndex} calculator instruction count has no corresponding instructions or bytecode.");
            }

            return;
        }

        if (program.Bytecode.Length % 8 != 0 || (program.InstructionCount != 0 && program.InstructionCount != program.Bytecode.Length / 8))
        {
            throw new IccProfileWriteException($"Processing element {elementIndex} calculator bytecode shall contain exactly eight bytes per declared instruction.");
        }

        int count = program.Bytecode.Length / 8;
        if (count > 100000)
        {
            throw new IccProfileWriteException($"Processing element {elementIndex} exceeds the supported calculator instruction count.");
        }

        IccCalculatorInstruction[] instructions = new IccCalculatorInstruction[count];
        System.ReadOnlyMemory<byte> encoded = new System.ReadOnlyMemory<byte>(program.Bytecode);
        int index = 0;
        while (index < count)
        {
            int offset = index * 8;
            uint signature = MultiProcessElementTagCodec.ReadUInt32At(encoded, offset);
            uint operand = MultiProcessElementTagCodec.ReadUInt32At(encoded, offset + 4);
            instructions[index] = CreateInstruction(signature, operand);
            index = index + 1;
        }

        program.Instructions = instructions;
        program.InstructionCount = count;
    }

    internal static bool RangeFits(int start, int additionalCount, int available)
    {
        return start < available && additionalCount < available - start;
    }

    internal static int LimitToInt(long value)
    {
        return value > int.MaxValue ? int.MaxValue : (int)value;
    }

    internal static bool SubElementKindMatches(uint signature, IccProcessingElementKind kind)
    {
        if (signature == CalculatorOperationSignatures.ApplyElement)
        {
            return true;
        }

        if (signature == CalculatorOperationSignatures.ApplyCurves)
        {
            return kind == IccProcessingElementKind.CurveSet;
        }

        if (signature == CalculatorOperationSignatures.ApplyMatrix)
        {
            return kind == IccProcessingElementKind.Matrix;
        }

        if (signature == CalculatorOperationSignatures.ApplyClut)
        {
            return kind == IccProcessingElementKind.Clut || kind == IccProcessingElementKind.ExtendedClut;
        }

        if (signature == CalculatorOperationSignatures.ApplyTint)
        {
            return kind == IccProcessingElementKind.TintArray;
        }

        if (signature == CalculatorOperationSignatures.ApplyToJab)
        {
            return kind == IccProcessingElementKind.XyzToJab;
        }

        if (signature == CalculatorOperationSignatures.ApplyFromJab)
        {
            return kind == IccProcessingElementKind.JabToXyz;
        }

        return signature == CalculatorOperationSignatures.ApplyCalculator && kind == IccProcessingElementKind.Calculator;
    }

    internal static bool RequiresZeroOperand(uint signature)
    {
        return signature == CalculatorOperationSignatures.NoOperation
            || signature == CalculatorOperationSignatures.StackCount
            || signature == CalculatorOperationSignatures.Pi
            || signature == CalculatorOperationSignatures.PositiveInfinity
            || signature == CalculatorOperationSignatures.NegativeInfinity
            || signature == CalculatorOperationSignatures.NotANumber
            || signature == CalculatorOperationSignatures.Select;
    }

    internal static bool RequiresZeroSecondSelector(uint signature)
    {
        return IsSequenceOperation(signature) || IsFunctionalVectorOperation(signature)
            || signature == CalculatorOperationSignatures.Flip
            || signature == CalculatorOperationSignatures.Pop;
    }

    internal static bool IsSubElementOperation(uint signature)
    {
        return signature == CalculatorOperationSignatures.ApplyCurves
            || signature == CalculatorOperationSignatures.ApplyMatrix
            || signature == CalculatorOperationSignatures.ApplyClut
            || signature == CalculatorOperationSignatures.ApplyTint
            || signature == CalculatorOperationSignatures.ApplyToJab
            || signature == CalculatorOperationSignatures.ApplyFromJab
            || signature == CalculatorOperationSignatures.ApplyCalculator
            || signature == CalculatorOperationSignatures.ApplyElement;
    }

    internal static bool IsSequenceOperation(uint signature)
    {
        return signature == CalculatorOperationSignatures.Sum
            || signature == CalculatorOperationSignatures.Product
            || signature == CalculatorOperationSignatures.Minimum
            || signature == CalculatorOperationSignatures.Maximum
            || signature == CalculatorOperationSignatures.And
            || signature == CalculatorOperationSignatures.Or;
    }

    internal static bool IsScalarVectorOperation(uint signature)
    {
        return signature == CalculatorOperationSignatures.Gamma
            || signature == CalculatorOperationSignatures.ScalarAdd
            || signature == CalculatorOperationSignatures.ScalarSubtract
            || signature == CalculatorOperationSignatures.ScalarMultiply
            || signature == CalculatorOperationSignatures.ScalarDivide;
    }

    internal static bool IsBinaryVectorOperation(uint signature)
    {
        return signature == CalculatorOperationSignatures.Add
            || signature == CalculatorOperationSignatures.Subtract
            || signature == CalculatorOperationSignatures.Multiply
            || signature == CalculatorOperationSignatures.Divide
            || signature == CalculatorOperationSignatures.Modulus
            || signature == CalculatorOperationSignatures.Power
            || signature == CalculatorOperationSignatures.ArcTangent2
            || signature == CalculatorOperationSignatures.LessThan
            || signature == CalculatorOperationSignatures.LessThanOrEqual
            || signature == CalculatorOperationSignatures.Equal
            || signature == CalculatorOperationSignatures.NotEqual
            || signature == CalculatorOperationSignatures.Near
            || signature == CalculatorOperationSignatures.GreaterThanOrEqual
            || signature == CalculatorOperationSignatures.GreaterThan
            || signature == CalculatorOperationSignatures.VectorMinimum
            || signature == CalculatorOperationSignatures.VectorMaximum
            || signature == CalculatorOperationSignatures.VectorAnd
            || signature == CalculatorOperationSignatures.VectorXor
            || signature == CalculatorOperationSignatures.VectorOr;
    }

    internal static bool IsUnaryVectorOperation(uint signature)
    {
        return signature == CalculatorOperationSignatures.Negate
            || signature == CalculatorOperationSignatures.Square
            || signature == CalculatorOperationSignatures.SquareRoot
            || signature == CalculatorOperationSignatures.Cube
            || signature == CalculatorOperationSignatures.CubeRoot
            || signature == CalculatorOperationSignatures.Sign
            || signature == CalculatorOperationSignatures.Absolute
            || signature == CalculatorOperationSignatures.Floor
            || signature == CalculatorOperationSignatures.Ceiling
            || signature == CalculatorOperationSignatures.Truncate
            || signature == CalculatorOperationSignatures.Round
            || signature == CalculatorOperationSignatures.Exponential
            || signature == CalculatorOperationSignatures.Logarithm
            || signature == CalculatorOperationSignatures.NaturalLogarithm
            || signature == CalculatorOperationSignatures.Sine
            || signature == CalculatorOperationSignatures.Cosine
            || signature == CalculatorOperationSignatures.Tangent
            || signature == CalculatorOperationSignatures.ArcSine
            || signature == CalculatorOperationSignatures.ArcCosine
            || signature == CalculatorOperationSignatures.ArcTangent
            || signature == CalculatorOperationSignatures.RealNumber
            || signature == CalculatorOperationSignatures.Not;
    }

    internal static bool IsFunctionalVectorOperation(uint signature)
    {
        return IsScalarVectorOperation(signature)
            || IsBinaryVectorOperation(signature)
            || IsUnaryVectorOperation(signature)
            || signature == CalculatorOperationSignatures.CartesianToPolar
            || signature == CalculatorOperationSignatures.PolarToCartesian
            || signature == CalculatorOperationSignatures.ToLab
            || signature == CalculatorOperationSignatures.ToXyz;
    }

    internal static bool IsKnownOperation(uint signature)
    {
        return signature == CalculatorOperationSignatures.NoOperation
            || signature == CalculatorOperationSignatures.StackCount
            || signature == CalculatorOperationSignatures.Data
            || signature == CalculatorOperationSignatures.Pi
            || signature == CalculatorOperationSignatures.PositiveInfinity
            || signature == CalculatorOperationSignatures.NegativeInfinity
            || signature == CalculatorOperationSignatures.NotANumber
            || signature == CalculatorOperationSignatures.Input
            || signature == CalculatorOperationSignatures.Output
            || signature == CalculatorOperationSignatures.TemporaryGet
            || signature == CalculatorOperationSignatures.TemporaryPut
            || signature == CalculatorOperationSignatures.TemporarySave
            || signature == CalculatorOperationSignatures.Environment
            || IsSubElementOperation(signature)
            || signature == CalculatorOperationSignatures.Copy
            || signature == CalculatorOperationSignatures.RotateLeft
            || signature == CalculatorOperationSignatures.RotateRight
            || signature == CalculatorOperationSignatures.PositionDuplicate
            || signature == CalculatorOperationSignatures.Flip
            || signature == CalculatorOperationSignatures.Pop
            || IsSequenceOperation(signature)
            || IsFunctionalVectorOperation(signature)
            || signature == CalculatorOperationSignatures.Solve
            || signature == CalculatorOperationSignatures.Transpose
            || signature == CalculatorOperationSignatures.If
            || signature == CalculatorOperationSignatures.Else
            || signature == CalculatorOperationSignatures.Select
            || signature == CalculatorOperationSignatures.Case
            || signature == CalculatorOperationSignatures.Default;
    }
}
