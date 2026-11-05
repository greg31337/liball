namespace random.Tests;

public class FisherSnedecorSamplerTests
{
    [Test]
    public void SamplesMatchStableBetaOddsInBothMathModes()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            RandomGenerator actualGenerator = Create(42UL, mode);
            RandomGenerator referenceGenerator = Create(42UL, mode);
            FisherSnedecorSampler actual = new FisherSnedecorSampler(actualGenerator, 5.0, 9.0);
            BetaSampler beta = new BetaSampler(referenceGenerator, 2.5, 4.5);
            double logDegreeRatio = mode == RandomMathMode.Portable ? PortableMath.Log(9.0) - PortableMath.Log(5.0) : Math.Log(9.0) - Math.Log(5.0);

            for (int index = 0; index < 100; index++)
            {
                double betaValue = beta.Sample();
                double expected;
                if (betaValue == 0.0)
                {
                    expected = 0.0;
                }
                else if (betaValue == 1.0)
                {
                    expected = double.PositiveInfinity;
                }
                else if (mode == RandomMathMode.Portable)
                {
                    expected = PortableMath.Exp(logDegreeRatio + PortableMath.Log(betaValue) - PortableMath.Log1p(-betaValue));
                }
                else
                {
                    expected = Math.Exp(logDegreeRatio + Math.Log(betaValue) - Math.Log(1.0 - betaValue));
                }

                Assert.That(PortableMath.DoubleBits(actual.Sample()), Is.EqualTo(PortableMath.DoubleBits(expected)));
            }

            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        }
    }

    [Test]
    public void FillMatchesRepeatedSamples()
    {
        RandomGenerator fillGenerator = Create(123456UL, RandomMathMode.Portable);
        RandomGenerator scalarGenerator = Create(123456UL, RandomMathMode.Portable);
        FisherSnedecorSampler fillSampler = new FisherSnedecorSampler(fillGenerator, 7.0, 11.0);
        FisherSnedecorSampler scalarSampler = new FisherSnedecorSampler(scalarGenerator, 7.0, 11.0);
        double[] actual = new double[257];
        double[] expected = new double[actual.Length];
        byte[] beforeEmptyFill = fillGenerator.ExportState().ToBytes();

        fillSampler.Fill(Span<double>.Empty);
        Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(beforeEmptyFill));
        fillSampler.Fill(actual);
        for (int index = 0; index < expected.Length; index++)
        {
            expected[index] = scalarSampler.Sample();
        }

        for (int index = 0; index < actual.Length; index++)
        {
            Assert.That(PortableMath.DoubleBits(actual[index]), Is.EqualTo(PortableMath.DoubleBits(expected[index])));
        }

        Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(scalarGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void InvalidDegreesOfFreedomAreRejectedWithoutConsumption()
    {
        RandomArgumentException nullError = Assert.Throws<RandomArgumentException>(() => new FisherSnedecorSampler(NullReference.Value<global::random.IRandomSource>(), 1.0, 1.0));
        Assert.That(nullError.ArgumentName(), Is.EqualTo("generator"));

        RandomGenerator generator = Create(23UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        object[] cases =
        [
            new object[] { (TestDelegate)(() => new FisherSnedecorSampler(generator, double.NaN, 1.0)), "numeratorDegreesOfFreedom" },
            new object[] { (TestDelegate)(() => new FisherSnedecorSampler(generator, double.PositiveInfinity, 1.0)), "numeratorDegreesOfFreedom" },
            new object[] { (TestDelegate)(() => new FisherSnedecorSampler(generator, 0.0, 1.0)), "numeratorDegreesOfFreedom" },
            new object[] { (TestDelegate)(() => new FisherSnedecorSampler(generator, -double.Epsilon, 1.0)), "numeratorDegreesOfFreedom" },
            new object[] { (TestDelegate)(() => new FisherSnedecorSampler(generator, 1.0, double.NaN)), "denominatorDegreesOfFreedom" },
            new object[] { (TestDelegate)(() => new FisherSnedecorSampler(generator, 1.0, double.NegativeInfinity)), "denominatorDegreesOfFreedom" },
            new object[] { (TestDelegate)(() => new FisherSnedecorSampler(generator, 1.0, 0.0)), "denominatorDegreesOfFreedom" },
            new object[] { (TestDelegate)(() => new FisherSnedecorSampler(generator, 1.0, -double.Epsilon)), "denominatorDegreesOfFreedom" }
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        }
    }

    [Test]
    public void ExtremeDegreesAvoidZeroOverZeroAndRatioOverflow()
    {
        FisherSnedecorSampler largeSymmetric = new FisherSnedecorSampler(Create(42UL, RandomMathMode.Portable), double.MaxValue, double.MaxValue);
        FisherSnedecorSampler tinySymmetric = new FisherSnedecorSampler(Create(42UL, RandomMathMode.Portable), double.Epsilon, double.Epsilon);
        FisherSnedecorSampler high = new FisherSnedecorSampler(Create(42UL, RandomMathMode.Portable), double.MaxValue, double.Epsilon);
        FisherSnedecorSampler low = new FisherSnedecorSampler(Create(42UL, RandomMathMode.Portable), double.Epsilon, double.MaxValue);
        bool foundZero = false;
        bool foundInfinity = false;

        for (int index = 0; index < 256; index++)
        {
            double tiny = tinySymmetric.Sample();
            Assert.Multiple(() =>
            {
                Assert.That(PortableMath.DoubleBits(largeSymmetric.Sample()), Is.EqualTo(0x3FF0000000000000UL));
                Assert.That(double.IsNaN(tiny), Is.False);
                Assert.That(high.Sample(), Is.EqualTo(double.PositiveInfinity));
                Assert.That(PortableMath.DoubleBits(low.Sample()), Is.EqualTo(0UL));
            });
            if (tiny == 0.0)
            {
                foundZero = true;
            }
            else if (tiny == double.PositiveInfinity)
            {
                foundInfinity = true;
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(foundZero, Is.True);
            Assert.That(foundInfinity, Is.True);
        });
    }

    [Test]
    public void DeterministicMeanMatchesFiniteMoment()
    {
        const int sampleCount = 200000;
        FisherSnedecorSampler sampler = new FisherSnedecorSampler(Create(123UL, RandomMathMode.Portable), 5.0, 9.0);
        double sum = 0.0;

        for (int index = 0; index < sampleCount; index++)
        {
            double value = sampler.Sample();
            Assert.That(value, Is.GreaterThanOrEqualTo(0.0));
            sum += value;
        }

        Assert.That(sum / sampleCount, Is.EqualTo(9.0 / 7.0).Within(0.03));
    }

    [Test]
    public void PortableSequenceHasLockedBits()
    {
        ulong[] expected =
        [
            0x3FF07C6957B5D706UL,
            0x3FECBC07E3276750UL,
            0x3FE1DD4D254D6DF9UL,
            0x3FF18499BC52326DUL
        ];
        FisherSnedecorSampler sampler = new FisherSnedecorSampler(Create(42UL, RandomMathMode.Portable), 5.0, 9.0);

        foreach (ulong expectedBits in expected)
        {
            Assert.That(PortableMath.DoubleBits(sampler.Sample()), Is.EqualTo(expectedBits));
        }
    }

    static RandomGenerator Create(ulong seed, RandomMathMode mode)
    {
        return RandomGenerator.CreateSplitMix64(seed, mode);
    }
}
