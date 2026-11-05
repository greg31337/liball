using icc;

namespace icc.Tests;

public class CalculatorVirtualMachineTests
{
    [Test]
    public void ExecutesChannelsTemporaryStorageSequencesAndEnvironmentLookup()
    {
        IccCalculatorInstruction[] instructions =
        {
            Instruction(CalculatorOperationSignatures.Input, Selectors(0, 2)),
            Instruction(CalculatorOperationSignatures.TemporarySave, Selectors(0, 2)),
            Instruction(CalculatorOperationSignatures.Pop, Selectors(2, 0)),
            Instruction(CalculatorOperationSignatures.TemporaryGet, Selectors(0, 2)),
            Instruction(CalculatorOperationSignatures.Sum, Selectors(1, 0)),
            Instruction(CalculatorOperationSignatures.Environment, 0x74657374),
            Instruction(CalculatorOperationSignatures.Pop, Selectors(0, 0)),
            Instruction(CalculatorOperationSignatures.Sum, Selectors(0, 0)),
            Instruction(CalculatorOperationSignatures.Output, Selectors(0, 0))
        };

        double[] result = RunCalculator(instructions, new double[] { 1.0, 2.0, 3.0 }, 1, Array.Empty<IccProcessingElement>(), new uint[] { 0x74657374 }, new double[] { 4.0 }, new IccResourceLimits());

        Assert.That(result, Is.EqualTo(new double[] { 10.0 }));
    }

    [Test]
    public void ExecutesStackManipulationWithDefinedOrdering()
    {
        IccCalculatorInstruction[] instructions =
        {
            Instruction(CalculatorOperationSignatures.Input, Selectors(0, 2)),
            Instruction(CalculatorOperationSignatures.Copy, Selectors(2, 0)),
            Instruction(CalculatorOperationSignatures.RotateLeft, Selectors(2, 0)),
            Instruction(CalculatorOperationSignatures.RotateRight, Selectors(2, 0)),
            Instruction(CalculatorOperationSignatures.PositionDuplicate, Selectors(2, 1)),
            Instruction(CalculatorOperationSignatures.Flip, Selectors(1, 0)),
            Instruction(CalculatorOperationSignatures.Output, Selectors(0, 7)),
            Instruction(CalculatorOperationSignatures.StackCount, 0),
            Instruction(CalculatorOperationSignatures.Output, Selectors(8, 0))
        };

        double[] result = RunCalculator(instructions, new double[] { 1.0, 2.0, 3.0 }, 9, Array.Empty<IccProcessingElement>(), Array.Empty<uint>(), Array.Empty<double>(), new IccResourceLimits());

        Assert.That(result, Is.EqualTo(new double[] { 1.0, 2.0, 3.0, 1.0, 2.0, 1.0, 1.0, 3.0, 0.0 }));
    }

