using icc;

namespace icc.Tests;

public class CalculatorCompleteCoverageTests
{
    [Test]
    public void ExecutesTemporaryPutAndEveryEnvironmentResultForm()
    {
        const uint missing = 0x6d697373;
        const uint custom = 0x63757374;
        IccCalculatorInstruction[] instructions =
        {
            Instruction(CalculatorOperationSignatures.Input, Selectors(0, 2)),
            Instruction(CalculatorOperationSignatures.TemporaryPut, Selectors(2, 2)),
            Instruction(CalculatorOperationSignatures.TemporaryGet, Selectors(2, 2)),
            Instruction(CalculatorOperationSignatures.Environment, 0x74727565),
            Instruction(CalculatorOperationSignatures.Environment, 0x6e646566),
            Instruction(CalculatorOperationSignatures.Environment, missing),
            Instruction(CalculatorOperationSignatures.Environment, custom),
            Instruction(CalculatorOperationSignatures.Output, Selectors(0, 10))
        };

        double[] result = RunCalculator(
            instructions,
            new double[] { 2.0, 4.0, 8.0 },
            11,
            Array.Empty<IccProcessingElement>(),
            new uint[] { custom },
            new double[] { double.PositiveInfinity },
            new IccResourceLimits());

        Assert.Multiple(() =>
        {
            Assert.That(result[..9], Is.EqualTo(new double[] { 2.0, 4.0, 8.0, 1.0, 1.0, 0.0, 0.0, 0.0, 0.0 }));
            Assert.That(result[9], Is.EqualTo(double.PositiveInfinity));
            Assert.That(result[10], Is.EqualTo(1.0));
        });
    }

    [Test]
    public void RoutesEveryNonFiniteSelectionToDefaultAndClassifiesItAsNotReal()
    {
        double[] values = { double.NaN, double.PositiveInfinity, double.NegativeInfinity };

        foreach (double value in values)
        {
            Assert.Multiple(() =>
            {
                Assert.That(RunSelection(value), Is.EqualTo(30.0));
                Assert.That(RunUnary(CalculatorOperationSignatures.RealNumber, value), Is.EqualTo(0.0));
            });
        }

        Assert.Multiple(() =>
        {
            Assert.That(RunBinary(CalculatorOperationSignatures.Equal, double.NaN, double.NaN), Is.EqualTo(0.0));
            Assert.That(RunBinary(CalculatorOperationSignatures.NotEqual, double.NaN, double.NaN), Is.EqualTo(1.0));
            Assert.That(RunUnary(CalculatorOperationSignatures.SquareRoot, -1.0), Is.NaN);
        });
    }

    [Test]
    public void SolvesUnderdeterminedSystemsAndReportsSingularSystems()
    {
        IccCalculatorInstruction[] solvable = DataProgram(
            new double[] { 3.0, 4.0, 10.0 },
            CalculatorOperationSignatures.Solve,
            Selectors(0, 1),
            3);
        IccCalculatorInstruction[] singular = DataProgram(
            new double[] { 0.0, 0.0, 10.0 },
            CalculatorOperationSignatures.Solve,
            Selectors(0, 1),
            3);

        Assert.Multiple(() =>
        {
            Assert.That(RunCalculator(solvable, new double[] { 0.0 }, 3, Array.Empty<IccProcessingElement>(), Array.Empty<uint>(), Array.Empty<double>(), new IccResourceLimits()), Is.EqualTo(new double[] { 1.2, 1.6, 1.0 }).Within(1e-12));
            Assert.That(RunCalculator(singular, new double[] { 0.0 }, 3, Array.Empty<IccProcessingElement>(), Array.Empty<uint>(), Array.Empty<double>(), new IccResourceLimits()), Is.EqualTo(new double[] { 0.0, 0.0, 0.0 }));
        });
    }

