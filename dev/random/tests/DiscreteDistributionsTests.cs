namespace random.Tests;

public class DiscreteDistributionsTests
{
    delegate bool BoolScalarCall(RandomGenerator generator);
    delegate void BoolFillCall(RandomGenerator generator, Span<bool> destination);
    delegate int IntScalarCall(RandomGenerator generator);
    delegate void IntFillCall(RandomGenerator generator, Span<int> destination);
    delegate long LongScalarCall(RandomGenerator generator);
    delegate void LongFillCall(RandomGenerator generator, Span<long> destination);

    [Test]
    public void EveryScalarFacadeMethodMatchesItsReusableSampler()
    {
        double[] probabilities = [0.0, 1.0, 3.0, 0.0, 2.0];
        AssertBoolScalar(generator => DiscreteDistributions.Bernoulli(generator, 0.37), generator => new BernoulliSampler(generator, 0.37).Sample());
        AssertIntScalar(generator => DiscreteDistributions.Rademacher(generator), generator => new RademacherSampler(generator).Sample());
        AssertIntScalar(generator => DiscreteDistributions.DiscreteUniform(generator, -17, 23), generator => new DiscreteUniformSampler(generator, -17, 23).Sample());
        AssertIntScalar(generator => DiscreteDistributions.Categorical(generator, probabilities), generator => new CategoricalSampler(generator, probabilities).Sample());
        AssertIntScalar(generator => DiscreteDistributions.Binomial(generator, 47, 0.37), generator => new BinomialSampler(generator, 47, 0.37).Sample());
        AssertIntScalar(generator => DiscreteDistributions.BetaBinomial(generator, 47, 1.5, 2.5), generator => new BetaBinomialSampler(generator, 47, 1.5, 2.5).Sample());
        AssertLongScalar(generator => DiscreteDistributions.GeometricFailures(generator, 0.37), generator => new GeometricSampler(generator, 0.37).SampleFailures());
        AssertLongScalar(generator => DiscreteDistributions.GeometricTrials(generator, 0.37), generator => new GeometricSampler(generator, 0.37).SampleTrials());
        AssertLongScalar(generator => DiscreteDistributions.NegativeBinomial(generator, 3.5, 0.37), generator => new NegativeBinomialSampler(generator, 3.5, 0.37).Sample());
        AssertLongScalar(generator => DiscreteDistributions.Poisson(generator, 17.5), generator => new PoissonSampler(generator, 17.5).Sample());
        AssertIntScalar(generator => DiscreteDistributions.Hypergeometric(generator, 100, 37, 29), generator => new HypergeometricSampler(generator, 100, 37, 29).Sample());
        AssertLongScalar(generator => DiscreteDistributions.LogSeries(generator, 0.73), generator => new LogSeriesSampler(generator, 0.73).Sample());
        AssertLongScalar(generator => DiscreteDistributions.Zipf(generator, 2.25), generator => new ZipfSampler(generator, 2.25).Sample());
        AssertLongScalar(generator => DiscreteDistributions.ConwayMaxwellPoisson(generator, 4.0, 1.7), generator => new ConwayMaxwellPoissonSampler(generator, 4.0, 1.7).Sample());
    }

    [Test]
    public void EveryBulkFacadeMethodMatchesRepeatedScalarFacadeCalls()
    {
        double[] probabilities = [0.0, 1.0, 3.0, 0.0, 2.0];
        AssertBoolFill((generator, values) => DiscreteDistributions.FillBernoulli(generator, 0.37, values), generator => DiscreteDistributions.Bernoulli(generator, 0.37));
        AssertIntFill((generator, values) => DiscreteDistributions.FillRademacher(generator, values), generator => DiscreteDistributions.Rademacher(generator));
        AssertIntFill((generator, values) => DiscreteDistributions.FillDiscreteUniform(generator, -17, 23, values), generator => DiscreteDistributions.DiscreteUniform(generator, -17, 23));
        AssertIntFill((generator, values) => DiscreteDistributions.FillCategorical(generator, probabilities, values), generator => DiscreteDistributions.Categorical(generator, probabilities));
        AssertIntFill((generator, values) => DiscreteDistributions.FillBinomial(generator, 47, 0.37, values), generator => DiscreteDistributions.Binomial(generator, 47, 0.37));
        AssertIntFill((generator, values) => DiscreteDistributions.FillBetaBinomial(generator, 47, 1.5, 2.5, values), generator => DiscreteDistributions.BetaBinomial(generator, 47, 1.5, 2.5));
        AssertLongFill((generator, values) => DiscreteDistributions.FillGeometricFailures(generator, 0.37, values), generator => DiscreteDistributions.GeometricFailures(generator, 0.37));
        AssertLongFill((generator, values) => DiscreteDistributions.FillGeometricTrials(generator, 0.37, values), generator => DiscreteDistributions.GeometricTrials(generator, 0.37));
        AssertLongFill((generator, values) => DiscreteDistributions.FillNegativeBinomial(generator, 3.5, 0.37, values), generator => DiscreteDistributions.NegativeBinomial(generator, 3.5, 0.37));
        AssertLongFill((generator, values) => DiscreteDistributions.FillPoisson(generator, 17.5, values), generator => DiscreteDistributions.Poisson(generator, 17.5));
        AssertIntFill((generator, values) => DiscreteDistributions.FillHypergeometric(generator, 100, 37, 29, values), generator => DiscreteDistributions.Hypergeometric(generator, 100, 37, 29));
        AssertLongFill((generator, values) => DiscreteDistributions.FillLogSeries(generator, 0.73, values), generator => DiscreteDistributions.LogSeries(generator, 0.73));
        AssertLongFill((generator, values) => DiscreteDistributions.FillZipf(generator, 2.25, values), generator => DiscreteDistributions.Zipf(generator, 2.25));
        AssertLongFill((generator, values) => DiscreteDistributions.FillConwayMaxwellPoisson(generator, 4.0, 1.7, values), generator => DiscreteDistributions.ConwayMaxwellPoisson(generator, 4.0, 1.7));
    }

