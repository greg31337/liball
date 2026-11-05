using icc;

namespace icc.Tests;

public class IccCamMathTests
{
    [Test]
    public void ConvertsReferenceWhiteAndBlackToStableAppearanceValues()
    {
        IccCamViewingConditions conditions = CreateConditions();
        double[] white = new double[3];
        double[] black = new double[3];

        IccCamMath.ConvertXyzToJab(conditions, new double[] { conditions.WhitePoint.X, conditions.WhitePoint.Y, conditions.WhitePoint.Z }, white);
        IccCamMath.ConvertXyzToJab(conditions, new double[] { 0.0, 0.0, 0.0 }, black);

        Assert.Multiple(() =>
        {
            Assert.That(white[0], Is.EqualTo(100.0).Within(1e-10));
            Assert.That(white, Has.All.Matches<double>(double.IsFinite));
            Assert.That(black, Is.EqualTo(new double[] { 0.0, 0.0, 0.0 }).Within(1e-10));
        });
    }

    [Test]
    public void RoundTripsRepresentativeXyzValuesThroughJab()
    {
        IccCamViewingConditions conditions = CreateConditions();
        double[][] samples =
        {
            new double[] { 0.0, 0.0, 0.0 },
            new double[] { 0.2, 0.25, 0.3 },
            new double[] { 0.8, 0.4, 0.1 },
            new double[] { conditions.WhitePoint.X, conditions.WhitePoint.Y, conditions.WhitePoint.Z }
        };

        foreach (double[] sample in samples)
        {
            double[] jab = new double[3];
            double[] roundTrip = new double[3];
            IccCamMath.ConvertXyzToJab(conditions, sample, jab);
            IccCamMath.ConvertJabToXyz(conditions, jab, roundTrip);

            Assert.That(roundTrip, Is.EqualTo(sample).Within(0.00001), $"XYZ {string.Join(", ", sample)} produced Jab {string.Join(", ", jab)}");
        }
    }

    [Test]
    public void InverseRemainsFiniteForBlackAchromaticAndHueAxisValues()
    {
        IccCamViewingConditions conditions = CreateConditions();
        double[][] samples =
        {
            new double[] { 0.0, 0.0, 0.0 },
            new double[] { 50.0, 0.0, 0.0 },
            new double[] { 50.0, 20.0, 0.0 },
            new double[] { 50.0, 0.0, 20.0 },
            new double[] { 50.0, -20.0, 0.0 },
            new double[] { 50.0, 0.0, -20.0 }
        };

        foreach (double[] sample in samples)
        {
            double[] xyz = new double[3];
            IccCamMath.ConvertJabToXyz(conditions, sample, xyz);
            Assert.That(xyz, Has.All.Matches<double>(double.IsFinite), $"Jab {string.Join(", ", sample)}");
        }
    }

    [Test]
    public void ClipsColoursOutsideTheHpeTriangleWithoutProducingNonFiniteValues()
    {
        IccCamViewingConditions conditions = CreateConditions();
        double[] jab = new double[3];

        IccCamMath.ConvertXyzToJab(conditions, new double[] { -1.0, 0.2, 0.1 }, jab);

        Assert.That(jab, Has.All.Matches<double>(double.IsFinite));
    }

    [Test]
    public void ProducesDeterministicCartesianAppearanceCorrelates()
    {
        double[] jab = new double[3];

        IccCamMath.ConvertXyzToJab(CreateConditions(), new double[] { 0.8, 0.4, 0.1 }, jab);

        Assert.That(jab, Is.EqualTo(new double[] { 7.639294682419108, 59.27659736460703, 15.514473377352937 }).Within(1e-12));
    }

    [Test]
    public void RejectsDegenerateViewingConditionsAndNonFiniteSamples()
    {
        IccCamViewingConditions zeroWhite = CreateConditions();
        zeroWhite.WhitePoint.Y = 0.0;
        IccCamViewingConditions zeroLuminance = CreateConditions();
        zeroLuminance.Luminance = 0.0;
        IccCamViewingConditions zeroBackground = CreateConditions();
        zeroBackground.BackgroundLuminance = 0.0;
        IccCamViewingConditions zeroSurround = CreateConditions();
        zeroSurround.ImpactOfSurround = 0.0;
        IccCamViewingConditions zeroInduction = CreateConditions();
        zeroInduction.ChromaticInductionFactor = 0.0;
        IccCamViewingConditions invalidAdaptation = CreateConditions();
        invalidAdaptation.AdaptationFactor = 1.1;

        Assert.Multiple(() =>
        {
            Assert.That(() => ConvertWhite(zeroWhite), Throws.TypeOf<IccTransformException>().With.Message.Contains("white-point"));
            Assert.That(() => ConvertWhite(zeroLuminance), Throws.TypeOf<IccTransformException>().With.Message.Contains("adapting-field"));
            Assert.That(() => ConvertWhite(zeroBackground), Throws.TypeOf<IccTransformException>().With.Message.Contains("background"));
            Assert.That(() => ConvertWhite(zeroSurround), Throws.TypeOf<IccTransformException>().With.Message.Contains("surround"));
            Assert.That(() => ConvertWhite(zeroInduction), Throws.TypeOf<IccTransformException>().With.Message.Contains("induction"));
            Assert.That(() => ConvertWhite(invalidAdaptation), Throws.TypeOf<IccTransformException>().With.Message.Contains("adaptation factor"));
            Assert.That(() => IccCamMath.ConvertXyzToJab(CreateConditions(), new double[] { double.NaN, 0.2, 0.3 }, new double[3]), Throws.TypeOf<IccTransformException>().With.Message.Contains("must be finite"));
        });
    }