    [Test]
    public void ExecutesCurveClutTintAndGenericAcsSubElements()
    {
        IccProcessingElement curves = new IccProcessingElement
        {
            Signature = IccProcessingElementSignatures.CurveSet,
            Kind = IccProcessingElementKind.CurveSet,
            InputChannels = 2,
            OutputChannels = 2,
            Curves = new IccCurve[]
            {
                new IccCurve { Kind = IccCurveKind.Gamma, Gamma = 2.0 },
                new IccCurve { Kind = IccCurveKind.Gamma, Gamma = 2.0 }
            }
        };
        IccProcessingElement clut = ClutElement(IccProcessingElementSignatures.Clut, IccProcessingElementKind.Clut, 1, 1, new double[] { 0.0, 2.0 });
        IccProcessingElement tint = ClutElement(IccProcessingElementSignatures.TintArray, IccProcessingElementKind.TintArray, 1, 2, new double[] { 0.0, 1.0, 2.0, 3.0 });
        IccProcessingElement acs = new IccProcessingElement
        {
            Signature = IccProcessingElementSignatures.BeginAcs,
            Kind = IccProcessingElementKind.BeginAcs,
            InputChannels = 2,
            OutputChannels = 2
        };

        Assert.Multiple(() =>
        {
            Assert.That(RunSubElement(CalculatorOperationSignatures.ApplyCurves, curves, new double[] { 0.25, 0.5 }), Is.EqualTo(new double[] { 0.0625, 0.25 }).Within(1e-12));
            Assert.That(RunSubElement(CalculatorOperationSignatures.ApplyClut, clut, new double[] { 0.25 }), Is.EqualTo(new double[] { 0.5 }).Within(1e-12));
            Assert.That(RunSubElement(CalculatorOperationSignatures.ApplyTint, tint, new double[] { 0.25 }), Is.EqualTo(new double[] { 0.5, 1.5 }).Within(1e-12));
            Assert.That(RunSubElement(CalculatorOperationSignatures.ApplyElement, acs, new double[] { 0.25, 0.75 }), Is.EqualTo(new double[] { 0.25, 0.75 }));
        });
    }

    [Test]
    public void ExecutesSixteenInputExtendedClutAsCalculatorSubElement()
    {
        int pointCount = 1 << 16;
        double[] values = new double[pointCount];
        values[pointCount - 1] = 0.75;
        IccProcessingElement extended = new IccProcessingElement
        {
            Signature = IccProcessingElementSignatures.ExtendedClut,
            Kind = IccProcessingElementKind.ExtendedClut,
            InputChannels = 16,
            OutputChannels = 1,
            Clut = new IccClut
            {
                InputChannels = 16,
                OutputChannels = 1,
                GridPoints = Enumerable.Repeat(2, 16).ToArray(),
                Values = values,
                Interpolation = IccInterpolationKind.Multilinear
            }
        };

        double[] result = RunSubElement(CalculatorOperationSignatures.ApplyClut, extended, Enumerable.Repeat(1.0, 16).ToArray());

        Assert.That(result, Is.EqualTo(new double[] { 0.75 }).Within(1e-12));
    }

    [Test]
    public void RejectsMalformedMatrixSubElementWithTransformException()
    {
        IccProcessingElement matrix = new IccProcessingElement
        {
            Signature = IccProcessingElementSignatures.Matrix,
            Kind = IccProcessingElementKind.Matrix,
            InputChannels = 1,
            OutputChannels = 1,
            Matrix = new IccMatrix { Rows = 1, Columns = 2, Values = new double[] { 2.0 } }
        };

        Assert.That(
            () => RunSubElement(CalculatorOperationSignatures.ApplyMatrix, matrix, new double[] { 0.5 }),
            Throws.TypeOf<IccTransformException>().With.Message.Contains("matrix sub-element data are inconsistent"));
    }

    [Test]
    public void AcceptsExactInstructionStackStorageAndRecursionLimits()
    {
        IccCalculatorInstruction[] stackProgram =
        {
            Instruction(CalculatorOperationSignatures.Data, FloatOperand(1.0)),
            Instruction(CalculatorOperationSignatures.Data, FloatOperand(2.0)),
            Instruction(CalculatorOperationSignatures.TemporarySave, Selectors(1, 0)),
            Instruction(CalculatorOperationSignatures.Output, Selectors(0, 1))
        };
        IccResourceLimits exact = new IccResourceLimits
        {
            MaximumCalculatorInstructions = 4,
            MaximumCalculatorStackDepth = 2,
            MaximumCalculatorStorageValues = 2
        };
        IccProcessingElement inner = Calculator(
            new IccCalculatorInstruction[]
            {
                Instruction(CalculatorOperationSignatures.Input, Selectors(0, 0)),
                Instruction(CalculatorOperationSignatures.Output, Selectors(0, 0))
            },
            1,
            1,
            Array.Empty<IccProcessingElement>());
        IccProcessingElement outer = Calculator(
            new IccCalculatorInstruction[]
            {
                Instruction(CalculatorOperationSignatures.Input, Selectors(0, 0)),
                Instruction(CalculatorOperationSignatures.ApplyCalculator, Selectors(0, 0)),
                Instruction(CalculatorOperationSignatures.Output, Selectors(0, 0))
            },
            1,
            1,
            new IccProcessingElement[] { inner });
        IccResourceLimits nestedExact = new IccResourceLimits { MaximumCalculatorInstructions = 5, MaximumNestingDepth = 2 };

        Assert.Multiple(() =>
        {
            Assert.That(RunCalculator(stackProgram, new double[] { 0.0 }, 2, Array.Empty<IccProcessingElement>(), Array.Empty<uint>(), Array.Empty<double>(), exact), Is.EqualTo(new double[] { 1.0, 2.0 }));
            Assert.That(Execute(outer, new double[] { 0.75 }, nestedExact), Is.EqualTo(new double[] { 0.75 }));
        });
    }