    [Test]
    public void ExecutesEveryBinaryAndScalarVectorOperation()
    {
        (uint Signature, double First, double Second, double Expected)[] binaryCases =
        {
            (CalculatorOperationSignatures.Add, 6.0, 2.0, 8.0),
            (CalculatorOperationSignatures.Subtract, 6.0, 2.0, 4.0),
            (CalculatorOperationSignatures.Multiply, 6.0, 2.0, 12.0),
            (CalculatorOperationSignatures.Divide, 6.0, 2.0, 3.0),
            (CalculatorOperationSignatures.Modulus, 7.0, 4.0, 3.0),
            (CalculatorOperationSignatures.Power, 3.0, 2.0, 9.0),
            (CalculatorOperationSignatures.ArcTangent2, 1.0, 1.0, Math.PI / 4.0),
            (CalculatorOperationSignatures.LessThan, 1.0, 2.0, 1.0),
            (CalculatorOperationSignatures.LessThanOrEqual, 2.0, 2.0, 1.0),
            (CalculatorOperationSignatures.Equal, 2.0, 2.0, 1.0),
            (CalculatorOperationSignatures.NotEqual, 2.0, 3.0, 1.0),
            (CalculatorOperationSignatures.Near, 2.0, 2.000000005, 1.0),
            (CalculatorOperationSignatures.GreaterThanOrEqual, 2.0, 2.0, 1.0),
            (CalculatorOperationSignatures.GreaterThan, 3.0, 2.0, 1.0),
            (CalculatorOperationSignatures.VectorMinimum, 3.0, 2.0, 2.0),
            (CalculatorOperationSignatures.VectorMaximum, 3.0, 2.0, 3.0),
            (CalculatorOperationSignatures.VectorAnd, 0.5, 0.75, 1.0),
            (CalculatorOperationSignatures.VectorXor, 0.25, 0.75, 1.0),
            (CalculatorOperationSignatures.VectorOr, 0.25, 0.75, 1.0)
        };
        foreach ((uint signature, double first, double second, double expected) in binaryCases)
        {
            double actual = RunSingleBinary(signature, first, second);
            Assert.That(actual, Is.EqualTo(expected).Within(1e-12), FourCc(signature));
        }

        (uint Signature, double Expected)[] scalarCases =
        {
            (CalculatorOperationSignatures.Gamma, 9.0),
            (CalculatorOperationSignatures.ScalarAdd, 5.0),
            (CalculatorOperationSignatures.ScalarSubtract, 1.0),
            (CalculatorOperationSignatures.ScalarMultiply, 6.0),
            (CalculatorOperationSignatures.ScalarDivide, 1.5)
        };
        foreach ((uint signature, double expected) in scalarCases)
        {
            double actual = RunSingleBinary(signature, 3.0, 2.0);
            Assert.That(actual, Is.EqualTo(expected).Within(1e-12), FourCc(signature));
        }
    }

    [Test]
    public void ExecutesEveryUnaryOperationAndIeeeSpecialValues()
    {
        (uint Signature, double Input, double Expected)[] cases =
        {
            (CalculatorOperationSignatures.Negate, 2.0, -2.0),
            (CalculatorOperationSignatures.Square, 3.0, 9.0),
            (CalculatorOperationSignatures.SquareRoot, 9.0, 3.0),
            (CalculatorOperationSignatures.Cube, 2.0, 8.0),
            (CalculatorOperationSignatures.CubeRoot, -8.0, -2.0),
            (CalculatorOperationSignatures.Sign, -2.0, -1.0),
            (CalculatorOperationSignatures.Absolute, -2.0, 2.0),
            (CalculatorOperationSignatures.Floor, 2.8, 2.0),
            (CalculatorOperationSignatures.Ceiling, 2.2, 3.0),
            (CalculatorOperationSignatures.Truncate, -2.8, -2.0),
            (CalculatorOperationSignatures.Round, -2.5, -3.0),
            (CalculatorOperationSignatures.Exponential, 0.0, 1.0),
            (CalculatorOperationSignatures.Logarithm, 100.0, 2.0),
            (CalculatorOperationSignatures.NaturalLogarithm, 1.0, 0.0),
            (CalculatorOperationSignatures.Sine, Math.PI / 2.0, 1.0),
            (CalculatorOperationSignatures.Cosine, 0.0, 1.0),
            (CalculatorOperationSignatures.Tangent, 0.0, 0.0),
            (CalculatorOperationSignatures.ArcSine, 1.0, Math.PI / 2.0),
            (CalculatorOperationSignatures.ArcCosine, 1.0, 0.0),
            (CalculatorOperationSignatures.ArcTangent, 1.0, Math.PI / 4.0),
            (CalculatorOperationSignatures.RealNumber, 4.0, 1.0),
            (CalculatorOperationSignatures.Not, 0.25, 1.0)
        };
        foreach ((uint signature, double input, double expected) in cases)
        {
            double actual = RunSingleUnary(signature, input);
            Assert.That(actual, Is.EqualTo(expected).Within(1e-12), FourCc(signature));
        }

        Assert.Multiple(() =>
        {
            Assert.That(RunSingleBinary(CalculatorOperationSignatures.Divide, 1.0, 0.0), Is.EqualTo(double.PositiveInfinity));
            Assert.That(RunSingleBinary(CalculatorOperationSignatures.Divide, 0.0, 0.0), Is.NaN);
            Assert.That(RunConstant(CalculatorOperationSignatures.PositiveInfinity), Is.EqualTo(double.PositiveInfinity));
            Assert.That(RunConstant(CalculatorOperationSignatures.NegativeInfinity), Is.EqualTo(double.NegativeInfinity));
            Assert.That(RunConstant(CalculatorOperationSignatures.NotANumber), Is.NaN);
            Assert.That(RunConstant(CalculatorOperationSignatures.Pi), Is.EqualTo(Math.PI));
        });
    }

