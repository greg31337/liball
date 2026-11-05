namespace random.Tests;

public class ConwayMaxwellPoissonSamplerTests
{
    [Test]
    public void DispersionOneMatchesPoissonExactly()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            RandomGenerator actualGenerator = Create(101UL, mode);
            RandomGenerator referenceGenerator = Create(101UL, mode);
            byte[] before = actualGenerator.ExportState().ToBytes();
            ConwayMaxwellPoissonSampler actual = new ConwayMaxwellPoissonSampler(actualGenerator, 12.5, 1.0);
            PoissonSampler reference = new PoissonSampler(referenceGenerator, 12.5);

            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(before));
            for (int index = 0; index < 2000; index++)
            {
                Assert.That(actual.Sample(), Is.EqualTo(reference.Sample()));
            }

            Assert.Multiple(() =>
            {
                Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
                Assert.That(actual.NormalizationConverged(), Is.True);
                Assert.That(actual.NormalizationTermCount(), Is.EqualTo(1));
                Assert.That(actual.NormalizationRelativeErrorBound(), Is.Zero);
                Assert.That(actual.LogNormalization(), Is.EqualTo(12.5));
            });
        }
    }

    [Test]
    public void DispersionZeroMatchesFailureCountGeometricExactly()
    {
        RandomGenerator actualGenerator = Create(202UL, RandomMathMode.Portable);
        RandomGenerator referenceGenerator = Create(202UL, RandomMathMode.Portable);
        ConwayMaxwellPoissonSampler actual = new ConwayMaxwellPoissonSampler(actualGenerator, 0.35, 0.0);
        GeometricSampler reference = new GeometricSampler(referenceGenerator, 0.65);

        for (int index = 0; index < 2000; index++)
        {
            Assert.That(actual.Sample(), Is.EqualTo(reference.SampleFailures()));
        }

        Assert.Multiple(() =>
        {
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
            Assert.That(actual.NormalizationConverged(), Is.True);
            Assert.That(actual.NormalizationRelativeErrorBound(), Is.Zero);
            Assert.That(actual.LogNormalization(), Is.EqualTo(-PortableMath.Log1p(-0.35)).Within(1.0e-15));
        });
    }

    [Test]
    public void ZeroRateIsAPointMassWithoutConsumption()
    {
        double[] dispersions = [0.0, 0.25, 1.0, 10.0];
        foreach (double dispersion in dispersions)
        {
            RandomGenerator generator = Create(303UL, RandomMathMode.Portable);
            generator.NextBoolean();
            byte[] before = generator.ExportState().ToBytes();
            ConwayMaxwellPoissonSampler sampler = new ConwayMaxwellPoissonSampler(generator, 0.0, dispersion);
            long[] values = new long[32];

            Assert.That(sampler.Sample(), Is.Zero);
            sampler.Fill(values);

            Assert.Multiple(() =>
            {
                Assert.That(values, Is.All.Zero);
                Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
                Assert.That(sampler.NormalizationConverged(), Is.True);
                Assert.That(sampler.LogNormalization(), Is.Zero);
            });
        }
    }

    [Test]
    public void FillMatchesRepeatedScalarSamplesAcrossRegimesAndModes()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        double[][] parameters = [[4.0, 2.0], [2.0, 0.5]];
        foreach (RandomMathMode mode in modes)
        {
            foreach (double[] pair in parameters)
            {
                RandomGenerator fillGenerator = Create(404UL, mode);
                RandomGenerator scalarGenerator = Create(404UL, mode);
                ConwayMaxwellPoissonSampler fillSampler = new ConwayMaxwellPoissonSampler(fillGenerator, pair[0], pair[1]);
                ConwayMaxwellPoissonSampler scalarSampler = new ConwayMaxwellPoissonSampler(scalarGenerator, pair[0], pair[1]);
                long[] actual = new long[513];
                long[] expected = new long[actual.Length];
                byte[] beforeEmpty = fillGenerator.ExportState().ToBytes();

                fillSampler.Fill(Span<long>.Empty);
                Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(beforeEmpty));
                fillSampler.Fill(actual);
                for (int index = 0; index < expected.Length; index++)
                {
                    expected[index] = scalarSampler.Sample();
                }

                Assert.Multiple(() =>
                {
                    Assert.That(actual, Is.EqualTo(expected));
                    Assert.That(actual, Is.All.GreaterThanOrEqualTo(0L));
                    Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(scalarGenerator.ExportState().ToBytes()));
                });
            }
        }
    }

    [Test]
    public void RejectionSamplesMatchUnderdispersedAndOverdispersedMoments()
    {
        AssertMoments(4.0, 2.0, 505UL, 0.015, 0.025);
        AssertMoments(2.0, 0.5, 606UL, 0.04, 0.15);
    }

    [Test]
    public void NormalizerConvergesFromTheModeForDifficultParameters()
    {
        double[][] parameters = [[10000.0, 4.0], [1.0, 0.05], [0.999, double.Epsilon]];
        foreach (double[] pair in parameters)
        {
            ConwayMaxwellPoissonSampler sampler = new ConwayMaxwellPoissonSampler(
                Create(707UL, RandomMathMode.Portable),
                pair[0],
                pair[1]);

            Assert.Multiple(() =>
            {
                Assert.That(sampler.NormalizationConverged(), Is.True);
                Assert.That(sampler.NormalizationTermCount(), Is.GreaterThan(0).And.LessThanOrEqualTo(ConwayMaxwellPoissonSampler.MaximumNormalizationTerms));
                Assert.That(sampler.NormalizationRelativeErrorBound(), Is.LessThanOrEqualTo(ConwayMaxwellPoissonSampler.NormalizationRelativeTolerance));
                Assert.That(double.IsFinite(sampler.LogNormalization()), Is.True);
            });
        }
    }

    [Test]
    public void CertifiedLogNormalizerMatchesDirectSummation()
    {
        double[][] parameters = [[4.0, 2.0], [2.0, 0.5], [0.25, 3.0]];
        foreach (double[] pair in parameters)
        {
            ConwayMaxwellPoissonSampler sampler = new ConwayMaxwellPoissonSampler(
                Create(808UL, RandomMathMode.Portable),
                pair[0],
                pair[1]);
            double expected = Math.Log(DirectMoments(pair[0], pair[1])[0]);

            Assert.That(sampler.LogNormalization(), Is.EqualTo(expected).Within(2.0e-12));
        }
    }

    [Test]
    public void UncertifiedNormalizerReportsItsWorkLimitExplicitly()
    {
        RandomGenerator generator = Create(909UL, RandomMathMode.Portable);
        byte[] before = generator.ExportState().ToBytes();
        ConwayMaxwellPoissonSampler sampler = new ConwayMaxwellPoissonSampler(generator, 1.0e12, 1.000001);

        Assert.Multiple(() =>
        {
            Assert.That(sampler.NormalizationConverged(), Is.False);
            Assert.That(sampler.NormalizationTermCount(), Is.EqualTo(ConwayMaxwellPoissonSampler.MaximumNormalizationTerms));
            Assert.That(sampler.NormalizationRelativeErrorBound(), Is.GreaterThan(ConwayMaxwellPoissonSampler.NormalizationRelativeTolerance));
            Assert.Throws<RandomExhaustedException>(() => sampler.LogNormalization());
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void InvalidParametersAreRejectedWithoutConsumption()
    {
        RandomArgumentException nullGenerator = Assert.Throws<RandomArgumentException>(() => new ConwayMaxwellPoissonSampler(NullReference.Value<global::random.IRandomSource>(), 1.0, 1.0));
        Assert.That(nullGenerator.ArgumentName(), Is.EqualTo("generator"));

        RandomGenerator generator = Create(1001UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        double[][] invalid =
        [
            [-1.0, 1.0],
            [double.NaN, 1.0],
            [double.PositiveInfinity, 1.0],
            [1.0, -double.Epsilon],
            [1.0, double.NaN],
            [1.0, double.PositiveInfinity],
            [1.0, 0.0],
            [PortableMath.NextUp(1.0), 0.0],
            [double.MaxValue, 0.5]
        ];

        foreach (double[] pair in invalid)
        {
            Assert.Throws<RandomArgumentException>(() => new ConwayMaxwellPoissonSampler(generator, pair[0], pair[1]));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        }
    }

    static void AssertMoments(
        double rate,
        double dispersion,
        ulong seed,
        double meanTolerance,
        double varianceTolerance)
    {
        const int sampleCount = 150000;
        double[] direct = DirectMoments(rate, dispersion);
        ConwayMaxwellPoissonSampler sampler = new ConwayMaxwellPoissonSampler(
            Create(seed, RandomMathMode.Portable),
            rate,
            dispersion);
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
        Assert.Multiple(() =>
        {
            Assert.That(mean, Is.EqualTo(direct[1]).Within(meanTolerance));
            Assert.That(variance, Is.EqualTo(direct[2]).Within(varianceTolerance));
        });
    }

    static double[] DirectMoments(double rate, double dispersion)
    {
        double term = 1.0;
        double total = 1.0;
        double first = 0.0;
        double second = 0.0;
        int count = 1;
        while (count < 100000)
        {
            term *= rate / Math.Pow(count, dispersion);
            total += term;
            first += count * term;
            second += count * count * term;
            double nextRatio = rate / Math.Pow(count + 1.0, dispersion);
            if (nextRatio < 1.0 && term / total < 1.0e-16)
            {
                break;
            }

            count++;
        }

        double mean = first / total;
        return [total, mean, second / total - mean * mean];
    }

    static RandomGenerator Create(ulong seed, RandomMathMode mode)
    {
        return RandomGenerator.CreateSplitMix64(seed, mode);
    }
}