    [Test]
    public void RejectsOnePastEveryExecutionSafetyLimit()
    {
        IccCalculatorInstruction[] stackProgram =
        {
            Instruction(CalculatorOperationSignatures.Data, FloatOperand(1.0)),
            Instruction(CalculatorOperationSignatures.Data, FloatOperand(2.0)),
            Instruction(CalculatorOperationSignatures.TemporarySave, Selectors(1, 0)),
            Instruction(CalculatorOperationSignatures.Output, Selectors(0, 1))
        };
        IccProcessingElement inner = Calculator(
            new IccCalculatorInstruction[]
            {
                Instruction(CalculatorOperationSignatures.Input, Selectors(0, 0)),
                Instruction(CalculatorOperationSignatures.Output, Selectors(0, 0))
            }, 1, 1, Array.Empty<IccProcessingElement>());
        IccProcessingElement outer = Calculator(
            new IccCalculatorInstruction[]
            {
                Instruction(CalculatorOperationSignatures.Input, Selectors(0, 0)),
                Instruction(CalculatorOperationSignatures.ApplyCalculator, Selectors(0, 0)),
                Instruction(CalculatorOperationSignatures.Output, Selectors(0, 0))
            }, 1, 1, new IccProcessingElement[] { inner });

        Assert.Multiple(() =>
        {
            Assert.That(() => RunCalculator(stackProgram, new double[] { 0.0 }, 2, Array.Empty<IccProcessingElement>(), Array.Empty<uint>(), Array.Empty<double>(), new IccResourceLimits { MaximumCalculatorInstructions = 3 }), Throws.TypeOf<IccTransformException>().With.Message.Contains("instruction limit"));
            Assert.That(() => RunCalculator(stackProgram, new double[] { 0.0 }, 2, Array.Empty<IccProcessingElement>(), Array.Empty<uint>(), Array.Empty<double>(), new IccResourceLimits { MaximumCalculatorStackDepth = 1 }), Throws.TypeOf<IccTransformException>().With.Message.Contains("stack depth"));
            Assert.That(() => RunCalculator(stackProgram, new double[] { 0.0 }, 2, Array.Empty<IccProcessingElement>(), Array.Empty<uint>(), Array.Empty<double>(), new IccResourceLimits { MaximumCalculatorStorageValues = 1 }), Throws.TypeOf<IccTransformException>().With.Message.Contains("storage"));
            Assert.That(() => Execute(outer, new double[] { 0.75 }, new IccResourceLimits { MaximumNestingDepth = 1 }), Throws.TypeOf<IccTransformException>().With.Message.Contains("recursion depth"));
        });
    }

    [Test]
    public void IndependentlyChecksOperandStackAndStorageEffects()
    {
        IccCalculatorInstruction[] instructions =
        {
            Instruction(CalculatorOperationSignatures.Input, Selectors(0, 2)),
            Instruction(CalculatorOperationSignatures.TemporarySave, Selectors(4, 2)),
            Instruction(CalculatorOperationSignatures.Copy, Selectors(2, 1)),
            Instruction(CalculatorOperationSignatures.Pop, Selectors(8, 0)),
            Instruction(CalculatorOperationSignatures.Environment, 0x74657374),
            Instruction(CalculatorOperationSignatures.Pop, Selectors(1, 0))
        };
        IccProcessingElement element = Calculator(instructions, 3, 1, Array.Empty<IccProcessingElement>());

        string error = CalculatorElementCodec.ValidateProgram(element.Calculator!, element, 9, 7, 4);

        Assert.Multiple(() =>
        {
            Assert.That(error, Is.Empty);
            Assert.That(element.Calculator!.RequiredStackDepth, Is.EqualTo(9));
            Assert.That(element.Calculator.RequiredStorageValues, Is.EqualTo(7));
        });
    }