    [Test]
    public void ExecutesSequenceCoordinateAndNormalizedLabOperations()
    {
        (uint Signature, double Expected)[] sequences =
        {
            (CalculatorOperationSignatures.Sum, 9.0),
            (CalculatorOperationSignatures.Product, 24.0),
            (CalculatorOperationSignatures.Minimum, 2.0),
            (CalculatorOperationSignatures.Maximum, 4.0),
            (CalculatorOperationSignatures.And, 1.0),
            (CalculatorOperationSignatures.Or, 1.0)
        };
        foreach ((uint signature, double expected) in sequences)
        {
            double[] values = signature == CalculatorOperationSignatures.And ? new double[] { 0.5, 0.6, 0.7 } : signature == CalculatorOperationSignatures.Or ? new double[] { 0.1, 0.2, 0.7 } : new double[] { 2.0, 3.0, 4.0 };
            IccCalculatorInstruction[] instructions = DataInstructions(values, signature, Selectors(1, 0), 1);
            double actual = RunCalculator(instructions, Array.Empty<double>(), 1, Array.Empty<IccProcessingElement>(), Array.Empty<uint>(), Array.Empty<double>(), new IccResourceLimits())[0];
            Assert.That(actual, Is.EqualTo(expected), FourCc(signature));
        }

        IccCalculatorInstruction[] polar = DataInstructions(new double[] { 3.0, 4.0 }, CalculatorOperationSignatures.CartesianToPolar, Selectors(0, 0), 2);
        double[] polarResult = RunCalculator(polar, Array.Empty<double>(), 2, Array.Empty<IccProcessingElement>(), Array.Empty<uint>(), Array.Empty<double>(), new IccResourceLimits());
        IccCalculatorInstruction[] cartesian = DataInstructions(polarResult, CalculatorOperationSignatures.PolarToCartesian, Selectors(0, 0), 2);
        double[] cartesianResult = RunCalculator(cartesian, Array.Empty<double>(), 2, Array.Empty<IccProcessingElement>(), Array.Empty<uint>(), Array.Empty<double>(), new IccResourceLimits());

        IccCalculatorInstruction[] toLab = DataInstructions(new double[] { 0.2, 0.3, 0.4 }, CalculatorOperationSignatures.ToLab, Selectors(0, 0), 3);
        double[] lab = RunCalculator(toLab, Array.Empty<double>(), 3, Array.Empty<IccProcessingElement>(), Array.Empty<uint>(), Array.Empty<double>(), new IccResourceLimits());
        IccCalculatorInstruction[] toXyz = DataInstructions(lab, CalculatorOperationSignatures.ToXyz, Selectors(0, 0), 3);
        double[] xyz = RunCalculator(toXyz, Array.Empty<double>(), 3, Array.Empty<IccProcessingElement>(), Array.Empty<uint>(), Array.Empty<double>(), new IccResourceLimits());

        Assert.Multiple(() =>
        {
            Assert.That(polarResult[0], Is.EqualTo(5.0).Within(1e-12));
            Assert.That(polarResult[1], Is.EqualTo(53.13010235415598).Within(1e-12));
            Assert.That(cartesianResult, Is.EqualTo(new double[] { 3.0, 4.0 }).Within(1e-5));
            Assert.That(xyz, Is.EqualTo(new double[] { 0.2, 0.3, 0.4 }).Within(1e-6));
        });
    }

