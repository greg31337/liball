namespace random.Tests;

public class AdvancedContinuousSamplerTests
{
    const ulong OneOutputSeed = 0xF8364607E9C949BDUL;
    const ulong UInt64MaximumOutputSeed = 0x31628AF67B2131ABUL;

    [Test]
    public void StandardFormsMatchExplicitParameters()
    {
        RandomGenerator[] firstGenerators = CreateMany(5, 42UL);
        RandomGenerator[] secondGenerators = CreateMany(5, 42UL);
        PowerSampler firstPower = new PowerSampler(firstGenerators[0], 0.0, 1.0, 1.7);
        PowerSampler secondPower = new PowerSampler(secondGenerators[0], 0.0, 1.0, 1.7);
        BurrSampler firstBurr = new BurrSampler(firstGenerators[1], 0.0, 1.0, 1.7, 2.3);
        BurrSampler secondBurr = new BurrSampler(secondGenerators[1], 0.0, 1.0, 1.7, 2.3);
        LevySampler firstLevy = new LevySampler(firstGenerators[2], 0.0, 1.0);
        LevySampler secondLevy = new LevySampler(secondGenerators[2], 0.0, 1.0);
        InverseGammaSampler firstInverseGamma = new InverseGammaSampler(firstGenerators[3], 2.5, 1.0);
        InverseGammaSampler secondInverseGamma = new InverseGammaSampler(secondGenerators[3], 2.5, 1.0);
        InverseGaussianSampler firstInverseGaussian = new InverseGaussianSampler(firstGenerators[4], 1.0, 1.0);
        InverseGaussianSampler secondInverseGaussian = new InverseGaussianSampler(secondGenerators[4], 1.0, 1.0);

        for (int index = 0; index < 64; index++)
        {
            Assert.Multiple(() =>
            {
                Assert.That(Bits(firstPower.Sample()), Is.EqualTo(Bits(secondPower.Sample())));
                Assert.That(Bits(firstBurr.Sample()), Is.EqualTo(Bits(secondBurr.Sample())));
                Assert.That(Bits(firstLevy.Sample()), Is.EqualTo(Bits(secondLevy.Sample())));
                Assert.That(Bits(firstInverseGamma.Sample()), Is.EqualTo(Bits(secondInverseGamma.Sample())));
                Assert.That(Bits(firstInverseGaussian.Sample()), Is.EqualTo(Bits(secondInverseGaussian.Sample())));
            });
        }

        for (int index = 0; index < firstGenerators.Length; index++)
        {
            Assert.That(firstGenerators[index].ExportState().ToBytes(), Is.EqualTo(secondGenerators[index].ExportState().ToBytes()));
        }
    }

    [Test]
    public void PowerAndBurrMatchLogDomainTransformsInBothMathModes()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            RandomGenerator powerGenerator = Create(71UL, mode);
            RandomGenerator powerReference = Create(71UL, mode);
            RandomGenerator burrGenerator = Create(72UL, mode);
            RandomGenerator burrReference = Create(72UL, mode);
            PowerSampler power = new PowerSampler(powerGenerator, -2.0, 3.0, 0.7);
            BurrSampler burr = new BurrSampler(burrGenerator, -2.0, 3.0, 1.4, 2.7);

            for (int index = 0; index < 128; index++)
            {
                double powerUnit = powerReference.NextDoubleOpenOpen();
                double powerLog = Log(powerUnit, mode) / 0.7;
                double expectedPower = -2.0 + 3.0 * Exp(powerLog, mode);

                double burrUnit = burrReference.NextDoubleOpenOpen();
                double outer = -Log(burrUnit, mode) / 2.7;
                double excess = Expm1(outer, mode);
                double standard = Exp(Log(excess, mode) / 1.4, mode);
                double expectedBurr = -2.0 + 3.0 * standard;

                Assert.Multiple(() =>
                {
                    Assert.That(Bits(power.Sample()), Is.EqualTo(Bits(expectedPower)));
                    Assert.That(Bits(burr.Sample()), Is.EqualTo(Bits(expectedBurr)));
                });
            }

