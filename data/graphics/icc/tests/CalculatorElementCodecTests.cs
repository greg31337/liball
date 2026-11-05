using icc;

namespace icc.Tests;

public class CalculatorElementCodecTests
{
    [Test]
    public void RoundTripsInstructionsEnvironmentTemporaryStorageAndSubElements()
    {
        IccProcessingElement matrix = CreateMatrix();
        IccCalculatorProgram program = new IccCalculatorProgram
        {
            Instructions = new IccCalculatorInstruction[]
            {
                Instruction(CalculatorOperationSignatures.Input, 0),
                Instruction(CalculatorOperationSignatures.ApplyMatrix, 0),
                Instruction(CalculatorOperationSignatures.TemporarySave, 0x00030000),
                Instruction(CalculatorOperationSignatures.Environment, 0x74727565),
                Instruction(CalculatorOperationSignatures.Pop, 0x00010000),
                Instruction(CalculatorOperationSignatures.Output, 0)
            },
            SubElements = new IccProcessingElement[] { matrix }
        };
        IccProcessingElement calculator = CreateCalculator(1, 1, program);

        byte[] payload = SerializePipeline(calculator);
        TagDecodeState state = Decode(payload);
        IccProcessingElement decodedElement = state.Data.MultiProcessElements!.Elements[0];
        IccCalculatorProgram decoded = decodedElement.Calculator!;

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(decoded.IsExecutable, Is.True);
            Assert.That(state.Data.MultiProcessElements.IsExecutable, Is.True);
            Assert.That(decoded.InstructionCount, Is.EqualTo(6));
            Assert.That(decoded.RequiredStackDepth, Is.EqualTo(3));
            Assert.That(decoded.RequiredStorageValues, Is.EqualTo(4));
            Assert.That(decoded.Bytecode, Has.Length.EqualTo(48));
            Assert.That(decoded.Instructions[2].IsTemporaryStorageReference, Is.True);
            Assert.That(decoded.Instructions[2].FirstSelector, Is.EqualTo(3));
            Assert.That(decoded.Instructions[3].IsEnvironmentReference, Is.True);
            Assert.That(decoded.Instructions[3].Operand, Is.EqualTo(0x74727565));
            Assert.That(decoded.Instructions[1].IsSubElementReference, Is.True);
            Assert.That(decoded.SubElements[0].Kind, Is.EqualTo(IccProcessingElementKind.Matrix));
            Assert.That(SerializePipeline(decodedElement), Is.EqualTo(payload));
        });
    }

    [Test]
    public void ParsesConditionalAndSelectionOperationStreams()
    {
        IccCalculatorProgram program = new IccCalculatorProgram
        {
            Instructions = new IccCalculatorInstruction[]
            {
                Instruction(CalculatorOperationSignatures.Data, 0x3f800000),
                Instruction(CalculatorOperationSignatures.If, 2),
                Instruction(CalculatorOperationSignatures.Else, 1),
                Instruction(CalculatorOperationSignatures.Data, 0x40000000),
                Instruction(CalculatorOperationSignatures.Pop, 0),
                Instruction(CalculatorOperationSignatures.NoOperation, 0),
                Instruction(CalculatorOperationSignatures.Data, 0),
                Instruction(CalculatorOperationSignatures.Select, 0),
                Instruction(CalculatorOperationSignatures.Case, 1),
                Instruction(CalculatorOperationSignatures.Case, 1),
                Instruction(CalculatorOperationSignatures.Default, 1),
                Instruction(CalculatorOperationSignatures.NoOperation, 0),
                Instruction(CalculatorOperationSignatures.NoOperation, 0),
                Instruction(CalculatorOperationSignatures.NoOperation, 0)
            }
        };
        byte[] payload = SerializePipeline(CreateCalculator(1, 1, program));
        TagDecodeState state = Decode(payload);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.MultiProcessElements!.Elements[0].Calculator!.InstructionCount, Is.EqualTo(14));
            Assert.That(state.Data.MultiProcessElements.Elements[0].Calculator!.RequiredStackDepth, Is.EqualTo(1));
            Assert.That(state.Data.MultiProcessElements.Elements[0].Calculator!.IsExecutable, Is.True);
        });
    }

    [Test]
    public void PreservesUnknownOperationsButMarksCalculatorUnavailable()
    {
        IccCalculatorProgram program = new IccCalculatorProgram
        {
            Instructions = new IccCalculatorInstruction[]
            {
                Instruction(0x66757472, 0x12345678)
            }
        };
        byte[] payload = SerializePipeline(CreateCalculator(1, 1, program));
        TagDecodeState state = Decode(payload);
        IccCalculatorProgram decoded = state.Data.MultiProcessElements!.Elements[0].Calculator!;

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(decoded.UnknownOperationCount, Is.EqualTo(1));
            Assert.That(decoded.Instructions[0].IsKnown, Is.False);
            Assert.That(decoded.Instructions[0].Operand, Is.EqualTo(0x12345678));
            Assert.That(decoded.IsExecutable, Is.False);
            Assert.That(state.Data.MultiProcessElements.IsExecutable, Is.False);
            Assert.That(SerializePipeline(state.Data.MultiProcessElements.Elements[0]), Is.EqualTo(payload));
        });
    }

    [Test]
    public void WritesTheExistingRawBytecodeProgramRepresentation()
    {
        byte[] bytecode = new byte[16];
        WriteUInt32(bytecode, 0, CalculatorOperationSignatures.Input);
        WriteUInt32(bytecode, 4, 0);
        WriteUInt32(bytecode, 8, CalculatorOperationSignatures.Output);
        WriteUInt32(bytecode, 12, 0);
        IccCalculatorProgram program = new IccCalculatorProgram
        {
            Bytecode = bytecode,
            InstructionCount = 2
        };

        TagDecodeState state = Decode(SerializePipeline(CreateCalculator(1, 1, program)));

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.MultiProcessElements!.Elements[0].Calculator!.Instructions, Has.Length.EqualTo(2));
            Assert.That(state.Data.MultiProcessElements.Elements[0].Calculator!.Bytecode, Is.EqualTo(bytecode));
        });
    }

    [Test]
    public void RoundTripsNestedCalculatorSubElements()
    {
        IccCalculatorProgram innerProgram = new IccCalculatorProgram
        {
            Instructions = new IccCalculatorInstruction[]
            {
                Instruction(CalculatorOperationSignatures.Input, 0),
                Instruction(CalculatorOperationSignatures.Output, 0)
            }
        };
        IccProcessingElement inner = CreateCalculator(1, 1, innerProgram);
        IccCalculatorProgram outerProgram = new IccCalculatorProgram
        {
            Instructions = new IccCalculatorInstruction[]
            {
                Instruction(CalculatorOperationSignatures.Input, 0),
                Instruction(CalculatorOperationSignatures.ApplyCalculator, 0),
                Instruction(CalculatorOperationSignatures.Output, 0)
            },
            SubElements = new IccProcessingElement[] { inner }
        };

        byte[] payload = SerializePipeline(CreateCalculator(1, 1, outerProgram));
        TagDecodeState state = Decode(payload);
        IccProcessingElement decoded = state.Data.MultiProcessElements!.Elements[0];

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(decoded.Calculator!.IsExecutable, Is.True);
            Assert.That(decoded.Calculator.SubElements[0].Calculator!.IsExecutable, Is.True);
            Assert.That(decoded.Calculator.SubElements[0].Calculator!.Instructions, Has.Length.EqualTo(2));
            Assert.That(SerializePipeline(decoded), Is.EqualTo(payload));
        });
    }

    [Test]
    public void RecognizesEveryDefinedOperationSignatureAndOperandFamily()
    {
        uint[] signatures = new uint[]
        {
            CalculatorOperationSignatures.NoOperation, CalculatorOperationSignatures.StackCount,
            CalculatorOperationSignatures.Data, CalculatorOperationSignatures.Pi,
            CalculatorOperationSignatures.PositiveInfinity, CalculatorOperationSignatures.NegativeInfinity,
            CalculatorOperationSignatures.NotANumber, CalculatorOperationSignatures.Input,
            CalculatorOperationSignatures.Output, CalculatorOperationSignatures.TemporaryGet,
            CalculatorOperationSignatures.TemporaryPut, CalculatorOperationSignatures.TemporarySave,
            CalculatorOperationSignatures.Environment, CalculatorOperationSignatures.ApplyCurves,
            CalculatorOperationSignatures.ApplyMatrix, CalculatorOperationSignatures.ApplyClut,
            CalculatorOperationSignatures.ApplyTint, CalculatorOperationSignatures.ApplyToJab,
            CalculatorOperationSignatures.ApplyFromJab, CalculatorOperationSignatures.ApplyCalculator,
            CalculatorOperationSignatures.ApplyElement, CalculatorOperationSignatures.Copy,
            CalculatorOperationSignatures.RotateLeft, CalculatorOperationSignatures.RotateRight,
            CalculatorOperationSignatures.PositionDuplicate, CalculatorOperationSignatures.Flip,
            CalculatorOperationSignatures.Pop, CalculatorOperationSignatures.Sum,
            CalculatorOperationSignatures.Product, CalculatorOperationSignatures.Minimum,
            CalculatorOperationSignatures.Maximum, CalculatorOperationSignatures.And,
            CalculatorOperationSignatures.Or, CalculatorOperationSignatures.Negate,
            CalculatorOperationSignatures.Add, CalculatorOperationSignatures.Subtract,
            CalculatorOperationSignatures.Multiply, CalculatorOperationSignatures.Divide,
            CalculatorOperationSignatures.Modulus, CalculatorOperationSignatures.Power,
            CalculatorOperationSignatures.Gamma, CalculatorOperationSignatures.ScalarAdd,
            CalculatorOperationSignatures.ScalarSubtract, CalculatorOperationSignatures.ScalarMultiply,
            CalculatorOperationSignatures.ScalarDivide, CalculatorOperationSignatures.Square,
            CalculatorOperationSignatures.SquareRoot, CalculatorOperationSignatures.Cube,
            CalculatorOperationSignatures.CubeRoot, CalculatorOperationSignatures.Sign,
            CalculatorOperationSignatures.Absolute, CalculatorOperationSignatures.Floor,
            CalculatorOperationSignatures.Ceiling, CalculatorOperationSignatures.Truncate,
            CalculatorOperationSignatures.Round, CalculatorOperationSignatures.Exponential,
            CalculatorOperationSignatures.Logarithm, CalculatorOperationSignatures.NaturalLogarithm,
            CalculatorOperationSignatures.Sine, CalculatorOperationSignatures.Cosine,
            CalculatorOperationSignatures.Tangent, CalculatorOperationSignatures.ArcSine,
            CalculatorOperationSignatures.ArcCosine, CalculatorOperationSignatures.ArcTangent,
            CalculatorOperationSignatures.ArcTangent2, CalculatorOperationSignatures.CartesianToPolar,
            CalculatorOperationSignatures.PolarToCartesian, CalculatorOperationSignatures.RealNumber,
            CalculatorOperationSignatures.LessThan, CalculatorOperationSignatures.LessThanOrEqual,
            CalculatorOperationSignatures.Equal, CalculatorOperationSignatures.NotEqual,
            CalculatorOperationSignatures.Near, CalculatorOperationSignatures.GreaterThanOrEqual,
            CalculatorOperationSignatures.GreaterThan, CalculatorOperationSignatures.Not,
            CalculatorOperationSignatures.ToLab, CalculatorOperationSignatures.ToXyz,
            CalculatorOperationSignatures.VectorMinimum, CalculatorOperationSignatures.VectorMaximum,
            CalculatorOperationSignatures.VectorAnd, CalculatorOperationSignatures.VectorXor,
            CalculatorOperationSignatures.VectorOr, CalculatorOperationSignatures.Solve,
            CalculatorOperationSignatures.Transpose, CalculatorOperationSignatures.If,
            CalculatorOperationSignatures.Else, CalculatorOperationSignatures.Select,
            CalculatorOperationSignatures.Case, CalculatorOperationSignatures.Default
        };

        Assert.That(signatures.All(CalculatorElementCodec.IsKnownOperation), Is.True);
        Assert.That(CalculatorElementCodec.CreateInstruction(CalculatorOperationSignatures.Data, 0x3fc00000).FloatingPointValue, Is.EqualTo(1.5));
        Assert.That(CalculatorElementCodec.CreateInstruction(CalculatorOperationSignatures.Input, 0x00020003).FirstSelector, Is.EqualTo(2));
        Assert.That(CalculatorElementCodec.CreateInstruction(CalculatorOperationSignatures.Input, 0x00020003).SecondSelector, Is.EqualTo(3));
    }

    [Test]
    public void RejectsInvalidAddressingControlFlowAndConfiguredLimits()
    {
        IccCalculatorProgram validProgram = new IccCalculatorProgram
        {
            Instructions = new IccCalculatorInstruction[]
            {
                Instruction(CalculatorOperationSignatures.Input, 0),
                Instruction(CalculatorOperationSignatures.Output, 0)
            }
        };
        byte[] valid = SerializePipeline(CreateCalculator(1, 1, validProgram));
        int calculatorOffset = (int)ReadUInt32(valid, 16);
        int functionOffset = calculatorOffset + (int)ReadUInt32(valid, calculatorOffset + 16);

        byte[] badInput = valid.ToArray();
        WriteUInt32(badInput, functionOffset + 16, 0x00010000);
        byte[] badControl = valid.ToArray();
        WriteUInt32(badControl, functionOffset + 12, CalculatorOperationSignatures.Else);

        TagDecodeState limited = NewState(valid);
        limited.MaximumCalculatorInstructions = 1;
        TagCodecDispatcher.DecodeTagPayload(limited);

        Assert.Multiple(() =>
        {
            AssertInvalid(badInput, "outside the element input vector");
            AssertInvalid(badControl, "outside its required control header");
            Assert.That(limited.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(limited.Result.Message, Does.Contain("configured maximum of 1"));
        });
    }

    private static IccCalculatorInstruction Instruction(uint signature, uint operand)
    {
        return CalculatorElementCodec.CreateInstruction(signature, operand);
    }

    private static IccProcessingElement CreateCalculator(int inputs, int outputs, IccCalculatorProgram program)
    {
        return new IccProcessingElement
        {
            Signature = IccProcessingElementSignatures.Calculator,
            Kind = IccProcessingElementKind.Calculator,
            InputChannels = inputs,
            OutputChannels = outputs,
            Calculator = program
        };
    }

    private static IccProcessingElement CreateMatrix()
    {
        return new IccProcessingElement
        {
            Signature = IccProcessingElementSignatures.Matrix,
            Kind = IccProcessingElementKind.Matrix,
            InputChannels = 1,
            OutputChannels = 1,
            Matrix = new IccMatrix
            {
                Rows = 1,
                Columns = 2,
                Values = new double[] { 1.0, 0.0 }
            }
        };
    }

    private static byte[] SerializePipeline(IccProcessingElement calculator)
    {
        IccMultiProcessElements pipeline = new IccMultiProcessElements
        {
            InputChannels = calculator.InputChannels,
            OutputChannels = calculator.OutputChannels,
            Elements = new IccProcessingElement[] { calculator }
        };
        return MultiProcessElementsTagCodec.SerializeMultiProcessElementsVersion5(pipeline);
    }

    private static TagDecodeState Decode(byte[] payload)
    {
        TagDecodeState state = NewState(payload);
        TagCodecDispatcher.DecodeTagPayload(state);
        return state;
    }

    private static TagDecodeState NewState(byte[] payload)
    {
        return new TagDecodeState(new TagDecodeSource(payload), 0, payload.Length, 0, IccVersionKind.Version5, true);
    }

    private static void AssertInvalid(byte[] payload, string message)
    {
        TagDecodeState state = Decode(payload);
        Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
        Assert.That(state.Result.Message, Does.Contain(message));
    }

    private static uint ReadUInt32(byte[] bytes, int offset)
    {
        return ((uint)bytes[offset] << 24) | ((uint)bytes[offset + 1] << 16) | ((uint)bytes[offset + 2] << 8) | bytes[offset + 3];
    }

    private static void WriteUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }
}
