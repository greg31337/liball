namespace random.Tests;

public class TruncatedCompositionSamplerTests
{
    [Test]
    public void TruncationMatchesDirectBuiltInSamplerRejection()
    {
        RandomGenerator actualGenerator = Create(101UL);
        RandomGenerator expectedGenerator = Create(101UL);
        NormalSampler actualSource = new NormalSampler(actualGenerator, 0.25, 1.5);
        NormalSampler expectedSource = new NormalSampler(expectedGenerator, 0.25, 1.5);
        TruncatedSampler sampler = new TruncatedSampler(actualSource.Sample, -0.5, 0.75, 10000);
        long expectedTotal = 0L;

        for (int sampleIndex = 0; sampleIndex < 2000; sampleIndex++)
        {
            int expectedAttempts = 0;
            double expected;
            do
            {
                expectedAttempts++;
                expectedTotal++;
                expected = expectedSource.Sample();
            }
            while (expected < -0.5 || expected > 0.75);

            Assert.Multiple(() =>
            {
                Assert.That(sampler.Sample(), Is.EqualTo(expected));
                Assert.That(sampler.LastAttemptCount(), Is.EqualTo(expectedAttempts));
                Assert.That(sampler.TotalAttemptCount(), Is.EqualTo(expectedTotal));
            });
        }

        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(expectedGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void InclusiveBoundsAndNonFiniteCandidatesFollowContract()
    {
        double[] candidates = [-1.0, 0.0, double.NaN, 1.0, double.NegativeInfinity, double.PositiveInfinity];
        int index = 0;
        TruncatedSampler finite = new TruncatedSampler(() => candidates[index++], 0.0, 1.0, 4);
        Assert.That(finite.Sample(), Is.EqualTo(0.0));
        Assert.That(finite.Sample(), Is.EqualTo(1.0));

        TruncatedSampler negativeInfinity = new TruncatedSampler(() => double.NegativeInfinity, double.NegativeInfinity, 0.0, 1);
        TruncatedSampler positiveInfinity = new TruncatedSampler(() => double.PositiveInfinity, 0.0, double.PositiveInfinity, 1);
        Assert.Multiple(() =>
        {
            Assert.That(negativeInfinity.Sample(), Is.EqualTo(double.NegativeInfinity));
            Assert.That(positiveInfinity.Sample(), Is.EqualTo(double.PositiveInfinity));
        });
    }

    [Test]
    public void TruncationExhaustionNeverReturnsTheLastRejectedCandidate()
    {
        int calls = 0;
        TruncatedSampler sampler = new TruncatedSampler(() => ++calls, 10.0, 11.0, 3);

        Assert.Throws<RandomExhaustedException>(() => sampler.Sample());

        Assert.Multiple(() =>
        {
            Assert.That(calls, Is.EqualTo(3));
            Assert.That(sampler.MaximumAttempts(), Is.EqualTo(3));
            Assert.That(sampler.LastAttemptCount(), Is.EqualTo(3));
            Assert.That(sampler.TotalAttemptCount(), Is.EqualTo(3L));
        });
    }

    [Test]
    public void TruncationFillMatchesRepeatedSamplesAndDiagnostics()
    {
        RandomGenerator fillGenerator = Create(202UL);
        RandomGenerator repeatedGenerator = Create(202UL);
        UniformSampler fillSource = new UniformSampler(fillGenerator, -2.0, 2.0);
        UniformSampler repeatedSource = new UniformSampler(repeatedGenerator, -2.0, 2.0);
        TruncatedSampler filling = new TruncatedSampler(fillSource.Sample, -0.25, 1.0, 1000);
        TruncatedSampler repeated = new TruncatedSampler(repeatedSource.Sample, -0.25, 1.0, 1000);
        byte[] before = fillGenerator.ExportState().ToBytes();
        filling.Fill(Array.Empty<double>());
        Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(before));
        double[] actual = new double[1000];
        double[] expected = new double[1000];
        filling.Fill(actual);
        for (int index = 0; index < expected.Length; index++)
        {
            expected[index] = repeated.Sample();
        }

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(filling.LastAttemptCount(), Is.EqualTo(repeated.LastAttemptCount()));
            Assert.That(filling.TotalAttemptCount(), Is.EqualTo(repeated.TotalAttemptCount()));
            Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(repeatedGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void TruncationDelegateExceptionsPreserveAttemptDiagnostics()
    {
        InvalidOperationException expected = new InvalidOperationException("source");
        TruncatedSampler sampler = new TruncatedSampler(() => throw expected, 0.0, 1.0, 5);

        InvalidOperationException actual = Assert.Throws<InvalidOperationException>(() => sampler.Sample());

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.SameAs(expected));
            Assert.That(sampler.LastAttemptCount(), Is.EqualTo(1));
            Assert.That(sampler.TotalAttemptCount(), Is.EqualTo(1L));
        });
    }