    [Test]
    public void MultivariateHypergeometricFacadePreservesVectorBoundsAndTotal()
    {
        int[] categoryCounts = [11, 0, 23, 7, 19];
        const int draws = 31;
        RandomGenerator facadeGenerator = Create(61UL);
        RandomGenerator samplerGenerator = Create(61UL);
        MultivariateHypergeometricSampler sampler = new MultivariateHypergeometricSampler(samplerGenerator, categoryCounts, draws);

        int[] actual = DiscreteDistributions.MultivariateHypergeometric(facadeGenerator, categoryCounts, draws);
        int[] expected = sampler.Sample();

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actual.Sum(), Is.EqualTo(draws));
            for (int index = 0; index < actual.Length; index++)
            {
                Assert.That(actual[index], Is.InRange(0, categoryCounts[index]));
            }

            Assert.That(facadeGenerator.ExportState().ToBytes(), Is.EqualTo(samplerGenerator.ExportState().ToBytes()));
        });

        RandomGenerator destinationGenerator = Create(62UL);
        RandomGenerator allocatingGenerator = Create(62UL);
        int[] destination = new int[categoryCounts.Length];
        DiscreteDistributions.MultivariateHypergeometric(destinationGenerator, categoryCounts, draws, destination);
        int[] allocated = DiscreteDistributions.MultivariateHypergeometric(allocatingGenerator, categoryCounts, draws);

        Assert.Multiple(() =>
        {
            Assert.That(destination, Is.EqualTo(allocated));
            Assert.That(destination.Sum(), Is.EqualTo(draws));
            Assert.That(destinationGenerator.ExportState().ToBytes(), Is.EqualTo(allocatingGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void MultinomialFacadePreservesNonnegativeExactTrialTotals()
    {
        double[] probabilities = [0.0, 2.0, 3.0, 0.0, 5.0];
        const int trials = 73;
        RandomGenerator facadeGenerator = Create(71UL);
        RandomGenerator samplerGenerator = Create(71UL);
        MultinomialSampler sampler = new MultinomialSampler(samplerGenerator, trials, probabilities);

        int[] actual = DiscreteDistributions.Multinomial(facadeGenerator, trials, probabilities);
        int[] expected = sampler.Sample();

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actual, Is.All.GreaterThanOrEqualTo(0));
            Assert.That(actual.Sum(), Is.EqualTo(trials));
            Assert.That(actual[0], Is.Zero);
            Assert.That(actual[3], Is.Zero);
            Assert.That(facadeGenerator.ExportState().ToBytes(), Is.EqualTo(samplerGenerator.ExportState().ToBytes()));
        });

        RandomGenerator destinationGenerator = Create(72UL);
        RandomGenerator allocatingGenerator = Create(72UL);
        int[] destination = new int[probabilities.Length];
        DiscreteDistributions.Multinomial(destinationGenerator, trials, probabilities, destination);
        int[] allocated = DiscreteDistributions.Multinomial(allocatingGenerator, trials, probabilities);

        Assert.Multiple(() =>
        {
            Assert.That(destination, Is.EqualTo(allocated));
            Assert.That(destination.Sum(), Is.EqualTo(trials));
            Assert.That(destinationGenerator.ExportState().ToBytes(), Is.EqualTo(allocatingGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void FacadeForwardsValidationWithoutConsumingStateOrWritingVectors()
    {
        RandomGenerator generator = Create(81UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        int[] vector = [91, 92];

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<RandomArgumentException>(() => DiscreteDistributions.Bernoulli(NullReference.Value<global::random.IRandomSource>(), 0.5))?.ArgumentName(), Is.EqualTo("generator"));
            Assert.That(Assert.Throws<RandomArgumentException>(() => DiscreteDistributions.Categorical(generator, [0.0, 0.0]))?.ArgumentName(), Is.EqualTo("probabilities"));
            Assert.That(Assert.Throws<RandomArgumentException>(() => DiscreteDistributions.GeometricFailures(generator, 0.0))?.ArgumentName(), Is.EqualTo("probability"));
            Assert.That(Assert.Throws<RandomArgumentException>(() => DiscreteDistributions.ConwayMaxwellPoisson(generator, 1.0, 0.0))?.ArgumentName(), Is.EqualTo("rate"));
            Assert.That(Assert.Throws<RandomArgumentException>(() => DiscreteDistributions.Multinomial(generator, 10, [1.0, 1.0, 1.0], vector))?.ArgumentName(), Is.EqualTo("destination"));
        });

        Assert.Multiple(() =>
        {
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
            Assert.That(vector, Is.EqualTo(new int[] { 91, 92 }));
        });
    }

    static void AssertBoolScalar(BoolScalarCall facade, BoolScalarCall sampler)
    {
        RandomGenerator facadeGenerator = Create(11UL);
        RandomGenerator samplerGenerator = Create(11UL);
        for (int index = 0; index < 64; index++)
        {
            Assert.That(facade(facadeGenerator), Is.EqualTo(sampler(samplerGenerator)));
        }

        Assert.That(facadeGenerator.ExportState().ToBytes(), Is.EqualTo(samplerGenerator.ExportState().ToBytes()));
    }

    static void AssertIntScalar(IntScalarCall facade, IntScalarCall sampler)
    {
        RandomGenerator facadeGenerator = Create(12UL);
        RandomGenerator samplerGenerator = Create(12UL);
        for (int index = 0; index < 64; index++)
        {
            Assert.That(facade(facadeGenerator), Is.EqualTo(sampler(samplerGenerator)));
        }

        Assert.That(facadeGenerator.ExportState().ToBytes(), Is.EqualTo(samplerGenerator.ExportState().ToBytes()));
    }

    static void AssertLongScalar(LongScalarCall facade, LongScalarCall sampler)
    {
        RandomGenerator facadeGenerator = Create(13UL);
        RandomGenerator samplerGenerator = Create(13UL);
        for (int index = 0; index < 64; index++)
        {
            Assert.That(facade(facadeGenerator), Is.EqualTo(sampler(samplerGenerator)));
        }

        Assert.That(facadeGenerator.ExportState().ToBytes(), Is.EqualTo(samplerGenerator.ExportState().ToBytes()));
    }

    static void AssertBoolFill(BoolFillCall fill, BoolScalarCall scalar)
    {
        RandomGenerator fillGenerator = Create(21UL);
        RandomGenerator scalarGenerator = Create(21UL);
        bool[] actual = new bool[65];
        bool[] expected = new bool[actual.Length];
        byte[] before = fillGenerator.ExportState().ToBytes();

        fill(fillGenerator, Span<bool>.Empty);
        Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(before));
        fill(fillGenerator, actual);
        for (int index = 0; index < expected.Length; index++)
        {
            expected[index] = scalar(scalarGenerator);
        }

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(scalarGenerator.ExportState().ToBytes()));
        });
    }

    static void AssertIntFill(IntFillCall fill, IntScalarCall scalar)
    {
        RandomGenerator fillGenerator = Create(22UL);
        RandomGenerator scalarGenerator = Create(22UL);
        int[] actual = new int[65];
        int[] expected = new int[actual.Length];
        byte[] before = fillGenerator.ExportState().ToBytes();

        fill(fillGenerator, Span<int>.Empty);
        Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(before));
        fill(fillGenerator, actual);
        for (int index = 0; index < expected.Length; index++)
        {
            expected[index] = scalar(scalarGenerator);
        }

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(scalarGenerator.ExportState().ToBytes()));
        });
    }

    static void AssertLongFill(LongFillCall fill, LongScalarCall scalar)
    {
        RandomGenerator fillGenerator = Create(23UL);
        RandomGenerator scalarGenerator = Create(23UL);
        long[] actual = new long[65];
        long[] expected = new long[actual.Length];
        byte[] before = fillGenerator.ExportState().ToBytes();

        fill(fillGenerator, Span<long>.Empty);
        Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(before));
        fill(fillGenerator, actual);
        for (int index = 0; index < expected.Length; index++)
        {
            expected[index] = scalar(scalarGenerator);
        }

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(scalarGenerator.ExportState().ToBytes()));
        });
    }

    static RandomGenerator Create(ulong seed)
    {
        return RandomGenerator.CreateSplitMix64(seed, RandomMathMode.Portable);
    }
}
