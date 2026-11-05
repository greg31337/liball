namespace random.Tests;

public class WeibullRayleighSamplerTests
{
    const ulong ZeroOutputSeed = 0x61C8864680B583EBUL;
    const ulong OneOutputSeed = 0xF8364607E9C949BDUL;
    const ulong UInt64MaximumOutputSeed = 0x31628AF67B2131ABUL;

    [Test]
    public void StandardFormsMatchExplicitLocationAndScale()
    {
        RandomGenerator firstWeibullGenerator = Create(42UL, RandomMathMode.Portable);
        RandomGenerator secondWeibullGenerator = Create(42UL, RandomMathMode.Portable);
        RandomGenerator firstRayleighGenerator = Create(43UL, RandomMathMode.Portable);
        RandomGenerator secondRayleighGenerator = Create(43UL, RandomMathMode.Portable);
        WeibullSampler firstWeibull = new WeibullSampler(firstWeibullGenerator, 0.0, 1.0, 1.7);
        WeibullSampler secondWeibull = new WeibullSampler(secondWeibullGenerator, 0.0, 1.0, 1.7);
        RayleighSampler firstRayleigh = new RayleighSampler(firstRayleighGenerator, 0.0, 1.0);
        RayleighSampler secondRayleigh = new RayleighSampler(secondRayleighGenerator, 0.0, 1.0);

        for (int index = 0; index < 128; index++)
        {
            Assert.Multiple(() =>
            {
                Assert.That(PortableMath.DoubleBits(firstWeibull.Sample()), Is.EqualTo(PortableMath.DoubleBits(secondWeibull.Sample())));
                Assert.That(PortableMath.DoubleBits(firstRayleigh.Sample()), Is.EqualTo(PortableMath.DoubleBits(secondRayleigh.Sample())));
            });
        }

        Assert.Multiple(() =>
        {
            Assert.That(firstWeibullGenerator.ExportState().ToBytes(), Is.EqualTo(secondWeibullGenerator.ExportState().ToBytes()));
            Assert.That(firstRayleighGenerator.ExportState().ToBytes(), Is.EqualTo(secondRayleighGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void WeibullMatchesStableSurvivalTransformInBothMathModes()
    {
        double[] shapes = [0.25, 1.0, 2.0, double.MaxValue];
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (double shape in shapes)
        {
            foreach (RandomMathMode mode in modes)
            {
                RandomGenerator actualGenerator = Create(987UL, mode);
                RandomGenerator referenceGenerator = Create(987UL, mode);
                WeibullSampler sampler = new WeibullSampler(actualGenerator, -3.0, 2.5, shape);

                for (int index = 0; index < 128; index++)
                {
                    double unit = referenceGenerator.NextDoubleOpenOpen();
                    double exponential = mode == RandomMathMode.Portable ? -PortableMath.Log(unit) : -Math.Log(unit);
                    double standard;
                    if (shape == 1.0)
                    {
                        standard = exponential;
                    }
                    else
                    {
                        double logarithm = mode == RandomMathMode.Portable ? PortableMath.Log(exponential) : Math.Log(exponential);
                        standard = mode == RandomMathMode.Portable ? PortableMath.Exp(logarithm / shape) : Math.Exp(logarithm / shape);
                    }

                    double expected = -3.0 + 2.5 * standard;
                    Assert.That(PortableMath.DoubleBits(sampler.Sample()), Is.EqualTo(PortableMath.DoubleBits(expected)));
                }

                Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
            }
        }
    }

    [Test]
    public void RayleighMatchesStableSurvivalTransformInBothMathModes()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            RandomGenerator actualGenerator = Create(654UL, mode);
            RandomGenerator referenceGenerator = Create(654UL, mode);
            RayleighSampler sampler = new RayleighSampler(actualGenerator, -3.0, 2.5);

            for (int index = 0; index < 256; index++)
            {
                double unit = referenceGenerator.NextDoubleOpenOpen();
                double logarithm = mode == RandomMathMode.Portable ? PortableMath.Log(unit) : Math.Log(unit);
                double root = mode == RandomMathMode.Portable ? PortableMath.Sqrt(-2.0 * logarithm) : Math.Sqrt(-2.0 * logarithm);
                double expected = -3.0 + 2.5 * root;
                Assert.That(PortableMath.DoubleBits(sampler.Sample()), Is.EqualTo(PortableMath.DoubleBits(expected)));
            }

            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        }
    }

    [Test]
    public void ShapeOneWeibullMatchesExponentialSampler()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            RandomGenerator weibullGenerator = Create(17UL, mode);
            RandomGenerator exponentialGenerator = Create(17UL, mode);
            WeibullSampler weibull = new WeibullSampler(weibullGenerator, 0.0, 1.0, 1.0);
            ExponentialSampler exponential = new ExponentialSampler(exponentialGenerator, 1.0);

            for (int index = 0; index < 128; index++)
            {
                Assert.That(PortableMath.DoubleBits(weibull.Sample()), Is.EqualTo(PortableMath.DoubleBits(exponential.Sample())));
            }

            Assert.That(weibullGenerator.ExportState().ToBytes(), Is.EqualTo(exponentialGenerator.ExportState().ToBytes()));
        }
    }

    [Test]
    public void FillMatchesRepeatedSamplesAndEmptyFillConsumesNothing()
    {
        RandomGenerator weibullFillGenerator = Create(123UL, RandomMathMode.Portable);
        RandomGenerator weibullScalarGenerator = Create(123UL, RandomMathMode.Portable);
        RandomGenerator rayleighFillGenerator = Create(124UL, RandomMathMode.Portable);
        RandomGenerator rayleighScalarGenerator = Create(124UL, RandomMathMode.Portable);
        WeibullSampler weibullFill = new WeibullSampler(weibullFillGenerator, 4.0, 3.0, 0.8);
        WeibullSampler weibullScalar = new WeibullSampler(weibullScalarGenerator, 4.0, 3.0, 0.8);
        RayleighSampler rayleighFill = new RayleighSampler(rayleighFillGenerator, 4.0, 3.0);
        RayleighSampler rayleighScalar = new RayleighSampler(rayleighScalarGenerator, 4.0, 3.0);

        AssertFill(values => weibullFill.Fill(values), () => weibullScalar.Sample(), weibullFillGenerator, weibullScalarGenerator);
        AssertFill(values => rayleighFill.Fill(values), () => rayleighScalar.Sample(), rayleighFillGenerator, rayleighScalarGenerator);
    }

    [Test]
    public void ZeroScaleReturnsExactLocationWithoutConsumption()
    {
        RandomGenerator generator = Create(71UL, RandomMathMode.Portable);
        byte[] before = generator.ExportState().ToBytes();
        WeibullSampler weibull = new WeibullSampler(generator, -91.5, -0.0, 0.5);
        RayleighSampler rayleigh = new RayleighSampler(generator, -91.5, -0.0);
        double[] values = new double[8];

        Assert.Multiple(() =>
        {
            Assert.That(weibull.Sample(), Is.EqualTo(-91.5));
            Assert.That(rayleigh.Sample(), Is.EqualTo(-91.5));
        });
        weibull.Fill(values);
        Assert.That(values, Is.All.EqualTo(-91.5));
        rayleigh.Fill(values);
        Assert.Multiple(() =>
        {
            Assert.That(values, Is.All.EqualTo(-91.5));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void InvalidParametersAreRejectedWithoutConsumption()
    {
        Assert.That(Assert.Throws<RandomArgumentException>(() => new WeibullSampler(NullReference.Value<global::random.IRandomSource>(), 0.0, 1.0, 1.0))?.ArgumentName(), Is.EqualTo("generator"));
        Assert.That(Assert.Throws<RandomArgumentException>(() => new WeibullSampler(NullReference.Value<global::random.IRandomSource>(), 0.0, 1.0, 1.0))?.ArgumentName(), Is.EqualTo("generator"));
        Assert.That(Assert.Throws<RandomArgumentException>(() => new RayleighSampler(NullReference.Value<global::random.IRandomSource>(), 0.0, 1.0))?.ArgumentName(), Is.EqualTo("generator"));
        Assert.That(Assert.Throws<RandomArgumentException>(() => new RayleighSampler(NullReference.Value<global::random.IRandomSource>(), 0.0, 1.0))?.ArgumentName(), Is.EqualTo("generator"));

        RandomGenerator generator = Create(23UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        object[][] cases =
        [
            [(TestDelegate)(() => new WeibullSampler(generator, double.NaN, 1.0, 1.0)), "location"],
            [(TestDelegate)(() => new WeibullSampler(generator, 0.0, double.PositiveInfinity, 1.0)), "scale"],
            [(TestDelegate)(() => new WeibullSampler(generator, 0.0, -double.Epsilon, 1.0)), "scale"],
            [(TestDelegate)(() => new WeibullSampler(generator, 0.0, 1.0, 0.0)), "shape"],
            [(TestDelegate)(() => new WeibullSampler(generator, 0.0, 1.0, double.NaN)), "shape"],
            [(TestDelegate)(() => new WeibullSampler(generator, 0.0, 1.0, double.PositiveInfinity)), "shape"],
            [(TestDelegate)(() => new RayleighSampler(generator, double.NegativeInfinity, 1.0)), "location"],
            [(TestDelegate)(() => new RayleighSampler(generator, 0.0, double.NaN)), "scale"],
            [(TestDelegate)(() => new RayleighSampler(generator, 0.0, -double.Epsilon)), "scale"]
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        }
    }

    [Test]
    public void OpenEndpointAndExtremeShapeTransformsRemainDefined()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            double weibullUpper = new WeibullSampler(Create(OneOutputSeed, mode), 0.0, 1.0, 2.0).Sample();
            double weibullLower = new WeibullSampler(Create(UInt64MaximumOutputSeed, mode), 0.0, 1.0, 2.0).Sample();
            double rayleighUpper = new RayleighSampler(Create(OneOutputSeed, mode), 0.0, 1.0).Sample();
            double rayleighLower = new RayleighSampler(Create(UInt64MaximumOutputSeed, mode), 0.0, 1.0).Sample();
            double tinyShapeUpper = new WeibullSampler(Create(OneOutputSeed, mode), 0.0, 1.0, double.Epsilon).Sample();
            double tinyShapeLower = new WeibullSampler(Create(UInt64MaximumOutputSeed, mode), 0.0, 1.0, double.Epsilon).Sample();
            double largeShapeUpper = new WeibullSampler(Create(OneOutputSeed, mode), 0.0, 1.0, double.MaxValue).Sample();
            double largeShapeLower = new WeibullSampler(Create(UInt64MaximumOutputSeed, mode), 0.0, 1.0, double.MaxValue).Sample();

            Assert.Multiple(() =>
            {
                Assert.That(weibullUpper, Is.GreaterThan(6.0));
                Assert.That(weibullLower, Is.GreaterThan(0.0).And.LessThan(2.0e-8));
                Assert.That(rayleighUpper, Is.GreaterThan(8.0));
                Assert.That(rayleighLower, Is.GreaterThan(0.0).And.LessThan(2.0e-8));
                Assert.That(double.IsFinite(weibullUpper), Is.True);
                Assert.That(double.IsFinite(rayleighUpper), Is.True);
                Assert.That(tinyShapeUpper, Is.EqualTo(double.PositiveInfinity));
                Assert.That(tinyShapeLower, Is.EqualTo(0.0));
                Assert.That(largeShapeUpper, Is.EqualTo(1.0));
                Assert.That(largeShapeLower, Is.EqualTo(1.0));
            });
        }
    }

    [Test]
    public void ZeroRawNumeratorIsRejectedByBothTransforms()
    {
        RandomGenerator weibullGenerator = Create(ZeroOutputSeed, RandomMathMode.Portable);
        RandomGenerator weibullReference = Create(ZeroOutputSeed, RandomMathMode.Portable);
        RandomGenerator rayleighGenerator = Create(ZeroOutputSeed, RandomMathMode.Portable);
        RandomGenerator rayleighReference = Create(ZeroOutputSeed, RandomMathMode.Portable);
        double weibull = new WeibullSampler(weibullGenerator, 0.0, 1.0, 1.5).Sample();
        double rayleigh = new RayleighSampler(rayleighGenerator, 0.0, 1.0).Sample();
        double weibullUnit = weibullReference.NextDoubleOpenOpen();
        double rayleighUnit = rayleighReference.NextDoubleOpenOpen();

        Assert.Multiple(() =>
        {
            Assert.That(weibullUnit, Is.GreaterThan(0.0).And.LessThan(1.0));
            Assert.That(rayleighUnit, Is.GreaterThan(0.0).And.LessThan(1.0));
            Assert.That(double.IsFinite(weibull), Is.True);
            Assert.That(double.IsFinite(rayleigh), Is.True);
            Assert.That(weibullGenerator.ExportState().ToBytes(), Is.EqualTo(weibullReference.ExportState().ToBytes()));
            Assert.That(rayleighGenerator.ExportState().ToBytes(), Is.EqualTo(rayleighReference.ExportState().ToBytes()));
        });
    }

    [Test]
    public void LargeScaleOverflowIsExplicit()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            double weibull = new WeibullSampler(Create(OneOutputSeed, mode), 0.0, double.MaxValue, 2.0).Sample();
            double rayleigh = new RayleighSampler(Create(OneOutputSeed, mode), 0.0, double.MaxValue).Sample();
            Assert.Multiple(() =>
            {
                Assert.That(weibull, Is.EqualTo(double.PositiveInfinity));
                Assert.That(rayleigh, Is.EqualTo(double.PositiveInfinity));
            });
        }
    }

    [Test]
    public void DeterministicMomentsMatchSelectedParameterizations()
    {
        const int sampleCount = 100000;
        WeibullSampler exponentialWeibull = new WeibullSampler(Create(101UL, RandomMathMode.Portable), 0.0, 1.0, 1.0);
        RayleighSampler rayleigh = new RayleighSampler(Create(102UL, RandomMathMode.Portable), 0.0, 1.0);
        double weibullSum = 0.0;
        double rayleighSquareSum = 0.0;

        for (int index = 0; index < sampleCount; index++)
        {
            weibullSum += exponentialWeibull.Sample();
            double value = rayleigh.Sample();
            rayleighSquareSum += value * value;
        }

        Assert.Multiple(() =>
        {
            Assert.That(weibullSum / sampleCount, Is.EqualTo(1.0).Within(0.015));
            Assert.That(rayleighSquareSum / sampleCount, Is.EqualTo(2.0).Within(0.03));
        });
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
