namespace random.Tests;

public class GeometricSamplerTests
{
    [Test]
    public void FailureCountsMatchTheStableInverseTransformExactly()
    {
        RandomGenerator actualGenerator = Create(42UL, RandomMathMode.Portable);
        RandomGenerator referenceGenerator = Create(42UL, RandomMathMode.Portable);
        byte[] before = actualGenerator.ExportState().ToBytes();
        GeometricSampler sampler = new GeometricSampler(actualGenerator, 0.2);
        double denominator = PortableMath.Log1p(-0.2);

        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(before));
        for (int index = 0; index < 1000; index++)
        {
            double unit = referenceGenerator.NextDoubleOpenClosed();
            long expected = (long)Math.Floor(PortableMath.Log(unit) / denominator);
            Assert.That(sampler.SampleFailures(), Is.EqualTo(expected));
        }

        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void TrialAndFailureConventionsDifferByOneOnTheSameVariates()
    {
        RandomGenerator failureGenerator = Create(123UL, RandomMathMode.Portable);
        RandomGenerator trialGenerator = Create(123UL, RandomMathMode.Portable);
        GeometricSampler failures = new GeometricSampler(failureGenerator, 0.37);
        GeometricSampler trials = new GeometricSampler(trialGenerator, 0.37);

        for (int index = 0; index < 1000; index++)
        {
            Assert.That(trials.SampleTrials(), Is.EqualTo(failures.SampleFailures() + 1L));
        }

        Assert.That(trialGenerator.ExportState().ToBytes(), Is.EqualTo(failureGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void TinyProbabilitiesRetainPrecision()
    {
        const double probability = 1.0e-15;
        RandomGenerator actualGenerator = Create(234UL, RandomMathMode.Portable);
        RandomGenerator referenceGenerator = Create(234UL, RandomMathMode.Portable);
        GeometricSampler sampler = new GeometricSampler(actualGenerator, probability);
        double denominator = PortableMath.Log1p(-probability);
        long maximum = 0L;

        for (int index = 0; index < 1000; index++)
        {
            double unit = referenceGenerator.NextDoubleOpenClosed();
            long expected = (long)Math.Floor(PortableMath.Log(unit) / denominator);
            long actual = sampler.SampleFailures();
            Assert.That(actual, Is.EqualTo(expected));
            if (actual > maximum)
            {
                maximum = actual;
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(maximum, Is.GreaterThan(100000000000000L));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void FillsMatchRepeatedScalarSamplesInBothConventionsAndModes()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            RandomGenerator failureFillGenerator = Create(345UL, mode);
            RandomGenerator failureScalarGenerator = Create(345UL, mode);
            GeometricSampler failureFill = new GeometricSampler(failureFillGenerator, 0.125);
            GeometricSampler failureScalar = new GeometricSampler(failureScalarGenerator, 0.125);
            long[] failureActual = new long[257];
            long[] failureExpected = new long[failureActual.Length];
            byte[] beforeEmpty = failureFillGenerator.ExportState().ToBytes();
            failureFill.FillFailures(Span<long>.Empty);
            Assert.That(failureFillGenerator.ExportState().ToBytes(), Is.EqualTo(beforeEmpty));
            failureFill.FillFailures(failureActual);
            for (int index = 0; index < failureExpected.Length; index++)
            {
                failureExpected[index] = failureScalar.SampleFailures();
            }

            RandomGenerator trialFillGenerator = Create(456UL, mode);
            RandomGenerator trialScalarGenerator = Create(456UL, mode);
            GeometricSampler trialFill = new GeometricSampler(trialFillGenerator, 0.125);
            GeometricSampler trialScalar = new GeometricSampler(trialScalarGenerator, 0.125);
            long[] trialActual = new long[257];
            long[] trialExpected = new long[trialActual.Length];
            trialFill.FillTrials(trialActual);
            for (int index = 0; index < trialExpected.Length; index++)
            {
                trialExpected[index] = trialScalar.SampleTrials();
            }

            Assert.Multiple(() =>
            {
                Assert.That(failureActual, Is.EqualTo(failureExpected));
                Assert.That(failureActual, Is.All.GreaterThanOrEqualTo(0L));
                Assert.That(failureFillGenerator.ExportState().ToBytes(), Is.EqualTo(failureScalarGenerator.ExportState().ToBytes()));
                Assert.That(trialActual, Is.EqualTo(trialExpected));
                Assert.That(trialActual, Is.All.GreaterThanOrEqualTo(1L));
                Assert.That(trialFillGenerator.ExportState().ToBytes(), Is.EqualTo(trialScalarGenerator.ExportState().ToBytes()));
            });
        }
    }

    [Test]
    public void CertainSuccessConsumesNoStateIncludingReservoirBits()
    {
        RandomGenerator generator = Create(567UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        GeometricSampler sampler = new GeometricSampler(generator, 1.0);
        long[] failures = new long[16];
        long[] trials = new long[16];

        long failure = sampler.SampleFailures();
        long trial = sampler.SampleTrials();
        sampler.FillFailures(failures);
        sampler.FillTrials(trials);

        Assert.Multiple(() =>
        {
            Assert.That(failure, Is.Zero);
            Assert.That(trial, Is.EqualTo(1L));
            Assert.That(failures, Is.All.Zero);
            Assert.That(trials, Is.All.EqualTo(1L));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void FailureAndTrialMomentsMatchTheirConventions()
    {
        const int sampleCount = 100000;
        GeometricSampler failureSampler = new GeometricSampler(Create(678UL, RandomMathMode.Portable), 0.2);
        GeometricSampler trialSampler = new GeometricSampler(Create(789UL, RandomMathMode.Portable), 0.2);
        double failureSum = 0.0;
        double failureSquareSum = 0.0;
        double trialSum = 0.0;
        double trialSquareSum = 0.0;
        for (int index = 0; index < sampleCount; index++)
        {
            double failure = failureSampler.SampleFailures();
            double trial = trialSampler.SampleTrials();
            failureSum += failure;
            failureSquareSum += failure * failure;
            trialSum += trial;
            trialSquareSum += trial * trial;
        }

        double failureMean = failureSum / sampleCount;
        double trialMean = trialSum / sampleCount;
        double failureVariance = failureSquareSum / sampleCount - failureMean * failureMean;
        double trialVariance = trialSquareSum / sampleCount - trialMean * trialMean;
        Assert.Multiple(() =>
        {
            Assert.That(failureMean, Is.EqualTo(4.0).Within(0.06));
            Assert.That(trialMean, Is.EqualTo(5.0).Within(0.06));
            Assert.That(failureVariance, Is.EqualTo(20.0).Within(0.5));
            Assert.That(trialVariance, Is.EqualTo(20.0).Within(0.5));
        });
    }

    [Test]
    public void UnrepresentableCountsRaiseExhaustionAfterOneVariate()
    {
        RandomGenerator failureGenerator = Create(890UL, RandomMathMode.Portable);
        RandomGenerator failureReference = Create(890UL, RandomMathMode.Portable);
        RandomGenerator trialGenerator = Create(901UL, RandomMathMode.Portable);
        RandomGenerator trialReference = Create(901UL, RandomMathMode.Portable);
        GeometricSampler failures = new GeometricSampler(failureGenerator, double.Epsilon);
        GeometricSampler trials = new GeometricSampler(trialGenerator, double.Epsilon);

        Assert.Throws<RandomExhaustedException>(() => failures.SampleFailures());
        Assert.Throws<RandomExhaustedException>(() => trials.SampleTrials());
        failureReference.NextDoubleOpenClosed();
        trialReference.NextDoubleOpenClosed();

        Assert.Multiple(() =>
        {
            Assert.That(failureGenerator.ExportState().ToBytes(), Is.EqualTo(failureReference.ExportState().ToBytes()));
            Assert.That(trialGenerator.ExportState().ToBytes(), Is.EqualTo(trialReference.ExportState().ToBytes()));
        });
    }

    [Test]
    public void InvalidProbabilitiesAreRejectedWithoutConsumption()
    {
        RandomArgumentException nullGenerator = Assert.Throws<RandomArgumentException>(() => new GeometricSampler(NullReference.Value<global::random.IRandomSource>(), 0.5));
        Assert.That(nullGenerator.ArgumentName(), Is.EqualTo("generator"));

        RandomGenerator generator = Create(912UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        double[] invalid =
        [
            0.0,
            -double.Epsilon,
            PortableMath.NextUp(1.0),
            double.NaN,
            double.NegativeInfinity,
            double.PositiveInfinity
        ];

        foreach (double probability in invalid)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>(() => new GeometricSampler(generator, probability));
            Assert.That(error.ArgumentName(), Is.EqualTo("probability"));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        }
    }

    static RandomGenerator Create(ulong seed, RandomMathMode mode)
    {
        return RandomGenerator.CreateSplitMix64(seed, mode);
    }
}