    [Test]
    public void ExecutesConditionalAndSelectionStreamsIncludingNanCondition()
    {
        Assert.Multiple(() =>
        {
            Assert.That(RunConditional(1.0), Is.EqualTo(11.0));
            Assert.That(RunConditional(0.0), Is.EqualTo(22.0));
            Assert.That(RunConditional(double.NaN), Is.EqualTo(11.0));
            Assert.That(RunSelection(0.49), Is.EqualTo(10.0));
            Assert.That(RunSelection(0.5), Is.EqualTo(20.0));
            Assert.That(RunSelection(-2.0), Is.EqualTo(30.0));
        });
    }

    [Test]
    public void TransposesAndSolvesMatricesWithAStatusValue()
    {
        IccCalculatorInstruction[] transpose = DataInstructions(new double[] { 1, 2, 3, 4, 5, 6 }, CalculatorOperationSignatures.Transpose, Selectors(1, 2), 6);
        double[] transposed = RunCalculator(transpose, Array.Empty<double>(), 6, Array.Empty<IccProcessingElement>(), Array.Empty<uint>(), Array.Empty<double>(), new IccResourceLimits());
        IccCalculatorInstruction[] solve = DataInstructions(new double[] { 2, 0, 0, 4, 6, 8 }, CalculatorOperationSignatures.Solve, Selectors(1, 1), 3);
        double[] solved = RunCalculator(solve, Array.Empty<double>(), 3, Array.Empty<IccProcessingElement>(), Array.Empty<uint>(), Array.Empty<double>(), new IccResourceLimits());
        IccCalculatorInstruction[] singular = DataInstructions(new double[] { 1, 2, 2, 4, 3, 6 }, CalculatorOperationSignatures.Solve, Selectors(1, 1), 3);
        double[] failed = RunCalculator(singular, Array.Empty<double>(), 3, Array.Empty<IccProcessingElement>(), Array.Empty<uint>(), Array.Empty<double>(), new IccResourceLimits());

        Assert.Multiple(() =>
        {
            Assert.That(transposed, Is.EqualTo(new double[] { 1, 4, 2, 5, 3, 6 }));
            Assert.That(solved, Is.EqualTo(new double[] { 3, 2, 1 }).Within(1e-12));
            Assert.That(failed, Is.EqualTo(new double[] { 0, 0, 0 }));
        });
    }

    [Test]
    public void InvokesMatrixAndNestedCalculatorSubElementsWithoutSharingTemporaryStorage()
    {
        IccProcessingElement matrix = new IccProcessingElement
        {
            Signature = IccProcessingElementSignatures.Matrix,
            Kind = IccProcessingElementKind.Matrix,
            InputChannels = 1,
            OutputChannels = 1,
            Matrix = new IccMatrix { Rows = 1, Columns = 2, Values = new double[] { 2.0, 1.0 } }
        };
        IccProcessingElement nested = CreateCalculatorElement(new IccCalculatorInstruction[]
        {
            Instruction(CalculatorOperationSignatures.Input, Selectors(0, 0)),
            Instruction(CalculatorOperationSignatures.TemporaryGet, Selectors(0, 0)),
            Instruction(CalculatorOperationSignatures.Add, Selectors(0, 0)),
            Instruction(CalculatorOperationSignatures.Output, Selectors(0, 0))
        }, 1, 1, Array.Empty<IccProcessingElement>());
        IccCalculatorInstruction[] instructions =
        {
            Instruction(CalculatorOperationSignatures.Input, Selectors(0, 0)),
            Instruction(CalculatorOperationSignatures.TemporarySave, Selectors(0, 0)),
            Instruction(CalculatorOperationSignatures.ApplyMatrix, Selectors(0, 0)),
            Instruction(CalculatorOperationSignatures.ApplyCalculator, Selectors(1, 0)),
            Instruction(CalculatorOperationSignatures.Output, Selectors(0, 0))
        };

        double[] result = RunCalculator(instructions, new double[] { 3.0 }, 1, new IccProcessingElement[] { matrix, nested }, Array.Empty<uint>(), Array.Empty<double>(), new IccResourceLimits());

        Assert.That(result, Is.EqualTo(new double[] { 7.0 }));
    }

