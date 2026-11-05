namespace random.Tests;

public class DiscreteDistributionStatisticalTests
{
    [Test]
    public void EveryDiscreteSamplerStaysWithinItsSupportForFixedSeeds()
    {
        BernoulliSampler bernoulli = new BernoulliSampler(Create(101UL), 0.3);
        RademacherSampler rademacher = new RademacherSampler(Create(102UL));
        DiscreteUniformSampler uniform = new DiscreteUniformSampler(Create(103UL), -3, 4);
        CategoricalSampler categorical = new CategoricalSampler(Create(104UL), [1.0, 0.0, 2.0, 5.0]);
        BinomialSampler binomial = new BinomialSampler(Create(105UL), 17, 0.4);
        BetaBinomialSampler betaBinomial = new BetaBinomialSampler(Create(106UL), 17, 2.0, 5.0);
        GeometricSampler geometric = new GeometricSampler(Create(107UL), 0.4);
        NegativeBinomialSampler negativeBinomial = new NegativeBinomialSampler(Create(108UL), 4.5, 0.4);
        PoissonSampler poisson = new PoissonSampler(Create(109UL), 4.0);
        HypergeometricSampler hypergeometric = new HypergeometricSampler(Create(110UL), 20, 7, 9);
        LogSeriesSampler logSeries = new LogSeriesSampler(Create(111UL), 0.5);
        ZipfSampler zipf = new ZipfSampler(Create(112UL), 2.0);
        ConwayMaxwellPoissonSampler conwayMaxwellPoisson = new ConwayMaxwellPoissonSampler(Create(113UL), 4.0, 1.5);

        for (int index = 0; index < 1024; index++)
        {
            bool bernoulliValue = bernoulli.Sample();
            int rademacherValue = rademacher.Sample();
            int uniformValue = uniform.Sample();
            int categoricalValue = categorical.Sample();
            int binomialValue = binomial.Sample();
            int betaBinomialValue = betaBinomial.Sample();
            long failures = geometric.SampleFailures();
            long trials = geometric.SampleTrials();
            long negativeBinomialValue = negativeBinomial.Sample();
            long poissonValue = poisson.Sample();
            int hypergeometricValue = hypergeometric.Sample();
            long logSeriesValue = logSeries.Sample();
            long zipfValue = zipf.Sample();
            long conwayMaxwellPoissonValue = conwayMaxwellPoisson.Sample();

            Assert.Multiple(() =>
            {
                Assert.That(bernoulliValue, Is.TypeOf<bool>());
                Assert.That(rademacherValue, Is.EqualTo(-1).Or.EqualTo(1));
                Assert.That(uniformValue, Is.InRange(-3, 4));
                Assert.That(categoricalValue, Is.EqualTo(0).Or.EqualTo(2).Or.EqualTo(3));
                Assert.That(binomialValue, Is.InRange(0, 17));
                Assert.That(betaBinomialValue, Is.InRange(0, 17));
                Assert.That(failures, Is.GreaterThanOrEqualTo(0L));
                Assert.That(trials, Is.GreaterThanOrEqualTo(1L));
                Assert.That(negativeBinomialValue, Is.GreaterThanOrEqualTo(0L));
                Assert.That(poissonValue, Is.GreaterThanOrEqualTo(0L));
                Assert.That(hypergeometricValue, Is.InRange(0, 7));
                Assert.That(logSeriesValue, Is.GreaterThanOrEqualTo(1L));
                Assert.That(zipfValue, Is.GreaterThanOrEqualTo(1L));
                Assert.That(conwayMaxwellPoissonValue, Is.GreaterThanOrEqualTo(0L));
            });
        }
    }

