namespace random.Tests;

public class NoncentralSamplerTests
{
    [Test]
    public void ZeroNoncentralityMatchesCentralSamplersInBothMathModes()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            RandomGenerator chiGenerator = Create(31UL, mode);
            RandomGenerator chiReferenceGenerator = Create(31UL, mode);
            RandomGenerator fGenerator = Create(32UL, mode);
            RandomGenerator fReferenceGenerator = Create(32UL, mode);
            NoncentralChiSquareSampler chi = new NoncentralChiSquareSampler(chiGenerator, 4.5, 0.0);
            ChiSquareSampler chiReference = new ChiSquareSampler(chiReferenceGenerator, 4.5);
            NoncentralFSampler f = new NoncentralFSampler(fGenerator, 4.5, 9.0, 0.0);
            FisherSnedecorSampler fReference = new FisherSnedecorSampler(fReferenceGenerator, 4.5, 9.0);

            for (int index = 0; index < 256; index++)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(Bits(chi.Sample()), Is.EqualTo(Bits(chiReference.Sample())));
                    Assert.That(Bits(f.Sample()), Is.EqualTo(Bits(fReference.Sample())));
                });
            }

            Assert.Multiple(() =>
            {
                Assert.That(chiGenerator.ExportState().ToBytes(), Is.EqualTo(chiReferenceGenerator.ExportState().ToBytes()));
                Assert.That(fGenerator.ExportState().ToBytes(), Is.EqualTo(fReferenceGenerator.ExportState().ToBytes()));
            });
        }
    }

    [Test]
    public void DegreesAboveOneMatchCentralPlusShiftedNormalConstruction()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            RandomGenerator actualGenerator = Create(41UL, mode);
            RandomGenerator referenceGenerator = Create(41UL, mode);
            NoncentralChiSquareSampler actual = new NoncentralChiSquareSampler(actualGenerator, 5.5, 3.25);
            ChiSquareSampler remainder = new ChiSquareSampler(referenceGenerator, 4.5);
            NormalSampler normal = new NormalSampler(referenceGenerator, 0.0, 1.0);
            double root = Sqrt(3.25, mode);

            for (int index = 0; index < 256; index++)
            {
                double central = remainder.Sample();
                double shifted = normal.Sample() + root;
                double expected = central + shifted * shifted;
                Assert.That(Bits(actual.Sample()), Is.EqualTo(Bits(expected)));
            }

            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        }
    }

    [Test]
    public void OneDegreeMatchesShiftedNormalSquareWithoutExtraConsumption()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            RandomGenerator actualGenerator = Create(51UL, mode);
            RandomGenerator referenceGenerator = Create(51UL, mode);
            NoncentralChiSquareSampler actual = new NoncentralChiSquareSampler(actualGenerator, 1.0, 7.0);
            NormalSampler normal = new NormalSampler(referenceGenerator, 0.0, 1.0);
            double root = Sqrt(7.0, mode);

            for (int index = 0; index < 256; index++)
            {
                double shifted = normal.Sample() + root;
                Assert.That(Bits(actual.Sample()), Is.EqualTo(Bits(shifted * shifted)));
            }

            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        }
    }

    [Test]
    public void FractionalDegreeCasesMatchPoissonGammaMixtureAcrossPoissonRegimes()
    {
        double[] noncentralities = [3.0, 40.0, 1000000.0];
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            foreach (double noncentrality in noncentralities)
            {
                RandomGenerator actualGenerator = Create(61UL, mode);
                RandomGenerator referenceGenerator = Create(61UL, mode);
                NoncentralChiSquareSampler actual = new NoncentralChiSquareSampler(actualGenerator, 0.5, noncentrality);
                PoissonCountSampler poisson = new PoissonCountSampler(referenceGenerator, noncentrality * 0.5);

                for (int index = 0; index < 256; index++)
                {
                    long count = poisson.Sample();
                    GammaSampler conditional = new GammaSampler(referenceGenerator, 0.25 + (double)count, 2.0);
                    Assert.That(Bits(actual.Sample()), Is.EqualTo(Bits(conditional.Sample())));
                }

                Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
            }
        }
    }

    [Test]
    public void InternalPoissonRegimesHaveExpectedMeanAndVariance()
    {
        const int sampleCount = 150000;
        double[] means = [2.5, 25.0, 1000.0];
        foreach (double mean in means)
        {
            PoissonCountSampler sampler = new PoissonCountSampler(Create(71UL, RandomMathMode.Portable), mean);
            double sum = 0.0;
            double squareSum = 0.0;
            for (int index = 0; index < sampleCount; index++)
            {
                double value = sampler.Sample();
                sum += value;
                squareSum += value * value;
            }

            double actualMean = sum / sampleCount;
            double actualVariance = squareSum / sampleCount - actualMean * actualMean;
            double tolerance = 0.03 * Sqrt(mean, RandomMathMode.Portable) + 0.02;
            Assert.Multiple(() =>
            {
                Assert.That(actualMean, Is.EqualTo(mean).Within(tolerance));
                Assert.That(actualVariance, Is.EqualTo(mean).Within(tolerance * 4.0));
            });
        }
    }

    [Test]
    public void NoncentralFMatchesIndependentChiSquareRatioInBothMathModes()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            RandomGenerator actualGenerator = Create(81UL, mode);
            RandomGenerator referenceGenerator = Create(81UL, mode);
            NoncentralFSampler actual = new NoncentralFSampler(actualGenerator, 5.0, 9.0, 3.0);
            NoncentralChiSquareSampler numerator = new NoncentralChiSquareSampler(referenceGenerator, 5.0, 3.0);
            ChiSquareSampler denominator = new ChiSquareSampler(referenceGenerator, 9.0);
            double logDegreeRatio = Log(9.0, mode) - Log(5.0, mode);

            for (int index = 0; index < 256; index++)
            {
                double numeratorValue = numerator.Sample();
                double denominatorValue = denominator.Sample();
                double expected;
                if (numeratorValue == 0.0)
                {
                    expected = 0.0;
                }
                else if (denominatorValue == 0.0 || double.IsPositiveInfinity(numeratorValue))
                {
                    expected = double.PositiveInfinity;
                }
                else if (double.IsPositiveInfinity(denominatorValue))
                {
                    expected = 0.0;
                }
                else
                {
                    expected = Exp(logDegreeRatio + Log(numeratorValue, mode) - Log(denominatorValue, mode), mode);
                }

                Assert.That(Bits(actual.Sample()), Is.EqualTo(Bits(expected)));
            }

            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        }
    }

    [Test]
    public void DeterministicMomentsMatchNoncentralDistributionTheory()
    {
        const int sampleCount = 150000;
        double[][] chiCases =
        [
            [0.5, 3.0],
            [1.0, 3.0],
            [5.0, 3.0]
        ];

        foreach (double[] parameters in chiCases)
        {
            NoncentralChiSquareSampler sampler = new NoncentralChiSquareSampler(
                Create(91UL, RandomMathMode.Portable),
                parameters[0],
                parameters[1]);
            double sum = 0.0;
            double squareSum = 0.0;
            for (int index = 0; index < sampleCount; index++)
            {
                double value = sampler.Sample();
                sum += value;
                squareSum += value * value;
            }

            double mean = sum / sampleCount;
            double variance = squareSum / sampleCount - mean * mean;
            double expectedMean = parameters[0] + parameters[1];
            double expectedVariance = 2.0 * (parameters[0] + 2.0 * parameters[1]);
            Assert.Multiple(() =>
            {
                Assert.That(mean, Is.EqualTo(expectedMean).Within(0.04));
                Assert.That(variance, Is.EqualTo(expectedVariance).Within(0.18));
            });
        }

        NoncentralFSampler fSampler = new NoncentralFSampler(Create(92UL, RandomMathMode.Portable), 5.0, 10.0, 3.0);
        double fSum = 0.0;
        for (int index = 0; index < sampleCount; index++)
        {
            fSum += fSampler.Sample();
        }

        double expectedFMean = 10.0 * (5.0 + 3.0) / (5.0 * (10.0 - 2.0));
        Assert.That(fSum / sampleCount, Is.EqualTo(expectedFMean).Within(0.04));
    }

    [Test]
    public void FillMatchesRepeatedSamplesAcrossConstructionPaths()
    {
        double[][] chiCases =
        [
            [4.0, 0.0],
            [5.0, 3.0],
            [1.0, 3.0],
            [0.5, 3.0],
            [0.5, 40.0]
        ];

        foreach (double[] parameters in chiCases)
        {
            RandomGenerator fillGenerator = Create(101UL, RandomMathMode.Portable);
            RandomGenerator scalarGenerator = Create(101UL, RandomMathMode.Portable);
            NoncentralChiSquareSampler fillSampler = new NoncentralChiSquareSampler(fillGenerator, parameters[0], parameters[1]);
            NoncentralChiSquareSampler scalarSampler = new NoncentralChiSquareSampler(scalarGenerator, parameters[0], parameters[1]);
            AssertFill(fillSampler, scalarSampler, fillGenerator, scalarGenerator);
        }

        double[][] fCases =
        [
            [5.0, 9.0, 0.0],
            [5.0, 9.0, 3.0],
            [0.5, 9.0, 3.0]
        ];

        foreach (double[] parameters in fCases)
        {
            RandomGenerator fillGenerator = Create(102UL, RandomMathMode.Portable);
            RandomGenerator scalarGenerator = Create(102UL, RandomMathMode.Portable);
            NoncentralFSampler fillSampler = new NoncentralFSampler(fillGenerator, parameters[0], parameters[1], parameters[2]);
            NoncentralFSampler scalarSampler = new NoncentralFSampler(scalarGenerator, parameters[0], parameters[1], parameters[2]);
            AssertFill(fillSampler, scalarSampler, fillGenerator, scalarGenerator);
        }
    }

    [Test]
    public void ExtremeParametersAndRatioBoundariesNeverReturnNaN()
    {
        NoncentralChiSquareSampler huge = new NoncentralChiSquareSampler(
            Create(111UL, RandomMathMode.Portable),
            1.0,
            double.MaxValue);
        NoncentralFSampler hugeF = new NoncentralFSampler(
            Create(112UL, RandomMathMode.Portable),
            1.0,
            double.MaxValue,
            double.MaxValue);
        NoncentralFSampler tinyDenominator = new NoncentralFSampler(
            Create(113UL, RandomMathMode.Portable),
            0.5,
            double.Epsilon,
            3.0);

        for (int index = 0; index < 128; index++)
        {
            Assert.Multiple(() =>
            {
                Assert.That(double.IsNaN(huge.Sample()), Is.False);
                Assert.That(double.IsNaN(hugeF.Sample()), Is.False);
                Assert.That(double.IsNaN(tinyDenominator.Sample()), Is.False);
            });
        }
    }

    [Test]
    public void InvalidParametersAndUnsafePoissonMeanAreRejectedWithoutConsumption()
    {
        Assert.That(
            Assert.Throws<RandomArgumentException>(() => new NoncentralChiSquareSampler(NullReference.Value<global::random.IRandomSource>(), 1.0, 0.0))?.ArgumentName(),
            Is.EqualTo("generator"));
        Assert.That(
            Assert.Throws<RandomArgumentException>(() => new NoncentralFSampler(NullReference.Value<global::random.IRandomSource>(), 1.0, 1.0, 0.0))?.ArgumentName(),
            Is.EqualTo("generator"));

        RandomGenerator generator = Create(121UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        double unsafeNoncentrality = Math.BitIncrement(PoissonCountSampler.MaximumMean * 2.0);
        object[][] cases =
        [
            [(TestDelegate)(() => new NoncentralChiSquareSampler(generator, 0.0, 1.0)), "degreesOfFreedom"],
            [(TestDelegate)(() => new NoncentralChiSquareSampler(generator, double.PositiveInfinity, 1.0)), "degreesOfFreedom"],
            [(TestDelegate)(() => new NoncentralChiSquareSampler(generator, 1.0, -double.Epsilon)), "noncentrality"],
            [(TestDelegate)(() => new NoncentralChiSquareSampler(generator, 1.0, double.NaN)), "noncentrality"],
            [(TestDelegate)(() => new NoncentralChiSquareSampler(generator, 0.5, unsafeNoncentrality)), "noncentrality"],
            [(TestDelegate)(() => new NoncentralFSampler(generator, 0.0, 1.0, 0.0)), "degreesOfFreedom"],
            [(TestDelegate)(() => new NoncentralFSampler(generator, 1.0, 0.0, 0.0)), "denominatorDegreesOfFreedom"],
            [(TestDelegate)(() => new NoncentralFSampler(generator, 1.0, double.NaN, -1.0)), "denominatorDegreesOfFreedom"],
            [(TestDelegate)(() => new NoncentralFSampler(generator, 1.0, 1.0, -double.Epsilon)), "noncentrality"],
            [(TestDelegate)(() => new NoncentralFSampler(generator, 0.5, 1.0, unsafeNoncentrality)), "noncentrality"]
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        }

        Assert.DoesNotThrow(() => new NoncentralChiSquareSampler(generator, 1.0, double.MaxValue));
        Assert.DoesNotThrow(() => new NoncentralFSampler(generator, 1.0, 1.0, double.MaxValue));
        Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
    }

    static void AssertFill(
        NoncentralChiSquareSampler fillSampler,
        NoncentralChiSquareSampler scalarSampler,
        RandomGenerator fillGenerator,
        RandomGenerator scalarGenerator)
    {
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

        AssertEqualBits(actual, expected);
        Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(scalarGenerator.ExportState().ToBytes()));
    }

    static void AssertFill(
        NoncentralFSampler fillSampler,
        NoncentralFSampler scalarSampler,
        RandomGenerator fillGenerator,
        RandomGenerator scalarGenerator)
    {
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

        AssertEqualBits(actual, expected);
        Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(scalarGenerator.ExportState().ToBytes()));
    }

    static void AssertEqualBits(double[] actual, double[] expected)
    {
        for (int index = 0; index < actual.Length; index++)
        {
            Assert.That(Bits(actual[index]), Is.EqualTo(Bits(expected[index])));
        }
    }

    static double Sqrt(double value, RandomMathMode mode)
    {
        return mode == RandomMathMode.Portable ? PortableMath.Sqrt(value) : Math.Sqrt(value);
    }

    static double Log(double value, RandomMathMode mode)
    {
        return mode == RandomMathMode.Portable ? PortableMath.Log(value) : Math.Log(value);
    }

    static double Exp(double value, RandomMathMode mode)
    {
        return mode == RandomMathMode.Portable ? PortableMath.Exp(value) : Math.Exp(value);
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