            Assert.Multiple(() =>
            {
                Assert.That(powerGenerator.ExportState().ToBytes(), Is.EqualTo(powerReference.ExportState().ToBytes()));
                Assert.That(burrGenerator.ExportState().ToBytes(), Is.EqualTo(burrReference.ExportState().ToBytes()));
            });
        }
    }

    [Test]
    public void LevyAndInverseGammaMatchTheirCompositionsInBothMathModes()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            RandomGenerator levyGenerator = Create(81UL, mode);
            RandomGenerator levyReference = Create(81UL, mode);
            RandomGenerator gammaGenerator = Create(82UL, mode);
            RandomGenerator gammaReference = Create(82UL, mode);
            LevySampler levy = new LevySampler(levyGenerator, -1.5, 2.25);
            NormalSampler normal = new NormalSampler(levyReference, 0.0, 1.0);
            InverseGammaSampler inverseGamma = new InverseGammaSampler(gammaGenerator, 2.75, 1.5);
            GammaSampler gamma = new GammaSampler(gammaReference, 2.75, 1.0);

            for (int index = 0; index < 128; index++)
            {
                double normalValue = normal.Sample();
                double square = normalValue * normalValue;
                double expectedLevy = square == 0.0 ? double.PositiveInfinity : -1.5 + 2.25 / square;
                double gammaValue = gamma.Sample();
                double expectedInverseGamma = gammaValue == 0.0 ? double.PositiveInfinity : 1.5 / gammaValue;

                Assert.Multiple(() =>
                {
                    Assert.That(Bits(levy.Sample()), Is.EqualTo(Bits(expectedLevy)));
                    Assert.That(Bits(inverseGamma.Sample()), Is.EqualTo(Bits(expectedInverseGamma)));
                });
            }

            Assert.Multiple(() =>
            {
                Assert.That(levyGenerator.ExportState().ToBytes(), Is.EqualTo(levyReference.ExportState().ToBytes()));
                Assert.That(gammaGenerator.ExportState().ToBytes(), Is.EqualTo(gammaReference.ExportState().ToBytes()));
            });
        }
    }

    [Test]
    public void InverseGaussianMatchesStableMichaelSchucanyHaasConstruction()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            RandomGenerator actualGenerator = Create(91UL, mode);
            RandomGenerator referenceGenerator = Create(91UL, mode);
            InverseGaussianSampler actual = new InverseGaussianSampler(actualGenerator, 2.5, 1.75);
            NormalSampler normal = new NormalSampler(referenceGenerator, 0.0, 1.0);

            for (int index = 0; index < 256; index++)
            {
                double normalValue = normal.Sample();
                double squared = normalValue * normalValue;
                double expected;
                if (squared == 0.0)
                {
                    expected = 2.5;
                }
                else
                {
                    double rootParameter = 2.5 / 1.75 * (0.5 * squared);
                    double baseValue;
                    if (rootParameter > 1.0)
                    {
                        double inverseRootParameter = 1.0 / rootParameter;
                        double rootRatio = Sqrt(1.0 + 2.0 * inverseRootParameter, mode);
                        baseValue = inverseRootParameter / (inverseRootParameter + 1.0 + rootRatio);
                    }
                    else
                    {
                        double root = Sqrt(rootParameter, mode) * Sqrt(rootParameter + 2.0, mode);
                        baseValue = 1.0 / (1.0 + rootParameter + root);
                    }

                    double primary = 2.5 * baseValue;
                    double probability = 1.0 / (1.0 + baseValue);
                    expected = referenceGenerator.NextDoubleOpenOpen() <= probability ? primary : 2.5 / baseValue;
                }

                Assert.That(Bits(actual.Sample()), Is.EqualTo(Bits(expected)));
            }

            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        }
    }

    [Test]
    public void BurrWithFirstShapeOneMatchesLomax()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            RandomGenerator burrGenerator = Create(111UL, mode);
            RandomGenerator lomaxGenerator = Create(111UL, mode);
            BurrSampler burr = new BurrSampler(burrGenerator, 0.0, 1.0, 1.0, 2.5);
            LomaxSampler lomax = new LomaxSampler(lomaxGenerator, 0.0, 1.0, 2.5);

            for (int index = 0; index < 128; index++)
            {
                Assert.That(Bits(burr.Sample()), Is.EqualTo(Bits(lomax.Sample())));
            }

            Assert.That(burrGenerator.ExportState().ToBytes(), Is.EqualTo(lomaxGenerator.ExportState().ToBytes()));
        }
    }

    [Test]
    public void FillMatchesRepeatedSamplesAndEmptyFillConsumesNothing()
    {
        RandomGenerator[] fillGenerators = CreateMany(5, 123UL);
        RandomGenerator[] scalarGenerators = CreateMany(5, 123UL);
        PowerSampler powerFill = new PowerSampler(fillGenerators[0], 2.0, 3.0, 0.8);
        PowerSampler powerScalar = new PowerSampler(scalarGenerators[0], 2.0, 3.0, 0.8);
        BurrSampler burrFill = new BurrSampler(fillGenerators[1], 2.0, 3.0, 0.8, 2.5);
        BurrSampler burrScalar = new BurrSampler(scalarGenerators[1], 2.0, 3.0, 0.8, 2.5);
        LevySampler levyFill = new LevySampler(fillGenerators[2], 2.0, 3.0);
        LevySampler levyScalar = new LevySampler(scalarGenerators[2], 2.0, 3.0);
        InverseGammaSampler gammaFill = new InverseGammaSampler(fillGenerators[3], 2.5, 3.0);
        InverseGammaSampler gammaScalar = new InverseGammaSampler(scalarGenerators[3], 2.5, 3.0);
        InverseGaussianSampler gaussianFill = new InverseGaussianSampler(fillGenerators[4], 2.0, 3.0);
        InverseGaussianSampler gaussianScalar = new InverseGaussianSampler(scalarGenerators[4], 2.0, 3.0);

        AssertFill(values => powerFill.Fill(values), () => powerScalar.Sample(), fillGenerators[0], scalarGenerators[0]);
        AssertFill(values => burrFill.Fill(values), () => burrScalar.Sample(), fillGenerators[1], scalarGenerators[1]);
        AssertFill(values => levyFill.Fill(values), () => levyScalar.Sample(), fillGenerators[2], scalarGenerators[2]);
        AssertFill(values => gammaFill.Fill(values), () => gammaScalar.Sample(), fillGenerators[3], scalarGenerators[3]);
        AssertFill(values => gaussianFill.Fill(values), () => gaussianScalar.Sample(), fillGenerators[4], scalarGenerators[4]);
    }

    [Test]
    public void ZeroScalesReturnExactPointMassesWithoutConsumption()
    {
        RandomGenerator generator = Create(131UL, RandomMathMode.Portable);
        byte[] before = generator.ExportState().ToBytes();
        PowerSampler power = new PowerSampler(generator, -4.5, -0.0, 2.0);
        BurrSampler burr = new BurrSampler(generator, -4.5, -0.0, 2.0, 3.0);
        LevySampler levy = new LevySampler(generator, -4.5, -0.0);
        InverseGammaSampler inverseGamma = new InverseGammaSampler(generator, 2.0, -0.0);
        double[] values = new double[8];

        Assert.Multiple(() =>
        {
            Assert.That(power.Sample(), Is.EqualTo(-4.5));
            Assert.That(burr.Sample(), Is.EqualTo(-4.5));
            Assert.That(levy.Sample(), Is.EqualTo(-4.5));
            Assert.That(Bits(inverseGamma.Sample()), Is.EqualTo(0UL));
        });
        power.Fill(values);
        Assert.That(values, Is.All.EqualTo(-4.5));
        burr.Fill(values);
        Assert.That(values, Is.All.EqualTo(-4.5));
        levy.Fill(values);
        Assert.That(values, Is.All.EqualTo(-4.5));
        inverseGamma.Fill(values);
        Assert.Multiple(() =>
        {
            Assert.That(values, Is.All.EqualTo(0.0));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void InvalidParametersAreRejectedWithoutConsumption()
    {
        Assert.That(Assert.Throws<RandomArgumentException>(() => new PowerSampler(NullReference.Value<global::random.IRandomSource>(), 0.0, 1.0, 1.0))?.ArgumentName(), Is.EqualTo("generator"));
        Assert.That(Assert.Throws<RandomArgumentException>(() => new BurrSampler(NullReference.Value<global::random.IRandomSource>(), 0.0, 1.0, 1.0, 1.0))?.ArgumentName(), Is.EqualTo("generator"));
        Assert.That(Assert.Throws<RandomArgumentException>(() => new LevySampler(NullReference.Value<global::random.IRandomSource>(), 0.0, 1.0))?.ArgumentName(), Is.EqualTo("generator"));
        Assert.That(Assert.Throws<RandomArgumentException>(() => new InverseGammaSampler(NullReference.Value<global::random.IRandomSource>(), 1.0, 1.0))?.ArgumentName(), Is.EqualTo("generator"));
        Assert.That(Assert.Throws<RandomArgumentException>(() => new InverseGaussianSampler(NullReference.Value<global::random.IRandomSource>(), 1.0, 1.0))?.ArgumentName(), Is.EqualTo("generator"));

        RandomGenerator generator = Create(23UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        object[][] cases =
        [
            [(TestDelegate)(() => new PowerSampler(generator, 0.0, 1.0, 0.0)), "shape"],
            [(TestDelegate)(() => new PowerSampler(generator, 0.0, -double.Epsilon, 1.0)), "scale"],
            [(TestDelegate)(() => new BurrSampler(generator, 0.0, -double.Epsilon, 1.0, 1.0)), "scale"],
            [(TestDelegate)(() => new BurrSampler(generator, 0.0, 1.0, 0.0, 1.0)), "firstShape"],
            [(TestDelegate)(() => new BurrSampler(generator, 0.0, 1.0, 1.0, double.NaN)), "secondShape"],
            [(TestDelegate)(() => new BurrSampler(generator, 0.0, 1.0, 1.0, 0.0)), "secondShape"],
            [(TestDelegate)(() => new LevySampler(generator, double.NaN, 1.0)), "location"],
            [(TestDelegate)(() => new LevySampler(generator, 0.0, -double.Epsilon)), "scale"],
            [(TestDelegate)(() => new InverseGammaSampler(generator, 0.0, 1.0)), "shape"],
            [(TestDelegate)(() => new InverseGammaSampler(generator, 1.0, -double.Epsilon)), "scale"],
            [(TestDelegate)(() => new InverseGammaSampler(generator, 1.0, double.PositiveInfinity)), "scale"],
            [(TestDelegate)(() => new InverseGaussianSampler(generator, 0.0, 1.0)), "mean"],
            [(TestDelegate)(() => new InverseGaussianSampler(generator, double.NaN, 1.0)), "mean"],
            [(TestDelegate)(() => new InverseGaussianSampler(generator, 1.0, -double.Epsilon)), "shape"],
            [(TestDelegate)(() => new InverseGaussianSampler(generator, 1.0, double.PositiveInfinity)), "shape"]
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        }
    }

    [Test]
    public void NumericBoundariesDoNotProduceNaN()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            PowerSampler tinyPower = new PowerSampler(Create(OneOutputSeed, mode), 0.0, 1.0, double.Epsilon);
            PowerSampler largePower = new PowerSampler(Create(UInt64MaximumOutputSeed, mode), 0.0, 1.0, double.MaxValue);
            BurrSampler heavyBurr = new BurrSampler(Create(OneOutputSeed, mode), 0.0, 1.0, 1.0, double.Epsilon);
            BurrSampler concentratedBurr = new BurrSampler(Create(UInt64MaximumOutputSeed, mode), 0.0, 1.0, double.MaxValue, double.MaxValue);
            InverseGammaSampler inverseGamma = new InverseGammaSampler(Create(141UL, mode), double.Epsilon, double.MaxValue);
            InverseGaussianSampler lowerWald = new InverseGaussianSampler(Create(142UL, mode), double.MaxValue, double.Epsilon);
            InverseGaussianSampler finiteRootWald = new InverseGaussianSampler(Create(142UL, mode), double.MaxValue, 1.0);
            InverseGaussianSampler concentratedWald = new InverseGaussianSampler(Create(143UL, mode), double.Epsilon, double.MaxValue);

            Assert.Multiple(() =>
            {
                Assert.That(tinyPower.Sample(), Is.EqualTo(0.0));
                Assert.That(largePower.Sample(), Is.EqualTo(1.0));
                Assert.That(heavyBurr.Sample(), Is.EqualTo(double.PositiveInfinity));
                Assert.That(double.IsNaN(concentratedBurr.Sample()), Is.False);
                Assert.That(finiteRootWald.Sample(), Is.GreaterThan(0.0));
            });

            for (int index = 0; index < 64; index++)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(double.IsNaN(inverseGamma.Sample()), Is.False);
                    Assert.That(double.IsNaN(lowerWald.Sample()), Is.False);
                    Assert.That(double.IsNaN(concentratedWald.Sample()), Is.False);
                });
            }
        }
    }

    [Test]
    public void DeterministicMeansMatchFiniteMomentParameterizations()
    {
        const int sampleCount = 120000;
        PowerSampler power = new PowerSampler(Create(151UL, RandomMathMode.Portable), 0.0, 1.0, 3.0);
        InverseGammaSampler inverseGamma = new InverseGammaSampler(Create(152UL, RandomMathMode.Portable), 5.0, 2.0);
        InverseGaussianSampler inverseGaussian = new InverseGaussianSampler(Create(153UL, RandomMathMode.Portable), 2.0, 5.0);
        double powerSum = 0.0;
        double inverseGammaSum = 0.0;
        double inverseGaussianSum = 0.0;

        for (int index = 0; index < sampleCount; index++)
        {
            powerSum += power.Sample();
            inverseGammaSum += inverseGamma.Sample();
            inverseGaussianSum += inverseGaussian.Sample();
        }

        Assert.Multiple(() =>
        {
            Assert.That(powerSum / sampleCount, Is.EqualTo(0.75).Within(0.01));
            Assert.That(inverseGammaSum / sampleCount, Is.EqualTo(0.5).Within(0.015));
            Assert.That(inverseGaussianSum / sampleCount, Is.EqualTo(2.0).Within(0.025));
        });
    }

    static ulong Bits(double value)
    {
        return PortableMath.DoubleBits(value);
    }

    static double Log(double value, RandomMathMode mode)
    {
        return mode == RandomMathMode.Portable ? PortableMath.Log(value) : Math.Log(value);
    }

    static double Exp(double value, RandomMathMode mode)
    {
        return mode == RandomMathMode.Portable ? PortableMath.Exp(value) : Math.Exp(value);
    }

    static double Sqrt(double value, RandomMathMode mode)
    {
        return mode == RandomMathMode.Portable ? PortableMath.Sqrt(value) : Math.Sqrt(value);
    }

    static double Expm1(double value, RandomMathMode mode)
    {
        if (mode == RandomMathMode.Portable)
        {
            return PortableMath.Expm1(value);
        }

        if (value <= 0.5)
        {
            double half = value * 0.5;
            return 2.0 * Math.Exp(half) * Math.Sinh(half);
        }

        return Math.Exp(value) - 1.0;
    }

    static void AssertFill(Action<double[]> fill, Func<double> sample, RandomGenerator fillGenerator, RandomGenerator scalarGenerator)
    {
        byte[] before = fillGenerator.ExportState().ToBytes();
        fill(Array.Empty<double>());
        Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(before));
        double[] actual = new double[97];
        double[] expected = new double[actual.Length];
        fill(actual);
        for (int index = 0; index < expected.Length; index++)
        {
            expected[index] = sample();
        }

        for (int index = 0; index < actual.Length; index++)
        {
            Assert.That(Bits(actual[index]), Is.EqualTo(Bits(expected[index])));
        }

        Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(scalarGenerator.ExportState().ToBytes()));
    }

    static RandomGenerator[] CreateMany(int count, ulong seed)
    {
        RandomGenerator[] generators = new RandomGenerator[count];
        for (int index = 0; index < generators.Length; index++)
        {
            generators[index] = Create(seed, RandomMathMode.Portable);
        }

        return generators;
    }

    static RandomGenerator Create(ulong seed, RandomMathMode mode)
    {
        return RandomGenerator.CreateSplitMix64(seed, mode);
    }
}