    [Test]
    public void FixedSeedMassBinsMatchClosedFormProbabilitiesWithWideBounds()
    {
        const int sampleCount = 50000;
        BernoulliSampler bernoulli = new BernoulliSampler(Create(201UL), 0.3);
        AssertMassBins("Bernoulli", () => bernoulli.Sample() ? 1L : 0L, value => (int)value, [0.7, 0.3], sampleCount);

        RademacherSampler rademacher = new RademacherSampler(Create(202UL));
        AssertMassBins("Rademacher", () => rademacher.Sample(), value => value < 0 ? 0 : 1, [0.5, 0.5], sampleCount);

        DiscreteUniformSampler uniform = new DiscreteUniformSampler(Create(203UL), 0, 4);
        AssertMassBins("uniform", () => uniform.Sample(), value => (int)value, [0.2, 0.2, 0.2, 0.2, 0.2], sampleCount);

        CategoricalSampler categorical = new CategoricalSampler(Create(204UL), [1.0, 2.0, 0.0, 5.0]);
        AssertMassBins("categorical", () => categorical.Sample(), value => (int)value, [0.125, 0.25, 0.0, 0.625], sampleCount);

        BinomialSampler binomial = new BinomialSampler(Create(205UL), 4, 0.5);
        AssertMassBins("binomial", () => binomial.Sample(), value => (int)value, [0.0625, 0.25, 0.375, 0.25, 0.0625], sampleCount);

        PoissonSampler poisson = new PoissonSampler(Create(206UL), 2.0);
        double poissonZero = Math.Exp(-2.0);
        AssertMassBins(
            "Poisson",
            poisson.Sample,
            value => value < 5L ? (int)value : 5,
            [
                poissonZero,
                2.0 * poissonZero,
                2.0 * poissonZero,
                4.0 * poissonZero / 3.0,
                2.0 * poissonZero / 3.0,
                1.0 - 7.0 * poissonZero
            ],
            sampleCount);

        GeometricSampler geometric = new GeometricSampler(Create(207UL), 0.4);
        AssertMassBins(
            "geometric failures",
            geometric.SampleFailures,
            value => value < 5L ? (int)value : 5,
            [0.4, 0.24, 0.144, 0.0864, 0.05184, 0.07776],
            sampleCount);

        LogSeriesSampler logSeries = new LogSeriesSampler(Create(208UL), 0.5);
        double logSeriesScale = -1.0 / Math.Log(0.5);
        double[] logSeriesMass = new double[5];
        double logSeriesAssigned = 0.0;
        for (int value = 1; value <= 4; value++)
        {
            logSeriesMass[value - 1] = logSeriesScale * Math.Pow(0.5, value) / value;
            logSeriesAssigned += logSeriesMass[value - 1];
        }

        logSeriesMass[4] = 1.0 - logSeriesAssigned;
        AssertMassBins("logarithmic series", logSeries.Sample, value => value <= 4L ? (int)value - 1 : 4, logSeriesMass, sampleCount);

        ZipfSampler zipf = new ZipfSampler(Create(209UL), 2.0);
        double zipfNormalizer = Math.PI * Math.PI / 6.0;
        double[] zipfMass = new double[5];
        double zipfAssigned = 0.0;
        for (int value = 1; value <= 4; value++)
        {
            zipfMass[value - 1] = 1.0 / (value * value * zipfNormalizer);
            zipfAssigned += zipfMass[value - 1];
        }

        zipfMass[4] = 1.0 - zipfAssigned;
        AssertMassBins("Zipf", zipf.Sample, value => value <= 4L ? (int)value - 1 : 4, zipfMass, sampleCount);
    }

