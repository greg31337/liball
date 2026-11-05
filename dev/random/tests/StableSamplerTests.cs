namespace random.Tests;

public class StableSamplerTests
{
    const double PiOver2 = 1.57079632679489661923;
    const double RootTwo = 1.41421356237309504880;

    [Test]
    public void StandardAndSymmetricConstructorsMatchExplicitS0Parameters()
    {
        RandomGenerator firstGenerator = Create(41UL, RandomMathMode.Portable);
        RandomGenerator secondGenerator = Create(41UL, RandomMathMode.Portable);
        RandomGenerator thirdGenerator = Create(42UL, RandomMathMode.Portable);
        RandomGenerator fourthGenerator = Create(42UL, RandomMathMode.Portable);
        StableSampler standard = new StableSampler(firstGenerator, 0.0, 1.0, 1.35, -0.4);
        StableSampler explicitStandard = new StableSampler(secondGenerator, 0.0, 1.0, 1.35, -0.4);
        StableSampler symmetric = new StableSampler(thirdGenerator, 2.5, 1.75, 0.8, 0.0);
        StableSampler explicitSymmetric = new StableSampler(fourthGenerator, 2.5, 1.75, 0.8, 0.0);

        for (int index = 0; index < 128; index++)
        {
            Assert.Multiple(() =>
            {
                Assert.That(Bits(standard.Sample()), Is.EqualTo(Bits(explicitStandard.Sample())));
                Assert.That(Bits(symmetric.Sample()), Is.EqualTo(Bits(explicitSymmetric.Sample())));
            });
        }

        Assert.Multiple(() =>
        {
            Assert.That(firstGenerator.ExportState().ToBytes(), Is.EqualTo(secondGenerator.ExportState().ToBytes()));
            Assert.That(thirdGenerator.ExportState().ToBytes(), Is.EqualTo(fourthGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void GenericSymmetricAndAsymmetricCasesMatchContinuousCmsFormula()
    {
        double[][] parameterCases =
        [
            [1.25, 2.5, 0.7, 0.0],
            [-3.0, 0.75, 0.7, 0.6],
            [2.0, 1.5, 1.35, -0.8],
            [-1.0, 0.5, 1.8, 0.25]
        ];
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];

        foreach (RandomMathMode mode in modes)
        {
            foreach (double[] parameters in parameterCases)
            {
                RandomGenerator actualGenerator = Create(51UL, mode);
                RandomGenerator referenceGenerator = Create(51UL, mode);
                StableSampler sampler = new StableSampler(actualGenerator, parameters[0], parameters[1], parameters[2], parameters[3]);

                for (int index = 0; index < 128; index++)
                {
                    double expected = ReferenceCms(
                        referenceGenerator,
                        parameters[0],
                        parameters[1],
                        parameters[2],
                        parameters[3],
                        mode);
                    Assert.That(Bits(sampler.Sample()), Is.EqualTo(Bits(expected)));
                }

                Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
            }
        }
    }

    [Test]
    public void GaussianSpecialCaseHasVarianceTwoAndIgnoresSkewness()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            RandomGenerator actualGenerator = Create(61UL, mode);
            RandomGenerator referenceGenerator = Create(61UL, mode);
            RandomGenerator oppositeGenerator = Create(61UL, mode);
            StableSampler sampler = new StableSampler(actualGenerator, -1.5, 2.25, 2.0, 1.0);
            NormalSampler normal = new NormalSampler(referenceGenerator, 0.0, 1.0);
            StableSampler opposite = new StableSampler(oppositeGenerator, -1.5, 2.25, 2.0, -1.0);

            for (int index = 0; index < 256; index++)
            {
                double expected = 2.25 * (RootTwo * normal.Sample()) - 1.5;
                double actual = sampler.Sample();
                Assert.Multiple(() =>
                {
                    Assert.That(Bits(actual), Is.EqualTo(Bits(expected)));
                    Assert.That(Bits(opposite.Sample()), Is.EqualTo(Bits(actual)));
                });
            }

            Assert.Multiple(() =>
            {
                Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
                Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(oppositeGenerator.ExportState().ToBytes()));
            });
        }
    }

    [Test]
    public void CauchySpecialCaseMatchesHalfAngleConstruction()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            RandomGenerator actualGenerator = Create(71UL, mode);
            RandomGenerator referenceGenerator = Create(71UL, mode);
            StableSampler sampler = new StableSampler(actualGenerator, 3.0, 1.25, 1.0, 0.0);

            for (int index = 0; index < 256; index++)
            {
                double halfAngle = PiOver2 * (referenceGenerator.NextDoubleOpenOpen() - 0.5);
                double tangent = Tan(halfAngle, mode);
                double expected = 1.25 * (2.0 * tangent / (1.0 - tangent * tangent)) + 3.0;
                Assert.That(Bits(sampler.Sample()), Is.EqualTo(Bits(expected)));
            }

            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        }
    }

    [Test]
    public void LevySpecialCasesMatchShiftedAndReflectedNormalConstruction()
    {
        double[] skewnessValues = [-1.0, 1.0];
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            foreach (double skewness in skewnessValues)
            {
                RandomGenerator actualGenerator = Create(81UL, mode);
                RandomGenerator referenceGenerator = Create(81UL, mode);
                StableSampler sampler = new StableSampler(actualGenerator, 4.0, 1.5, 0.5, skewness);
                NormalSampler normal = new NormalSampler(referenceGenerator, 0.0, 1.0);

                for (int index = 0; index < 256; index++)
                {
                    double normalValue = normal.Sample();
                    double square = normalValue * normalValue;
                    double standard = square == 0.0
                        ? (skewness > 0.0 ? double.PositiveInfinity : double.NegativeInfinity)
                        : skewness * (1.0 / square - 1.0);
                    double expected = 1.5 * standard + 4.0;
                    double actual = sampler.Sample();
                    Assert.That(Bits(actual), Is.EqualTo(Bits(expected)));
                    if (skewness > 0.0)
                    {
                        Assert.That(actual, Is.GreaterThanOrEqualTo(2.5));
                    }
                    else
                    {
                        Assert.That(actual, Is.LessThanOrEqualTo(5.5));
                    }
                }

                Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
            }
        }
    }

    [Test]
    public void S0ConstructionIsSampleContinuousThroughStabilityOne()
    {
        double below = Math.BitDecrement(1.0);
        double above = Math.BitIncrement(1.0);
        StableSampler lower = new StableSampler(Create(91UL, RandomMathMode.System), 0.0, 1.0, below, 0.7);
        StableSampler exact = new StableSampler(Create(91UL, RandomMathMode.System), 0.0, 1.0, 1.0, 0.7);
        StableSampler upper = new StableSampler(Create(91UL, RandomMathMode.System), 0.0, 1.0, above, 0.7);

        for (int index = 0; index < 256; index++)
        {
            double center = exact.Sample();
            double tolerance = 2.0e-12 * Math.Max(1.0, Math.Abs(center));
            Assert.Multiple(() =>
            {
                Assert.That(lower.Sample(), Is.EqualTo(center).Within(tolerance));
                Assert.That(upper.Sample(), Is.EqualTo(center).Within(tolerance));
            });
        }
    }

    [Test]
    public void S0ConstructionIsSampleContinuousThroughZeroSkewness()
    {
        StableSampler negative = new StableSampler(Create(101UL, RandomMathMode.System), 0.0, 1.0, 0.75, -1.0e-12);
        StableSampler symmetric = new StableSampler(Create(101UL, RandomMathMode.System), 0.0, 1.0, 0.75, 0.0);
        StableSampler positive = new StableSampler(Create(101UL, RandomMathMode.System), 0.0, 1.0, 0.75, 1.0e-12);

        for (int index = 0; index < 256; index++)
        {
            double center = symmetric.Sample();
            double tolerance = 2.0e-9 * Math.Max(1.0, Math.Abs(center));
            Assert.Multiple(() =>
            {
                Assert.That(negative.Sample(), Is.EqualTo(center).Within(tolerance));
                Assert.That(positive.Sample(), Is.EqualTo(center).Within(tolerance));
            });
        }
    }

    [Test]
    public void SmallStabilityFallbackNeverReturnsNaNAndRespectsOneSidedSupport()
    {
        double[] skewnessValues = [-1.0, -0.4, 0.0, 0.4, 1.0];
        foreach (double skewness in skewnessValues)
        {
            StableSampler sampler = new StableSampler(Create(111UL, RandomMathMode.Portable), -2.0, 3.0, 0.001, skewness);
            double tangent = PortableMath.Tan(0.001 * PiOver2);
            double lower = -2.0 - 3.0 * tangent;
            double upper = -2.0 + 3.0 * tangent;
            for (int index = 0; index < 1024; index++)
            {
                double value = sampler.Sample();
                Assert.That(double.IsNaN(value), Is.False);
                if (skewness == 1.0)
                {
                    Assert.That(value, Is.GreaterThanOrEqualTo(lower));
                }
                else if (skewness == -1.0)
                {
                    Assert.That(value, Is.LessThanOrEqualTo(upper));
                }
            }
        }
    }

    [Test]
    public void FillMatchesRepeatedSamplesAcrossAlgorithmPaths()
    {
        double[][] parameterCases =
        [
            [0.0, 1.0, 2.0, 0.8],
            [0.0, 1.0, 1.0, 0.0],
            [0.0, 1.0, 0.5, -1.0],
            [2.0, 3.0, 1.0, 0.4],
            [2.0, 3.0, 0.001, 0.4],
            [2.0, 3.0, 1.4, 0.0],
            [2.0, 3.0, 1.4, 0.4]
        ];

        foreach (double[] parameters in parameterCases)
        {
            RandomGenerator fillGenerator = Create(121UL, RandomMathMode.Portable);
            RandomGenerator scalarGenerator = Create(121UL, RandomMathMode.Portable);
            StableSampler fillSampler = new StableSampler(fillGenerator, parameters[0], parameters[1], parameters[2], parameters[3]);
            StableSampler scalarSampler = new StableSampler(scalarGenerator, parameters[0], parameters[1], parameters[2], parameters[3]);
            double[] actual = new double[129];
            double[] expected = new double[actual.Length];
            byte[] before = fillGenerator.ExportState().ToBytes();

            fillSampler.Fill(Span<double>.Empty);
            Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(before));
            fillSampler.Fill(actual);
            for (int index = 0; index < expected.Length; index++)
            {
                expected[index] = scalarSampler.Sample();
            }

            for (int index = 0; index < actual.Length; index++)
            {
                Assert.That(Bits(actual[index]), Is.EqualTo(Bits(expected[index])));
            }

            Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(scalarGenerator.ExportState().ToBytes()));
        }
    }

    [Test]
    public void ZeroScaleReturnsExactLocationWithoutConsumption()
    {
        double[] stabilityValues = [0.001, 0.5, 1.0, 1.5, 2.0];
        foreach (double stability in stabilityValues)
        {
            RandomGenerator generator = Create(131UL, RandomMathMode.Portable);
            generator.NextBoolean();
            byte[] before = generator.ExportState().ToBytes();
            StableSampler sampler = new StableSampler(generator, -4.5, -0.0, stability, 0.75);
            double[] values = new double[16];

            Assert.That(sampler.Sample(), Is.EqualTo(-4.5));
            sampler.Fill(values);
            Assert.Multiple(() =>
            {
                Assert.That(values, Is.All.EqualTo(-4.5));
                Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
            });
        }
    }

    [Test]
    public void InvalidParametersAreRejectedWithoutConsumption()
    {
        Assert.That(Assert.Throws<RandomArgumentException>(() => new StableSampler(NullReference.Value<global::random.IRandomSource>(), 0.0, 1.0, 1.0, 0.0))?.ArgumentName(), Is.EqualTo("generator"));

        RandomGenerator generator = Create(141UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        object[][] cases =
        [
            [(TestDelegate)(() => new StableSampler(generator, double.NaN, 1.0, 1.0, 0.0)), "location"],
            [(TestDelegate)(() => new StableSampler(generator, 0.0, double.PositiveInfinity, 1.0, 0.0)), "scale"],
            [(TestDelegate)(() => new StableSampler(generator, 0.0, -double.Epsilon, 1.0, 0.0)), "scale"],
            [(TestDelegate)(() => new StableSampler(generator, 0.0, 1.0, 0.0, 0.0)), "stability"],
            [(TestDelegate)(() => new StableSampler(generator, 0.0, 1.0, double.Epsilon, 0.0)), "stability"],
            [(TestDelegate)(() => new StableSampler(generator, 0.0, 1.0, 2.0000000000000004, 0.0)), "stability"],
            [(TestDelegate)(() => new StableSampler(generator, 0.0, 1.0, double.NaN, 0.0)), "stability"],
            [(TestDelegate)(() => new StableSampler(generator, 0.0, 1.0, 1.0, -1.0000000000000002)), "skewness"],
            [(TestDelegate)(() => new StableSampler(generator, 0.0, 1.0, 1.0, double.NaN)), "skewness"]
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        }
    }

    static double ReferenceCms(
        RandomGenerator generator,
        double location,
        double scale,
        double stability,
        double skewness,
        RandomMathMode mode)
    {
        double epsilon = 1.0 - stability;
        double adjustedStability = 1.0 - epsilon;
        double inverseStability = 1.0 / adjustedStability;
        double epsilonOverStability = inverseStability - 1.0;
        double tau = ComputeTau(adjustedStability, epsilon, skewness, mode);
        double halfAngle = PiOver2 * (generator.NextDoubleOpenOpen() - 0.5);
        double omega = -Log(generator.NextDoubleOpenOpen(), mode);
        double a = Tan(halfAngle, mode);
        double b = epsilon * halfAngle * TanRatio(epsilon * halfAngle, mode);
        double aSquared = a * a;
        double bSquared = b * b;
        double oneMinusA = 1.0 - aSquared;
        double onePlusA = 1.0 + aSquared;
        double oneMinusB = 1.0 - bSquared;
        double onePlusB = 1.0 + bSquared;
        double coefficient;
        double factor;
        if (tau == 0.0)
        {
            coefficient = onePlusA * oneMinusB / (omega * oneMinusA * onePlusB);
            factor = 2.0 * (a - b) * (1.0 + a * b) / (oneMinusA * onePlusB);
        }
        else
        {
            double tangentRatio = TanRatio(epsilon * halfAngle, mode);
            coefficient = onePlusA * (oneMinusB + 2.0 * halfAngle * tangentRatio * tau) /
                (omega * oneMinusA * onePlusB);
            factor = 2.0 * ((a - b) * (1.0 + a * b) -
                halfAngle * tau * tangentRatio * (b * oneMinusA - 2.0 * a)) /
                (oneMinusA * onePlusB);
        }

        double logarithm = Log(coefficient, mode);
        double exponentialTerm = ExponentialRatio(epsilonOverStability * logarithm, mode) *
            (logarithm * inverseStability);
        double standard = (1.0 + epsilon * exponentialTerm) * factor + tau * exponentialTerm;
        return scale * standard + location;
    }

    static double ComputeTau(double stability, double epsilon, double skewness, RandomMathMode mode)
    {
        if (Math.Abs(epsilon) < 0.5)
        {
            return skewness / (TanRatio(epsilon * PiOver2, mode) * PiOver2);
        }

        if (stability > 1.0)
        {
            double reflected = 2.0 - stability;
            return skewness * PiOver2 * epsilon * reflected * -TanRatio(reflected * PiOver2, mode);
        }

        return skewness * PiOver2 * epsilon * stability * TanRatio(stability * PiOver2, mode);
    }

    static double TanRatio(double value, RandomMathMode mode)
    {
        return value == 0.0 ? 1.0 : Tan(value, mode) / value;
    }

    static double ExponentialRatio(double value, RandomMathMode mode)
    {
        if (Math.Abs(value) < 0.5)
        {
            return value == 0.0 ? 1.0 : Expm1(value, mode) / value;
        }

        return value < double.PositiveInfinity ? (Exp(value, mode) - 1.0) / value : value;
    }

    static double Log(double value, RandomMathMode mode)
    {
        return mode == RandomMathMode.Portable ? PortableMath.Log(value) : Math.Log(value);
    }

    static double Exp(double value, RandomMathMode mode)
    {
        return mode == RandomMathMode.Portable ? PortableMath.Exp(value) : Math.Exp(value);
    }

    static double Expm1(double value, RandomMathMode mode)
    {
        if (mode == RandomMathMode.Portable)
        {
            return PortableMath.Expm1(value);
        }

        double half = value * 0.5;
        return 2.0 * Math.Exp(half) * Math.Sinh(half);
    }

    static double Tan(double value, RandomMathMode mode)
    {
        return mode == RandomMathMode.Portable ? PortableMath.Tan(value) : Math.Tan(value);
    }

    static ulong Bits(double value)
    {
        return PortableMath.DoubleBits(value);
    }

    static RandomGenerator Create(ulong seed, RandomMathMode mode)
    {
        return RandomGenerator.CreateSplitMix64(seed, mode);
    }
}
