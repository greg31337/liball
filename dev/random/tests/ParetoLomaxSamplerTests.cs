namespace random.Tests;

public class ParetoLomaxSamplerTests
{
    const ulong ZeroOutputSeed = 0x61C8864680B583EBUL;
    const ulong OneOutputSeed = 0xF8364607E9C949BDUL;
    const ulong UInt64MaximumOutputSeed = 0x31628AF67B2131ABUL;

    [Test]
    public void StandardFormsMatchExplicitParameters()
    {
        RandomGenerator firstParetoGenerator = Create(42UL, RandomMathMode.Portable);
        RandomGenerator secondParetoGenerator = Create(42UL, RandomMathMode.Portable);
        RandomGenerator firstLomaxGenerator = Create(43UL, RandomMathMode.Portable);
        RandomGenerator secondLomaxGenerator = Create(43UL, RandomMathMode.Portable);
        RandomGenerator thirdLomaxGenerator = Create(43UL, RandomMathMode.Portable);
        ParetoSampler firstPareto = new ParetoSampler(firstParetoGenerator, 1.0, 1.7);
        ParetoSampler secondPareto = new ParetoSampler(secondParetoGenerator, 1.0, 1.7);
        LomaxSampler firstLomax = new LomaxSampler(firstLomaxGenerator, 0.0, 1.0, 1.7);
        LomaxSampler secondLomax = new LomaxSampler(secondLomaxGenerator, 0.0, 1.0, 1.7);
        LomaxSampler thirdLomax = new LomaxSampler(thirdLomaxGenerator, 0.0, 1.0, 1.7);

        for (int index = 0; index < 128; index++)
        {
            Assert.Multiple(() =>
            {
                Assert.That(PortableMath.DoubleBits(firstPareto.Sample()), Is.EqualTo(PortableMath.DoubleBits(secondPareto.Sample())));
                ulong expectedLomaxBits = PortableMath.DoubleBits(firstLomax.Sample());
                Assert.That(PortableMath.DoubleBits(secondLomax.Sample()), Is.EqualTo(expectedLomaxBits));
                Assert.That(PortableMath.DoubleBits(thirdLomax.Sample()), Is.EqualTo(expectedLomaxBits));
            });
        }

        Assert.Multiple(() =>
        {
            Assert.That(firstParetoGenerator.ExportState().ToBytes(), Is.EqualTo(secondParetoGenerator.ExportState().ToBytes()));
            Assert.That(firstLomaxGenerator.ExportState().ToBytes(), Is.EqualTo(secondLomaxGenerator.ExportState().ToBytes()));
            Assert.That(firstLomaxGenerator.ExportState().ToBytes(), Is.EqualTo(thirdLomaxGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void ParetoTypeIMatchesItsSurvivalTransformInBothMathModes()
    {
        double[] shapes = [0.25, 1.0, 3.0, double.MaxValue];
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (double shape in shapes)
        {
            foreach (RandomMathMode mode in modes)
            {
                RandomGenerator actualGenerator = Create(987UL, mode);
                RandomGenerator referenceGenerator = Create(987UL, mode);
                ParetoSampler sampler = new ParetoSampler(actualGenerator, 2.5, shape);

                for (int index = 0; index < 128; index++)
                {
                    double unit = referenceGenerator.NextDoubleOpenOpen();
                    double logarithm = mode == RandomMathMode.Portable ? PortableMath.Log(unit) : Math.Log(unit);
                    double excess = StableExpm1(-logarithm / shape, mode);
                    double expected = 2.5 + 2.5 * excess;
                    Assert.That(PortableMath.DoubleBits(sampler.Sample()), Is.EqualTo(PortableMath.DoubleBits(expected)));
                }

                Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
            }
        }
    }

    [Test]
    public void LomaxTypeIIMatchesItsSurvivalTransformInBothMathModes()
    {
        double[] shapes = [0.25, 1.0, 3.0, double.MaxValue];
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (double shape in shapes)
        {
            foreach (RandomMathMode mode in modes)
            {
                RandomGenerator actualGenerator = Create(654UL, mode);
                RandomGenerator referenceGenerator = Create(654UL, mode);
                LomaxSampler sampler = new LomaxSampler(actualGenerator, -3.0, 2.5, shape);

                for (int index = 0; index < 128; index++)
                {
                    double unit = referenceGenerator.NextDoubleOpenOpen();
                    double logarithm = mode == RandomMathMode.Portable ? PortableMath.Log(unit) : Math.Log(unit);
                    double excess = StableExpm1(-logarithm / shape, mode);
                    double expected = -3.0 + 2.5 * excess;
                    Assert.That(PortableMath.DoubleBits(sampler.Sample()), Is.EqualTo(PortableMath.DoubleBits(expected)));
                }

                Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
            }
        }
    }

    [Test]
    public void UnitMinimumParetoIsUnitScaleLomaxPlusOne()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            RandomGenerator paretoGenerator = Create(77UL, mode);
            RandomGenerator lomaxGenerator = Create(77UL, mode);
            ParetoSampler pareto = new ParetoSampler(paretoGenerator, 1.0, 2.25);
            LomaxSampler lomax = new LomaxSampler(lomaxGenerator, 0.0, 1.0, 2.25);

            for (int index = 0; index < 256; index++)
            {
                Assert.That(PortableMath.DoubleBits(pareto.Sample()), Is.EqualTo(PortableMath.DoubleBits(1.0 + lomax.Sample())));
            }

            Assert.That(paretoGenerator.ExportState().ToBytes(), Is.EqualTo(lomaxGenerator.ExportState().ToBytes()));
        }
    }

    [Test]
    public void FillMatchesRepeatedSamplesAndEmptyFillConsumesNothing()
    {
        RandomGenerator paretoFillGenerator = Create(123UL, RandomMathMode.Portable);
        RandomGenerator paretoScalarGenerator = Create(123UL, RandomMathMode.Portable);
        RandomGenerator lomaxFillGenerator = Create(124UL, RandomMathMode.Portable);
        RandomGenerator lomaxScalarGenerator = Create(124UL, RandomMathMode.Portable);
        ParetoSampler paretoFill = new ParetoSampler(paretoFillGenerator, 4.0, 1.8);
        ParetoSampler paretoScalar = new ParetoSampler(paretoScalarGenerator, 4.0, 1.8);
        LomaxSampler lomaxFill = new LomaxSampler(lomaxFillGenerator, 4.0, 3.0, 1.8);
        LomaxSampler lomaxScalar = new LomaxSampler(lomaxScalarGenerator, 4.0, 3.0, 1.8);

        AssertFill(values => paretoFill.Fill(values), () => paretoScalar.Sample(), paretoFillGenerator, paretoScalarGenerator);
        AssertFill(values => lomaxFill.Fill(values), () => lomaxScalar.Sample(), lomaxFillGenerator, lomaxScalarGenerator);
    }

    [Test]
    public void ZeroLomaxScaleReturnsExactLocationWithoutConsumption()
    {
        RandomGenerator generator = Create(71UL, RandomMathMode.Portable);
        byte[] before = generator.ExportState().ToBytes();
        LomaxSampler unshifted = new LomaxSampler(generator, 0.0, -0.0, 2.0);
        LomaxSampler shifted = new LomaxSampler(generator, -91.5, -0.0, 2.0);
        double[] values = new double[8];

        Assert.Multiple(() =>
        {
            Assert.That(PortableMath.DoubleBits(unshifted.Sample()), Is.EqualTo(0UL));
            Assert.That(shifted.Sample(), Is.EqualTo(-91.5));
        });
        shifted.Fill(values);
        Assert.Multiple(() =>
        {
            Assert.That(values, Is.All.EqualTo(-91.5));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void InvalidParametersAreRejectedWithoutConsumption()
    {
        Assert.That(Assert.Throws<RandomArgumentException>(() => new ParetoSampler(NullReference.Value<global::random.IRandomSource>(), 1.0, 1.0))?.ArgumentName(), Is.EqualTo("generator"));
        Assert.That(Assert.Throws<RandomArgumentException>(() => new ParetoSampler(NullReference.Value<global::random.IRandomSource>(), 1.0, 1.0))?.ArgumentName(), Is.EqualTo("generator"));
        Assert.That(Assert.Throws<RandomArgumentException>(() => new LomaxSampler(NullReference.Value<global::random.IRandomSource>(), 0.0, 1.0, 1.0))?.ArgumentName(), Is.EqualTo("generator"));
        Assert.That(Assert.Throws<RandomArgumentException>(() => new LomaxSampler(NullReference.Value<global::random.IRandomSource>(), 0.0, 1.0, 1.0))?.ArgumentName(), Is.EqualTo("generator"));
        Assert.That(Assert.Throws<RandomArgumentException>(() => new LomaxSampler(NullReference.Value<global::random.IRandomSource>(), 0.0, 1.0, 1.0))?.ArgumentName(), Is.EqualTo("generator"));

        RandomGenerator generator = Create(23UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        object[][] cases =
        [
            [(TestDelegate)(() => new ParetoSampler(generator, 0.0, 1.0)), "minimum"],
            [(TestDelegate)(() => new ParetoSampler(generator, double.NaN, 1.0)), "minimum"],
            [(TestDelegate)(() => new ParetoSampler(generator, 1.0, 0.0)), "shape"],
            [(TestDelegate)(() => new ParetoSampler(generator, 1.0, double.PositiveInfinity)), "shape"],
            [(TestDelegate)(() => new LomaxSampler(generator, double.NaN, 1.0, 1.0)), "location"],
            [(TestDelegate)(() => new LomaxSampler(generator, 0.0, double.PositiveInfinity, 1.0)), "scale"],
            [(TestDelegate)(() => new LomaxSampler(generator, 0.0, -double.Epsilon, 1.0)), "scale"],
            [(TestDelegate)(() => new LomaxSampler(generator, 0.0, 1.0, 0.0)), "shape"],
            [(TestDelegate)(() => new LomaxSampler(generator, 0.0, 1.0, double.NaN)), "shape"],
            [(TestDelegate)(() => new LomaxSampler(generator, 0.0, 1.0, -double.Epsilon)), "shape"]
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        }
    }

    [Test]
    public void StableLowerEndpointAndOpenTailRemainDefined()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            double paretoNearMinimum = new ParetoSampler(Create(UInt64MaximumOutputSeed, mode), 1.0, 1.0).Sample();
            double lomaxNearZero = new LomaxSampler(Create(UInt64MaximumOutputSeed, mode), 0.0, 1.0, 1.0).Sample();
            double paretoTail = new ParetoSampler(Create(OneOutputSeed, mode), 1.0, 1.0).Sample();
            double lomaxTail = new LomaxSampler(Create(OneOutputSeed, mode), 0.0, 1.0, 1.0).Sample();
            double paretoTinyShape = new ParetoSampler(Create(OneOutputSeed, mode), 1.0, double.Epsilon).Sample();
            double lomaxTinyShape = new LomaxSampler(Create(OneOutputSeed, mode), 0.0, 1.0, double.Epsilon).Sample();

            Assert.Multiple(() =>
            {
                Assert.That(paretoNearMinimum, Is.GreaterThanOrEqualTo(1.0));
                Assert.That(lomaxNearZero, Is.GreaterThan(0.0).And.LessThan(2.0e-16));
                Assert.That(paretoTail, Is.GreaterThan(9.0e15));
                Assert.That(lomaxTail, Is.GreaterThan(9.0e15));
                Assert.That(double.IsFinite(paretoTail), Is.True);
                Assert.That(double.IsFinite(lomaxTail), Is.True);
                Assert.That(paretoTinyShape, Is.EqualTo(double.PositiveInfinity));
                Assert.That(lomaxTinyShape, Is.EqualTo(double.PositiveInfinity));
            });
        }
    }

    [Test]
    public void ZeroRawNumeratorIsRejectedByBothTransforms()
    {
        RandomGenerator paretoGenerator = Create(ZeroOutputSeed, RandomMathMode.Portable);
        RandomGenerator paretoReference = Create(ZeroOutputSeed, RandomMathMode.Portable);
        RandomGenerator lomaxGenerator = Create(ZeroOutputSeed, RandomMathMode.Portable);
        RandomGenerator lomaxReference = Create(ZeroOutputSeed, RandomMathMode.Portable);
        double pareto = new ParetoSampler(paretoGenerator, 1.0, 2.0).Sample();
        double lomax = new LomaxSampler(lomaxGenerator, 0.0, 1.0, 2.0).Sample();
        double paretoUnit = paretoReference.NextDoubleOpenOpen();
        double lomaxUnit = lomaxReference.NextDoubleOpenOpen();

        Assert.Multiple(() =>
        {
            Assert.That(paretoUnit, Is.GreaterThan(0.0).And.LessThan(1.0));
            Assert.That(lomaxUnit, Is.GreaterThan(0.0).And.LessThan(1.0));
            Assert.That(double.IsFinite(pareto), Is.True);
            Assert.That(double.IsFinite(lomax), Is.True);
            Assert.That(paretoGenerator.ExportState().ToBytes(), Is.EqualTo(paretoReference.ExportState().ToBytes()));
            Assert.That(lomaxGenerator.ExportState().ToBytes(), Is.EqualTo(lomaxReference.ExportState().ToBytes()));
        });
    }

    [Test]
    public void LargeThresholdAndScaleOverflowExplicitly()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            double pareto = new ParetoSampler(Create(OneOutputSeed, mode), double.MaxValue, 2.0).Sample();
            double lomax = new LomaxSampler(Create(OneOutputSeed, mode), 0.0, double.MaxValue, 2.0).Sample();
            Assert.Multiple(() =>
            {
                Assert.That(pareto, Is.EqualTo(double.PositiveInfinity));
                Assert.That(lomax, Is.EqualTo(double.PositiveInfinity));
            });
        }
    }

    [Test]
    public void DeterministicMeansMatchSelectedParameterizations()
    {
        const int sampleCount = 150000;
        ParetoSampler pareto = new ParetoSampler(Create(101UL, RandomMathMode.Portable), 2.0, 3.0);
        LomaxSampler lomax = new LomaxSampler(Create(102UL, RandomMathMode.Portable), 0.0, 2.0, 3.0);
        double paretoSum = 0.0;
        double lomaxSum = 0.0;

        for (int index = 0; index < sampleCount; index++)
        {
            paretoSum += pareto.Sample();
            lomaxSum += lomax.Sample();
        }

        Assert.Multiple(() =>
        {
            Assert.That(paretoSum / sampleCount, Is.EqualTo(3.0).Within(0.05));
            Assert.That(lomaxSum / sampleCount, Is.EqualTo(1.0).Within(0.04));
        });
    }

    static double StableExpm1(double value, RandomMathMode mode)
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
        double[] actual = new double[129];
        double[] expected = new double[actual.Length];
        fill(actual);
        for (int index = 0; index < expected.Length; index++)
        {
            expected[index] = sample();
        }

        for (int index = 0; index < actual.Length; index++)
        {
            Assert.That(PortableMath.DoubleBits(actual[index]), Is.EqualTo(PortableMath.DoubleBits(expected[index])));
        }

        Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(scalarGenerator.ExportState().ToBytes()));
    }

    static RandomGenerator Create(ulong seed, RandomMathMode mode)
    {
        return RandomGenerator.CreateSplitMix64(seed, mode);
    }
}