    [Test]
    public void VectorSamplersPreserveTotalsAndCategoryBoundsForEveryFixedSeedDraw()
    {
        const int sampleCount = 2048;
        int trials = 37;
        MultinomialSampler multinomial = new MultinomialSampler(Create(301UL), trials, [1.0, 0.0, 2.0, 5.0]);
        int[] categoryCounts = [7, 0, 11, 5];
        int draws = 13;
        MultivariateHypergeometricSampler multivariateHypergeometric = new MultivariateHypergeometricSampler(Create(302UL), categoryCounts, draws);

        for (int index = 0; index < sampleCount; index++)
        {
            int[] multinomialValues = multinomial.Sample();
            int[] hypergeometricValues = multivariateHypergeometric.Sample();
            Assert.Multiple(() =>
            {
                Assert.That(multinomialValues, Has.All.GreaterThanOrEqualTo(0));
                Assert.That(multinomialValues[1], Is.Zero);
                Assert.That(Sum(multinomialValues), Is.EqualTo((long)trials));
                Assert.That(hypergeometricValues, Has.All.GreaterThanOrEqualTo(0));
                Assert.That(hypergeometricValues[1], Is.Zero);
                Assert.That(Sum(hypergeometricValues), Is.EqualTo((long)draws));
                for (int category = 0; category < categoryCounts.Length; category++)
                {
                    Assert.That(hypergeometricValues[category], Is.LessThanOrEqualTo(categoryCounts[category]));
                }
            });
        }
    }

    [Test]
    public void MaximumCapacityCasesStayRepresentableOrReportExplicitExhaustion()
    {
        BinomialSampler binomial = new BinomialSampler(Create(401UL), int.MaxValue, 0.5);
        BetaBinomialSampler betaBinomial = new BetaBinomialSampler(Create(402UL), int.MaxValue, 2.0, 5.0);
        PoissonSampler poisson = new PoissonSampler(Create(403UL), PoissonSampler.MaximumMean);
        MultinomialSampler multinomial = new MultinomialSampler(Create(404UL), int.MaxValue, [1.0, 2.0, 3.0]);
        int maximumDraws = 1073741824;
        int[] maximumPopulation = [int.MaxValue - 10, 10];
        MultivariateHypergeometricSampler multivariateHypergeometric = new MultivariateHypergeometricSampler(
            Create(405UL),
            maximumPopulation,
            maximumDraws);

        for (int index = 0; index < 32; index++)
        {
            int[] multinomialValues = multinomial.Sample();
            int[] hypergeometricValues = multivariateHypergeometric.Sample();
            Assert.Multiple(() =>
            {
                Assert.That(binomial.Sample(), Is.InRange(0, int.MaxValue));
                Assert.That(betaBinomial.Sample(), Is.InRange(0, int.MaxValue));
                Assert.That(poisson.Sample(), Is.InRange(0L, long.MaxValue));
                Assert.That(Sum(multinomialValues), Is.EqualTo((long)int.MaxValue));
                Assert.That(Sum(hypergeometricValues), Is.EqualTo((long)maximumDraws));
                Assert.That(hypergeometricValues[0], Is.LessThanOrEqualTo(maximumPopulation[0]));
                Assert.That(hypergeometricValues[1], Is.LessThanOrEqualTo(maximumPopulation[1]));
            });
        }

        GeometricSampler geometric = new GeometricSampler(Create(406UL), double.Epsilon);
        NegativeBinomialSampler negativeBinomial = new NegativeBinomialSampler(Create(407UL), 2.0, double.Epsilon);
        Assert.Multiple(() =>
        {
            Assert.Throws<RandomExhaustedException>(() => geometric.SampleFailures());
            Assert.Throws<RandomExhaustedException>(() => negativeBinomial.Sample());
        });
    }

