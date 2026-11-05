namespace random.Tests;

public class EmpiricalSamplerTests
{
    [Test]
    public void EqualWeightSamplingMatchesUnbiasedIndexDraws()
    {
        RandomGenerator actualGenerator = Create(101UL);
        RandomGenerator expectedGenerator = Create(101UL);
        double[] observations = [-3.0, 1.5, 8.0, 20.0];
        EmpiricalSampler sampler = EmpiricalSampler.CreateUnweighted(actualGenerator, observations);

        for (int index = 0; index < 5000; index++)
        {
            double expected = observations[expectedGenerator.NextInt32Bounded(observations.Length)];
            Assert.That(sampler.Sample(), Is.EqualTo(expected));
        }

        Assert.Multiple(() =>
        {
            Assert.That(sampler.ObservationCount(), Is.EqualTo(observations.Length));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(expectedGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void WeightedSamplingMatchesAliasTableTraversal()
    {
        RandomGenerator actualGenerator = Create(202UL);
        RandomGenerator expectedGenerator = Create(202UL);
        double[] observations = [-3.0, 1.5, 8.0, 20.0];
        double[] weights = [0.0, 1.0, 3.0, 6.0];
        EmpiricalSampler sampler = new EmpiricalSampler(actualGenerator, observations, weights);
        AliasTable table = new AliasTable(weights);

        for (int index = 0; index < 5000; index++)
        {
            double expected = observations[table.Sample(expectedGenerator)];
            Assert.That(sampler.Sample(), Is.EqualTo(expected));
        }

        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(expectedGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void EqualAndWeightedFrequenciesMatchTheirMasses()
    {
        const int sampleCount = 100000;
        double[] observations = [10.0, 20.0, 30.0, 40.0];
        EmpiricalSampler equal = EmpiricalSampler.CreateUnweighted(Create(303UL), observations);
        EmpiricalSampler weighted = new EmpiricalSampler(Create(404UL), observations, [0.0, 1.0, 3.0, 6.0]);
        int[] equalCounts = new int[4];
        int[] weightedCounts = new int[4];
        for (int index = 0; index < sampleCount; index++)
        {
            equalCounts[Array.IndexOf(observations, equal.Sample())]++;
            weightedCounts[Array.IndexOf(observations, weighted.Sample())]++;
        }

        Assert.Multiple(() =>
        {
            for (int index = 0; index < equalCounts.Length; index++)
            {
                Assert.That((double)equalCounts[index] / sampleCount, Is.EqualTo(0.25).Within(0.008));
            }

            Assert.That(weightedCounts[0], Is.EqualTo(0));
            Assert.That((double)weightedCounts[1] / sampleCount, Is.EqualTo(0.1).Within(0.006));
            Assert.That((double)weightedCounts[2] / sampleCount, Is.EqualTo(0.3).Within(0.008));
            Assert.That((double)weightedCounts[3] / sampleCount, Is.EqualTo(0.6).Within(0.008));
        });
    }

    [Test]
    public void FillMatchesRepeatedSamples()
    {
        RandomGenerator fillGenerator = Create(505UL);
        RandomGenerator repeatedGenerator = Create(505UL);
        EmpiricalSampler filling = new EmpiricalSampler(fillGenerator, [1.0, 2.0, 3.0, 4.0], [1.0, 2.0, 3.0, 4.0]);
        EmpiricalSampler repeated = new EmpiricalSampler(repeatedGenerator, [1.0, 2.0, 3.0, 4.0], [1.0, 2.0, 3.0, 4.0]);
        double[] actual = new double[2000];
        double[] expected = new double[2000];
        filling.Fill(actual);
        for (int index = 0; index < expected.Length; index++)
        {
            expected[index] = repeated.Sample();
        }

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(repeatedGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void ObservationsAndWeightsAreDefensivelyCopied()
    {
        double[] equalObservations = [-3.0, 1.5, 8.0, 20.0];
        double[] weightedObservations = [-3.0, 1.5, 8.0, 20.0];
        double[] weights = [1.0, 2.0, 3.0, 4.0];
        EmpiricalSampler equalActual = EmpiricalSampler.CreateUnweighted(Create(606UL), equalObservations);
        EmpiricalSampler equalExpected = EmpiricalSampler.CreateUnweighted(Create(606UL), [-3.0, 1.5, 8.0, 20.0]);
        EmpiricalSampler weightedActual = new EmpiricalSampler(Create(707UL), weightedObservations, weights);
        EmpiricalSampler weightedExpected = new EmpiricalSampler(Create(707UL), [-3.0, 1.5, 8.0, 20.0], [1.0, 2.0, 3.0, 4.0]);
        Array.Fill(equalObservations, double.NaN);
        Array.Fill(weightedObservations, double.NaN);
        Array.Fill(weights, double.NaN);

        for (int index = 0; index < 1000; index++)
        {
            Assert.That(equalActual.Sample(), Is.EqualTo(equalExpected.Sample()));
            Assert.That(weightedActual.Sample(), Is.EqualTo(weightedExpected.Sample()));
        }
    }

    [Test]
    public void DeterministicSupportsDoNotConsumeRandomState()
    {
        RandomGenerator equalGenerator = Create(808UL);
        RandomGenerator weightedGenerator = Create(909UL);
        equalGenerator.NextBoolean();
        weightedGenerator.NextBoolean();
        byte[] equalBefore = equalGenerator.ExportState().ToBytes();
        byte[] weightedBefore = weightedGenerator.ExportState().ToBytes();
        EmpiricalSampler equal = EmpiricalSampler.CreateUnweighted(equalGenerator, [17.0]);
        EmpiricalSampler weighted = new EmpiricalSampler(weightedGenerator, [1.0, 2.0, 3.0], [0.0, 7.0, 0.0]);

        Assert.Multiple(() =>
        {
            Assert.That(equal.Sample(), Is.EqualTo(17.0));
            Assert.That(weighted.Sample(), Is.EqualTo(2.0));
            Assert.That(equalGenerator.ExportState().ToBytes(), Is.EqualTo(equalBefore));
            Assert.That(weightedGenerator.ExportState().ToBytes(), Is.EqualTo(weightedBefore));
        });
    }

    [Test]
    public void InvalidInputsAreRejectedWithoutConsumingState()
    {
        AssertArgument("generator", () => EmpiricalSampler.CreateUnweighted(NullReference.Value<global::random.IRandomSource>(), [1.0]));
        AssertArgument("generator", () => new EmpiricalSampler(NullReference.Value<global::random.IRandomSource>(), [1.0], [1.0]));
        RandomGenerator generator = Create(1001UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        AssertArgument("observations", () => EmpiricalSampler.CreateUnweighted(generator, NullReference.Value<double[]>()));
        AssertArgument("observations", () => EmpiricalSampler.CreateUnweighted(generator, []));
        AssertArgument("observations", () => EmpiricalSampler.CreateUnweighted(generator, [double.NaN]));
        AssertArgument("observations", () => new EmpiricalSampler(generator, [double.NegativeInfinity], [1.0]));
        AssertArgument("weights", () => new EmpiricalSampler(generator, [1.0], NullReference.Value<double[]>()));
        AssertArgument("weights", () => new EmpiricalSampler(generator, [1.0, 2.0], [1.0]));
        AssertArgument("weights", () => new EmpiricalSampler(generator, [1.0], [-1.0]));
        AssertArgument("weights", () => new EmpiricalSampler(generator, [1.0], [double.PositiveInfinity]));
        AssertArgument("weights", () => new EmpiricalSampler(generator, [1.0, 2.0], [0.0, 0.0]));
        Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
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