    [Test]
    public void RejectsMalformedOperandsControlStreamsAndSubElementReferences()
    {
        IccProcessingElement curve = new IccProcessingElement
        {
            Signature = IccProcessingElementSignatures.CurveSet,
            Kind = IccProcessingElementKind.CurveSet,
            InputChannels = 1,
            OutputChannels = 1,
            Curves = new IccCurve[] { new IccCurve() }
        };

        Assert.Multiple(() =>
        {
            AssertProgramError(new IccCalculatorInstruction[] { Instruction(CalculatorOperationSignatures.NoOperation, 1) }, 1, 1, Array.Empty<IccProcessingElement>(), "zero operand");
            AssertProgramError(new IccCalculatorInstruction[] { Instruction(CalculatorOperationSignatures.Sum, Selectors(0, 1)) }, 1, 1, Array.Empty<IccProcessingElement>(), "reserved selector");
            AssertProgramError(new IccCalculatorInstruction[] { Instruction(CalculatorOperationSignatures.Input, Selectors(1, 0)) }, 1, 1, Array.Empty<IccProcessingElement>(), "outside the element input vector");
            AssertProgramError(new IccCalculatorInstruction[] { Instruction(CalculatorOperationSignatures.Output, Selectors(0, 1)) }, 1, 1, Array.Empty<IccProcessingElement>(), "outside the element output vector");
            AssertProgramError(new IccCalculatorInstruction[] { Instruction(CalculatorOperationSignatures.ApplyMatrix, Selectors(0, 0)) }, 1, 1, new IccProcessingElement[] { curve }, "incompatible sub-element kind");
            AssertProgramError(new IccCalculatorInstruction[] { Instruction(CalculatorOperationSignatures.Add, 0) }, 1, 1, Array.Empty<IccProcessingElement>(), "underflow");
            AssertProgramError(new IccCalculatorInstruction[] { Instruction(CalculatorOperationSignatures.If, 2) }, 1, 1, Array.Empty<IccProcessingElement>(), "underflow");
            AssertProgramError(new IccCalculatorInstruction[] { Instruction(CalculatorOperationSignatures.Data, 0), Instruction(CalculatorOperationSignatures.If, 2) }, 1, 1, Array.Empty<IccProcessingElement>(), "extends beyond");
            AssertProgramError(new IccCalculatorInstruction[] { Instruction(CalculatorOperationSignatures.Data, 0), Instruction(CalculatorOperationSignatures.Select, 0) }, 1, 1, Array.Empty<IccProcessingElement>(), "at least one case");
            AssertProgramError(new IccCalculatorInstruction[] { Instruction(CalculatorOperationSignatures.Case, 0) }, 1, 1, Array.Empty<IccProcessingElement>(), "outside its required control header");
        });
    }

    private static void AssertProgramError(IccCalculatorInstruction[] instructions, int inputs, int outputs, IccProcessingElement[] subElements, string expected)
    {
        IccProcessingElement element = Calculator(instructions, inputs, outputs, subElements);
        string error = CalculatorElementCodec.ValidateProgram(element.Calculator!, element, 1024, 65536, 32);
        Assert.That(error, Does.Contain(expected));
    }

    private static double RunUnary(uint signature, double value)
    {
        return RunCalculator(DataProgram(new double[] { value }, signature, 0, 1), new double[] { 0.0 }, 1, Array.Empty<IccProcessingElement>(), Array.Empty<uint>(), Array.Empty<double>(), new IccResourceLimits())[0];
    }

    private static double RunBinary(uint signature, double first, double second)
    {
        return RunCalculator(DataProgram(new double[] { first, second }, signature, 0, 1), new double[] { 0.0 }, 1, Array.Empty<IccProcessingElement>(), Array.Empty<uint>(), Array.Empty<double>(), new IccResourceLimits())[0];
    }