    [Test]
    public void RepresentativeScalarFillsReplayExactlyAcrossAlgorithmRegimes()
    {
        AssertIntFillReplay(
            generator => new BinomialSampler(generator, 1000, 0.37),
            (sampler, values) => sampler.Fill(values),
            sampler => sampler.Sample(),
            501UL);
        AssertIntFillReplay(
            generator => new BetaBinomialSampler(generator, 100, 2.0, 5.0),
            (sampler, values) => sampler.Fill(values),
            sampler => sampler.Sample(),
            502UL);
        AssertIntFillReplay(
            generator => new HypergeometricSampler(generator, 1000, 400, 300),
            (sampler, values) => sampler.Fill(values),
            sampler => sampler.Sample(),
            503UL);
        AssertLongFillReplay(
            generator => new GeometricSampler(generator, 0.37),
            (sampler, values) => sampler.FillFailures(values),
            sampler => sampler.SampleFailures(),
            504UL);
        AssertLongFillReplay(
            generator => new NegativeBinomialSampler(generator, 40.0, 0.4),
            (sampler, values) => sampler.Fill(values),
            sampler => sampler.Sample(),
            505UL);
        AssertLongFillReplay(
            generator => new PoissonSampler(generator, 1000.0),
            (sampler, values) => sampler.Fill(values),
            sampler => sampler.Sample(),
            506UL);
        AssertLongFillReplay(
            generator => new LogSeriesSampler(generator, 0.8),
            (sampler, values) => sampler.Fill(values),
            sampler => sampler.Sample(),
            507UL);
        AssertLongFillReplay(
            generator => new ZipfSampler(generator, 2.0),
            (sampler, values) => sampler.Fill(values),
            sampler => sampler.Sample(),
            508UL);
        AssertLongFillReplay(
            generator => new ConwayMaxwellPoissonSampler(generator, 4.0, 1.5),
            (sampler, values) => sampler.Fill(values),
            sampler => sampler.Sample(),
            509UL);
    }

    static void AssertMassBins(
        string name,
        Func<long> sample,
        Func<long, int> selectBin,
        double[] expectedMass,
        int sampleCount)
    {
        int[] counts = new int[expectedMass.Length];
        for (int index = 0; index < sampleCount; index++)
        {
            counts[selectBin(sample())]++;
        }

        for (int bin = 0; bin < counts.Length; bin++)
        {
            if (expectedMass[bin] == 0.0)
            {
                Assert.That(counts[bin], Is.Zero, $"{name} bin {bin}");
                continue;
            }

            double expected = sampleCount * expectedMass[bin];
            double tolerance = Math.Max(200.0, expected * 0.12);
            Assert.That(counts[bin], Is.EqualTo(expected).Within(tolerance), $"{name} bin {bin}");
        }
    }

    static void AssertIntFillReplay<TSampler>(
        Func<RandomGenerator, TSampler> createSampler,
        Action<TSampler, int[]> fill,
        Func<TSampler, int> sample,
        ulong seed)
    {
        RandomGenerator fillGenerator = Create(seed);
        RandomGenerator scalarGenerator = Create(seed);
        TSampler fillSampler = createSampler(fillGenerator);
        TSampler scalarSampler = createSampler(scalarGenerator);
        int[] actual = new int[257];
        int[] expected = new int[actual.Length];
        fill(fillSampler, actual);
        for (int index = 0; index < expected.Length; index++)
        {
            expected[index] = sample(scalarSampler);
        }

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(scalarGenerator.ExportState().ToBytes()));
        });
    }

    static void AssertLongFillReplay<TSampler>(
        Func<RandomGenerator, TSampler> createSampler,
        Action<TSampler, long[]> fill,
        Func<TSampler, long> sample,
        ulong seed)
    {
        RandomGenerator fillGenerator = Create(seed);
        RandomGenerator scalarGenerator = Create(seed);
        TSampler fillSampler = createSampler(fillGenerator);
        TSampler scalarSampler = createSampler(scalarGenerator);
        long[] actual = new long[257];
        long[] expected = new long[actual.Length];
        fill(fillSampler, actual);
        for (int index = 0; index < expected.Length; index++)
        {
            expected[index] = sample(scalarSampler);
        }

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(scalarGenerator.ExportState().ToBytes()));
        });
    }

    static long Sum(int[] values)
    {
        long sum = 0L;
        for (int index = 0; index < values.Length; index++)
        {
            sum += values[index];
        }

        return sum;
    }

    static RandomGenerator Create(ulong seed)
    {
        return RandomGenerator.CreateSplitMix64(seed, RandomMathMode.Portable);
    }
}