    [Test]
    public void EnforcesInstructionStackStorageAndRecursionBounds()
    {
        IccResourceLimits instructionLimits = new IccResourceLimits { MaximumCalculatorInstructions = 1 };
        IccCalculatorInstruction[] twoInstructions =
        {
            Instruction(CalculatorOperationSignatures.Data, FloatOperand(1.0)),
            Instruction(CalculatorOperationSignatures.Output, Selectors(0, 0))
        };
        IccResourceLimits stackLimits = new IccResourceLimits { MaximumCalculatorStackDepth = 1 };
        IccCalculatorInstruction[] stackInstructions =
        {
            Instruction(CalculatorOperationSignatures.Data, FloatOperand(1.0)),
            Instruction(CalculatorOperationSignatures.Data, FloatOperand(2.0)),
            Instruction(CalculatorOperationSignatures.Output, Selectors(0, 1))
        };
        IccResourceLimits storageLimits = new IccResourceLimits { MaximumCalculatorStorageValues = 1 };
        IccCalculatorInstruction[] storage =
        {
            Instruction(CalculatorOperationSignatures.TemporaryGet, Selectors(1, 0)),
            Instruction(CalculatorOperationSignatures.Output, Selectors(0, 0))
        };
        IccProcessingElement circular = CreateCalculatorElement(Array.Empty<IccCalculatorInstruction>(), 1, 1, Array.Empty<IccProcessingElement>());
        circular.Calculator!.Instructions = new IccCalculatorInstruction[]
        {
            Instruction(CalculatorOperationSignatures.Input, Selectors(0, 0)),
            Instruction(CalculatorOperationSignatures.ApplyCalculator, Selectors(0, 0)),
            Instruction(CalculatorOperationSignatures.Output, Selectors(0, 0))
        };
        circular.Calculator.SubElements = new IccProcessingElement[] { circular };

        Assert.Multiple(() =>
        {
            Assert.That(() => RunCalculator(twoInstructions, Array.Empty<double>(), 1, Array.Empty<IccProcessingElement>(), Array.Empty<uint>(), Array.Empty<double>(), instructionLimits), Throws.TypeOf<IccTransformException>().With.Message.Contains("instruction limit"));
            Assert.That(() => RunCalculator(stackInstructions, Array.Empty<double>(), 2, Array.Empty<IccProcessingElement>(), Array.Empty<uint>(), Array.Empty<double>(), stackLimits), Throws.TypeOf<IccTransformException>().With.Message.Contains("stack depth"));
            Assert.That(() => RunCalculator(storage, Array.Empty<double>(), 1, Array.Empty<IccProcessingElement>(), Array.Empty<uint>(), Array.Empty<double>(), storageLimits), Throws.TypeOf<IccTransformException>().With.Message.Contains("storage"));
            Assert.That(() => ExecuteElement(circular, new double[] { 1.0 }, new IccResourceLimits()), Throws.TypeOf<IccTransformException>().With.Message.Contains("circular"));
        });
    }

    [Test]
    public void RejectsControlBranchesWithDifferentStackEffects()
    {
        IccCalculatorInstruction[] instructions =
        {
            Instruction(CalculatorOperationSignatures.Data, FloatOperand(1.0)),
            Instruction(CalculatorOperationSignatures.If, 1),
            Instruction(CalculatorOperationSignatures.Else, 0),
            Instruction(CalculatorOperationSignatures.Data, FloatOperand(2.0))
        };

        Assert.That(
            () => RunCalculator(instructions, Array.Empty<double>(), 1, Array.Empty<IccProcessingElement>(), Array.Empty<uint>(), Array.Empty<double>(), new IccResourceLimits()),
            Throws.TypeOf<IccTransformException>().With.Message.Contains("identical stack effects"));
    }

    private static double RunSingleBinary(uint signature, double first, double second)
    {
        IccCalculatorInstruction[] instructions = DataInstructions(new double[] { first, second }, signature, Selectors(0, 0), 1);
        return RunCalculator(instructions, Array.Empty<double>(), 1, Array.Empty<IccProcessingElement>(), Array.Empty<uint>(), Array.Empty<double>(), new IccResourceLimits())[0];
    }