    private static double RunSelection(double selector)
    {
        IccCalculatorInstruction[] instructions =
        {
            Instruction(CalculatorOperationSignatures.Data, FloatOperand(selector)),
            Instruction(CalculatorOperationSignatures.Select, 0),
            Instruction(CalculatorOperationSignatures.Case, 1),
            Instruction(CalculatorOperationSignatures.Case, 1),
            Instruction(CalculatorOperationSignatures.Default, 1),
            Instruction(CalculatorOperationSignatures.Data, FloatOperand(10.0)),
            Instruction(CalculatorOperationSignatures.Data, FloatOperand(20.0)),
            Instruction(CalculatorOperationSignatures.Data, FloatOperand(30.0)),
            Instruction(CalculatorOperationSignatures.Output, Selectors(0, 0))
        };
        return RunCalculator(instructions, new double[] { 0.0 }, 1, Array.Empty<IccProcessingElement>(), Array.Empty<uint>(), Array.Empty<double>(), new IccResourceLimits())[0];
    }

    private static double[] RunSubElement(uint operation, IccProcessingElement subElement, double[] inputs)
    {
        IccCalculatorInstruction[] instructions =
        {
            Instruction(CalculatorOperationSignatures.Input, Selectors(0, inputs.Length - 1)),
            Instruction(operation, Selectors(0, 0)),
            Instruction(CalculatorOperationSignatures.Output, Selectors(0, subElement.OutputChannels - 1))
        };
        return RunCalculator(instructions, inputs, subElement.OutputChannels, new IccProcessingElement[] { subElement }, Array.Empty<uint>(), Array.Empty<double>(), new IccResourceLimits());
    }

    private static IccProcessingElement ClutElement(uint signature, IccProcessingElementKind kind, int inputs, int outputs, double[] values)
    {
        return new IccProcessingElement
        {
            Signature = signature,
            Kind = kind,
            InputChannels = inputs,
            OutputChannels = outputs,
            Clut = new IccClut
            {
                InputChannels = inputs,
                OutputChannels = outputs,
                GridPoints = Enumerable.Repeat(2, inputs).ToArray(),
                Values = values,
                Interpolation = IccInterpolationKind.Multilinear
            }
        };
    }

    private static IccCalculatorInstruction[] DataProgram(double[] values, uint operation, uint operand, int outputCount)
    {
        IccCalculatorInstruction[] instructions = new IccCalculatorInstruction[values.Length + 2];
        int index = 0;
        while (index < values.Length)
        {
            instructions[index] = Instruction(CalculatorOperationSignatures.Data, FloatOperand(values[index]));
            index++;
        }
        instructions[index] = Instruction(operation, operand);
        instructions[index + 1] = Instruction(CalculatorOperationSignatures.Output, Selectors(0, outputCount - 1));
        return instructions;
    }

    private static double[] RunCalculator(IccCalculatorInstruction[] instructions, double[] inputs, int outputChannels, IccProcessingElement[] subElements, uint[] environmentSignatures, double[] environmentValues, IccResourceLimits limits)
    {
        IccProcessingElement calculator = Calculator(instructions, inputs.Length, outputChannels, subElements);
        double[] outputs = new double[outputChannels];
        CalculatorVirtualMachine.Execute(calculator, inputs, outputs, limits, new CalculatorEnvironment(environmentSignatures, environmentValues));
        return outputs;
    }

    private static double[] Execute(IccProcessingElement element, double[] inputs, IccResourceLimits limits)
    {
        double[] outputs = new double[element.OutputChannels];
        CalculatorVirtualMachine.Execute(element, inputs, outputs, limits, new CalculatorEnvironment(Array.Empty<uint>(), Array.Empty<double>()));
        return outputs;
    }

    private static IccProcessingElement Calculator(IccCalculatorInstruction[] instructions, int inputs, int outputs, IccProcessingElement[] subElements)
    {
        return new IccProcessingElement
        {
            Signature = IccProcessingElementSignatures.Calculator,
            Kind = IccProcessingElementKind.Calculator,
            InputChannels = inputs,
            OutputChannels = outputs,
            Calculator = new IccCalculatorProgram { Instructions = instructions, SubElements = subElements }
        };
    }

    private static IccCalculatorInstruction Instruction(uint signature, uint operand)
    {
        return new IccCalculatorInstruction { Signature = signature, Operand = operand };
    }

    private static uint Selectors(int first, int second)
    {
        return ((uint)first << 16) | (uint)second;
    }

    private static uint FloatOperand(double value)
    {
        return (uint)BitConverter.SingleToInt32Bits((float)value);
    }
}