    [Test]
    public void ExecutesAppearanceElementsAndCalculatorSubElementOperations()
    {
        IccProcessingElement forward = CreateElement(IccProcessingElementSignatures.XyzToJab, IccProcessingElementKind.XyzToJab);
        IccProcessingElement inverse = CreateElement(IccProcessingElementSignatures.JabToXyz, IccProcessingElementKind.JabToXyz);
        double[] input = new double[] { 0.2, 0.25, 0.3 };
        double[] jab = new double[3];
        double[] direct = new double[3];
        IccCamMath.ExecuteElement(forward, input, jab);
        IccCamMath.ExecuteElement(inverse, jab, direct);

        IccProcessingElement calculator = CreateCalculator(forward, inverse);
        double[] calculated = new double[3];
        CalculatorVirtualMachine.Execute(calculator, input, calculated, new IccResourceLimits(), new CalculatorEnvironment(Array.Empty<uint>(), Array.Empty<double>()));

        Assert.Multiple(() =>
        {
            Assert.That(direct, Is.EqualTo(input).Within(0.00001));
            Assert.That(calculated, Is.EqualTo(input).Within(0.00001));
        });
    }

    [Test]
    public void RejectsAppearanceElementKindSignatureAndChannelMismatches()
    {
        IccProcessingElement mismatch = CreateElement(IccProcessingElementSignatures.XyzToJab, IccProcessingElementKind.JabToXyz);
        IccProcessingElement wrongChannels = CreateElement(IccProcessingElementSignatures.XyzToJab, IccProcessingElementKind.XyzToJab);
        wrongChannels.OutputChannels = 2;

        Assert.Multiple(() =>
        {
            Assert.That(() => IccCamMath.ExecuteElement(mismatch, new double[3], new double[3]), Throws.TypeOf<IccTransformException>().With.Message.Contains("kind and signature"));
            Assert.That(() => IccCamMath.ExecuteElement(wrongChannels, new double[3], new double[3]), Throws.TypeOf<IccTransformException>().With.Message.Contains("three input channels"));
        });
    }

    private static void ConvertWhite(IccCamViewingConditions conditions)
    {
        IccCamMath.ConvertXyzToJab(conditions, new double[] { 0.9642, 1.0, 0.8249 }, new double[3]);
    }

    private static IccCamViewingConditions CreateConditions()
    {
        return new IccCamViewingConditions
        {
            WhitePoint = new IccXyzColor { X = 0.9642, Y = 1.0, Z = 0.8249 },
            Luminance = 64.0,
            BackgroundLuminance = 12.8,
            ImpactOfSurround = 0.69,
            ChromaticInductionFactor = 1.0,
            AdaptationFactor = 1.0
        };
    }

    private static IccProcessingElement CreateElement(uint signature, IccProcessingElementKind kind)
    {
        return new IccProcessingElement
        {
            Signature = signature,
            Kind = kind,
            InputChannels = 3,
            OutputChannels = 3,
            CamViewingConditions = CreateConditions()
        };
    }

    private static IccProcessingElement CreateCalculator(IccProcessingElement forward, IccProcessingElement inverse)
    {
        return new IccProcessingElement
        {
            Signature = IccProcessingElementSignatures.Calculator,
            Kind = IccProcessingElementKind.Calculator,
            InputChannels = 3,
            OutputChannels = 3,
            Calculator = new IccCalculatorProgram
            {
                SubElements = new IccProcessingElement[] { forward, inverse },
                Instructions = new IccCalculatorInstruction[]
                {
                    Instruction(CalculatorOperationSignatures.Input, 0x00000002),
                    Instruction(CalculatorOperationSignatures.ApplyToJab, 0x00000000),
                    Instruction(CalculatorOperationSignatures.ApplyFromJab, 0x00010000),
                    Instruction(CalculatorOperationSignatures.Output, 0x00000002)
                }
            }
        };
    }

    private static IccCalculatorInstruction Instruction(uint signature, uint operand)
    {
        return new IccCalculatorInstruction { Signature = signature, Operand = operand };
    }
}