    private static double RunSingleUnary(uint signature, double value)
    {
        IccCalculatorInstruction[] instructions = DataInstructions(new double[] { value }, signature, Selectors(0, 0), 1);
        return RunCalculator(instructions, Array.Empty<double>(), 1, Array.Empty<IccProcessingElement>(), Array.Empty<uint>(), Array.Empty<double>(), new IccResourceLimits())[0];
    }

    private static double RunConstant(uint signature)
    {
        IccCalculatorInstruction[] instructions =
        {
            Instruction(signature, 0),
            Instruction(CalculatorOperationSignatures.Output, Selectors(0, 0))
        };
        return RunCalculator(instructions, Array.Empty<double>(), 1, Array.Empty<IccProcessingElement>(), Array.Empty<uint>(), Array.Empty<double>(), new IccResourceLimits())[0];
    }

    private static double RunConditional(double condition)
    {
        IccCalculatorInstruction[] instructions =
        {
            Instruction(CalculatorOperationSignatures.Data, FloatOperand(condition)),
            Instruction(CalculatorOperationSignatures.If, 1),
            Instruction(CalculatorOperationSignatures.Else, 1),
            Instruction(CalculatorOperationSignatures.Data, FloatOperand(11.0)),
            Instruction(CalculatorOperationSignatures.Data, FloatOperand(22.0)),
            Instruction(CalculatorOperationSignatures.Output, Selectors(0, 0))
        };
        return RunCalculator(instructions, Array.Empty<double>(), 1, Array.Empty<IccProcessingElement>(), Array.Empty<uint>(), Array.Empty<double>(), new IccResourceLimits())[0];
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
        return RunCalculator(instructions, Array.Empty<double>(), 1, Array.Empty<IccProcessingElement>(), Array.Empty<uint>(), Array.Empty<double>(), new IccResourceLimits())[0];
    }

    private static IccCalculatorInstruction[] DataInstructions(double[] values, uint signature, uint operand, int outputCount)
    {
        IccCalculatorInstruction[] instructions = new IccCalculatorInstruction[values.Length + 2];
        int index = 0;
        while (index < values.Length)
        {
            instructions[index] = Instruction(CalculatorOperationSignatures.Data, FloatOperand(values[index]));
            index++;
        }
        instructions[index] = Instruction(signature, operand);
        instructions[index + 1] = Instruction(CalculatorOperationSignatures.Output, Selectors(0, outputCount - 1));
        return instructions;
    }

    private static double[] RunCalculator(IccCalculatorInstruction[] instructions, double[] inputs, int outputChannels, IccProcessingElement[] subElements, uint[] environmentSignatures, double[] environmentValues, IccResourceLimits limits)
    {
        double[] executionInputs = inputs.Length == 0 ? new double[] { 0.0 } : inputs;
        IccProcessingElement element = CreateCalculatorElement(instructions, executionInputs.Length, outputChannels, subElements);
        double[] outputs = new double[outputChannels];
        CalculatorVirtualMachine.Execute(element, executionInputs, outputs, limits, new CalculatorEnvironment(environmentSignatures, environmentValues));
        return outputs;
    }

    private static void ExecuteElement(IccProcessingElement element, double[] inputs, IccResourceLimits limits)
    {
        double[] outputs = new double[element.OutputChannels];
        CalculatorVirtualMachine.Execute(element, inputs, outputs, limits, new CalculatorEnvironment(Array.Empty<uint>(), Array.Empty<double>()));
    }

    private static IccProcessingElement CreateCalculatorElement(IccCalculatorInstruction[] instructions, int inputChannels, int outputChannels, IccProcessingElement[] subElements)
    {
        return new IccProcessingElement
        {
            Signature = IccProcessingElementSignatures.Calculator,
            Kind = IccProcessingElementKind.Calculator,
            InputChannels = inputChannels,
            OutputChannels = outputChannels,
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

    private static string FourCc(uint signature)
    {
        return FourCcCodec.Format(signature);
    }
}