    [Test]
    public void AffineSamplingMatchesFixedOrderBuiltInComposition()
    {
        RandomGenerator actualGenerator = Create(303UL);
        RandomGenerator expectedGenerator = Create(303UL);
        StudentTSampler actualSource = new StudentTSampler(actualGenerator, 7.0);
        StudentTSampler expectedSource = new StudentTSampler(expectedGenerator, 7.0);
        AffineSampler sampler = new AffineSampler(actualSource.Sample, -3.0, -2.5);

        for (int index = 0; index < 5000; index++)
        {
            double expected = -3.0 + -2.5 * expectedSource.Sample();
            Assert.That(sampler.Sample(), Is.EqualTo(expected));
        }

        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(expectedGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void AffineFillMatchesRepeatedSamplesAndPropagatesNonFiniteOutput()
    {
        int fillValue = 0;
        int repeatedValue = 0;
        AffineSampler filling = new AffineSampler(() => ++fillValue, 1.0, 2.0);
        AffineSampler repeated = new AffineSampler(() => ++repeatedValue, 1.0, 2.0);
        double[] actual = new double[1000];
        double[] expected = new double[1000];
        filling.Fill(actual);
        for (int index = 0; index < expected.Length; index++)
        {
            expected[index] = repeated.Sample();
        }

        AffineSampler overflow = new AffineSampler(() => double.MaxValue, double.MaxValue, 2.0);
        AffineSampler notANumber = new AffineSampler(() => double.NaN, 0.0, 1.0);
        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(fillValue, Is.EqualTo(repeatedValue));
            Assert.That(overflow.Sample(), Is.EqualTo(double.PositiveInfinity));
            Assert.That(double.IsNaN(notANumber.Sample()), Is.True);
        });
    }

    [Test]
    public void ZeroAffineScaleDoesNotInvokeSource()
    {
        int calls = 0;
        AffineSampler sampler = new AffineSampler(
            () =>
            {
                calls++;
                return double.NaN;
            },
            -17.5,
            -0.0);
        double[] destination = new double[100];

        sampler.Fill(destination);

        Assert.Multiple(() =>
        {
            Assert.That(destination, Is.All.EqualTo(-17.5));
            Assert.That(calls, Is.EqualTo(0));
        });
    }

    [Test]
    public void InvalidCompositionInputsAreRejectedBeforeSourceInvocation()
    {
        int calls = 0;
        Func<double> source = () => ++calls;
        AssertArgument("source", () => new TruncatedSampler(NullReference.Value<global::System.Func<double>>(), 0.0, 1.0, 1));
        AssertArgument("lowerBound", () => new TruncatedSampler(source, double.NaN, 1.0, 1));
        AssertArgument("upperBound", () => new TruncatedSampler(source, 0.0, double.NaN, 1));
        AssertArgument("upperBound", () => new TruncatedSampler(source, 2.0, 1.0, 1));
        AssertArgument("upperBound", () => new TruncatedSampler(source, double.PositiveInfinity, double.PositiveInfinity, 1));
        AssertArgument("maximumAttempts", () => new TruncatedSampler(source, 0.0, 1.0, 0));
        AssertArgument("source", () => new AffineSampler(NullReference.Value<global::System.Func<double>>(), 0.0, 1.0));
        AssertArgument("offset", () => new AffineSampler(source, double.PositiveInfinity, 1.0));
        AssertArgument("scale", () => new AffineSampler(source, 0.0, double.NaN));
        Assert.That(calls, Is.EqualTo(0));
    }

    static RandomGenerator Create(ulong seed)
    {
        return RandomGenerator.CreateSplitMix64(seed, RandomMathMode.Portable);
    }

    static void AssertArgument(string expectedName, TestDelegate action)
    {
        RandomArgumentException error = Assert.Throws<RandomArgumentException>(action);
        Assert.That(error.ArgumentName(), Is.EqualTo(expectedName));
    }
}
