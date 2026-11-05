namespace random.Tests;

public class StochasticProcessBehaviorCoverageTests
{
    [Test]
    public void FixedLengthBatchesReplayRepeatedScalarCallsInPathMajorOrder()
    {
        AssertBatch<long>(
            (sampler, destination) => sampler.FillIntegerRandomWalks(4L, 3L, 3, 7, destination),
            sampler => sampler.IntegerRandomWalk(4L, 3L, 7),
            3,
            8,
            101UL);
        AssertBatch<double>(
            (sampler, destination) => sampler.FillFloatingRandomWalks(-2.0, 0.25, 3, 7, destination),
            sampler => sampler.FloatingRandomWalk(-2.0, 0.25, 7),
            3,
            8,
            102UL);
        AssertBatch<double>(
            (sampler, destination) => sampler.FillBrownianIncrements(0.125, 3, 7, destination),
            sampler => sampler.BrownianIncrements(0.125, 7),
            3,
            7,
            103UL);
        AssertBatch<double>(
            (sampler, destination) => sampler.FillBrownianMotions(1.0, 0.2, 0.7, 0.25, 3, 7, destination),
            sampler => sampler.BrownianMotion(1.0, 0.2, 0.7, 0.25, 7),
            3,
            8,
            104UL);
        AssertBatch<double>(
            (sampler, destination) => sampler.FillBrownianBridges(-1.0, 3.0, 0.6, 0.25, 3, 7, destination),
            sampler => sampler.BrownianBridge(-1.0, 3.0, 0.6, 0.25, 7),
            3,
            8,
            105UL);
        AssertBatch<double>(
            (sampler, destination) => sampler.FillGeometricBrownianMotions(10.0, 0.03, 0.2, 0.25, 3, 7, destination),
            sampler => sampler.GeometricBrownianMotion(10.0, 0.03, 0.2, 0.25, 7),
            3,
            8,
            106UL);
        AssertBatch<double>(
            (sampler, destination) => sampler.FillOrnsteinUhlenbeckPaths(2.0, -1.0, 0.4, 0.5, 0.25, 3, 7, destination),
            sampler => sampler.OrnsteinUhlenbeck(2.0, -1.0, 0.4, 0.5, 0.25, 7),
            3,
            8,
            107UL);
        AssertBatch<long>(
            (sampler, destination) => sampler.FillPoissonCountPaths(2.5, 3.0, 3, 7, destination),
            sampler => sampler.PoissonCountPath(2.5, 3.0, 7),
            3,
            8,
            108UL);

        MarkovChain chain = new MarkovChain([0.2, 0.5, 0.3, 0.0, 1.0, 0.0, 0.7, 0.1, 0.2], 3);
        AssertBatch<int>(
            (sampler, destination) => sampler.FillMarkovChainPaths(chain, 0, 3, 7, destination),
            sampler => sampler.MarkovChainPath(chain, 0, 7),
            3,
            8,
            109UL);
    }

    [Test]
    public void FixedSeedIncrementsAndTransitionsRespectProcessDefinitions()
    {
        IStochasticProcessSampler integerSampler = new StochasticProcessSampler(Create(201UL));
        long[] integerWalk = integerSampler.IntegerRandomWalk(11L, 4L, 40);
        double[] floatingWalk = integerSampler.FloatingRandomWalk(-3.0, 0.5, 40);
        for (int step = 1; step < integerWalk.Length; step++)
        {
            Assert.That(Math.Abs(integerWalk[step] - integerWalk[step - 1]), Is.EqualTo(4L));
            Assert.That(Math.Abs(floatingWalk[step] - floatingWalk[step - 1]), Is.EqualTo(0.5));
        }

        IStochasticProcessSampler incrementSampler = new StochasticProcessSampler(Create(202UL));
        IStochasticProcessSampler motionSampler = new StochasticProcessSampler(Create(202UL));
        double[] increments = incrementSampler.BrownianIncrements(0.25, 30);
        double[] motion = motionSampler.BrownianMotion(2.0, 0.3, 0.8, 0.25, 30);
        for (int step = 0; step < increments.Length; step++)
        {
            double expectedChange = 0.3 * 0.25 + 0.8 * increments[step];
            Assert.That(motion[step + 1] - motion[step], Is.EqualTo(expectedChange).Within(2.0e-15));
        }

        MarkovChain chain = new MarkovChain([0.0, 0.4, 0.6, 0.25, 0.0, 0.75, 1.0, 0.0, 0.0], 3);
        int[] states = new StochasticProcessSampler(Create(203UL)).MarkovChainPath(chain, 0, 1000);
        for (int step = 1; step < states.Length; step++)
        {
            Assert.That(chain.TransitionProbability(states[step - 1], states[step]), Is.GreaterThan(0.0));
        }
    }

    [Test]
    public void EndpointsLayoutsAndPoissonHorizonAreObservableThroughTheFacade()
    {
        RandomGenerator generator = Create(301UL);
        IStochasticProcessSampler sampler = new StochasticProcessSampler(generator);
        double[] bridge = sampler.BrownianBridge(-2.0, 5.0, 0.7, 0.2, 9);
        long[] counts = sampler.PoissonCountPath(3.0, 2.0, 8);
        MarkovChain chain = new MarkovChain([0.3, 0.7, 0.6, 0.4], 2);
        int[] markov = sampler.MarkovChainPath(chain, 1, 9);

        byte[] beforePreview = generator.ExportState().ToBytes();
        int capacity = sampler.RequiredPoissonArrivalCapacity(4.0, 3.0);
        byte[] afterPreview = generator.ExportState().ToBytes();
        double[] arrivals = Enumerable.Repeat(-1.0, capacity + 2).ToArray();
        int written = sampler.PoissonArrivalsInto(4.0, 3.0, arrivals);

        Assert.Multiple(() =>
        {
            Assert.That(bridge, Has.Length.EqualTo(sampler.RequiredDiffusionPathOutputLength(9)));
            Assert.That(bridge[0], Is.EqualTo(-2.0));
            Assert.That(bridge[^1], Is.EqualTo(5.0));
            Assert.That(counts[0], Is.Zero);
            Assert.That(counts, Is.Ordered.Ascending);
            Assert.That(markov[0], Is.EqualTo(1));
            Assert.That(afterPreview, Is.EqualTo(beforePreview));
            Assert.That(generator.ExportState().ToBytes(), Is.Not.EqualTo(afterPreview));
            Assert.That(written, Is.EqualTo(capacity));
            Assert.That(arrivals.Take(written), Is.Ordered.Ascending);
            Assert.That(arrivals.Take(written), Is.All.GreaterThanOrEqualTo(0.0));
            Assert.That(arrivals.Take(written), Is.All.LessThan(3.0));
            Assert.That(arrivals.Skip(written), Is.All.EqualTo(-1.0));
        });
    }

    [Test]
    public void InvalidFacadeRequestsAreRejectedBeforeConsumptionOrDestinationMutation()
    {
        RandomGenerator generator = Create(401UL);
        generator.NextBoolean();
        IStochasticProcessSampler sampler = new StochasticProcessSampler(generator);
        MarkovChain chain = new MarkovChain([0.5, 0.5, 0.25, 0.75], 2);
        byte[] before = generator.ExportState().ToBytes();
        long[] integerDestination = [2L, 3L];
        double[] floatingDestination = [5.0, 7.0];
        double[] diffusionDestination = [11.0, 13.0];
        long[] countDestination = [17L, 19L];
        int[] markovDestination = [23, 29];

        AssertArgument("stepSize", () => sampler.IntegerRandomWalkInto(long.MaxValue, 1L, 1, integerDestination));
        AssertArgument("initialState", () => sampler.FloatingRandomWalkInto(double.NaN, 1.0, 1, floatingDestination));
        AssertArgument("timeStep", () => sampler.BrownianIncrementsInto(0.0, 2, diffusionDestination));
        AssertArgument("destination", () => sampler.BrownianMotionInto(0.0, 0.0, 1.0, 1.0, 2, diffusionDestination));
        AssertArgument("stepCount", () => sampler.BrownianBridgeInto(0.0, 1.0, 1.0, 1.0, 0, diffusionDestination.AsSpan(0, 1)));
        AssertArgument("initialState", () => sampler.GeometricBrownianMotionInto(0.0, 0.1, 0.2, 1.0, 1, diffusionDestination));
        AssertArgument("meanReversion", () => sampler.OrnsteinUhlenbeckInto(0.0, 1.0, -0.1, 1.0, 1.0, 1, diffusionDestination));
        AssertArgument("rate", () => sampler.PoissonArrivalsInto(PoissonSampler.MaximumMean, 2.0, Span<double>.Empty));
        AssertArgument("intervalCount", () => sampler.PoissonCountPathInto(1.0, 1.0, 0, countDestination));
        AssertArgument("chain", () => sampler.MarkovChainPath(NullReference.Value<global::random.MarkovChain>(), 0, 1));
        AssertArgument("initialState", () => sampler.MarkovChainPathInto(chain, 2, 1, markovDestination));

        Assert.Multiple(() =>
        {
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
            Assert.That(integerDestination, Is.EqualTo(new long[] { 2L, 3L }));
            Assert.That(floatingDestination, Is.EqualTo(new double[] { 5.0, 7.0 }));
            Assert.That(diffusionDestination, Is.EqualTo(new double[] { 11.0, 13.0 }));
            Assert.That(countDestination, Is.EqualTo(new long[] { 17L, 19L }));
            Assert.That(markovDestination, Is.EqualTo(new int[] { 23, 29 }));
        });
    }

    [Test]
    public void ProcessLimitsAndFiniteGeneratorExhaustionAreReportedConsistently()
    {
        IStochasticProcessSampler lengthSampler = new StochasticProcessSampler(Create(501UL));
        AssertArgument("stepCount", () => lengthSampler.RequiredRandomWalkOutputLength(int.MaxValue));
        AssertArgument("pathCount", () => lengthSampler.RequiredRandomWalkBatchOutputLength(int.MaxValue, 1));
        AssertArgument("pathCount", () => lengthSampler.RequiredBrownianIncrementBatchOutputLength(int.MaxValue, 2));
        AssertArgument("stepCount", () => lengthSampler.RequiredDiffusionPathOutputLength(int.MaxValue));
        AssertArgument("intervalCount", () => lengthSampler.RequiredPoissonCountOutputLength(int.MaxValue));
        AssertArgument("pathCount", () => lengthSampler.RequiredPoissonCountBatchOutputLength(int.MaxValue, 1));
        AssertArgument("pathCount", () => lengthSampler.RequiredMarkovPathBatchOutputLength(int.MaxValue, 1));

        IStochasticProcessSampler arrivalSampler = new StochasticProcessSampler(Create(502UL));
        Assert.Throws<RandomExhaustedException>(() => arrivalSampler.PoissonArrivalsInto(1.0e10, 1.0, new double[2]));

        IStochasticProcessSampler deterministicSampler = new StochasticProcessSampler(CreateExhausted());
        MarkovChain deterministicChain = new MarkovChain([0.0, 1.0, 1.0, 0.0], 2);
        Assert.Multiple(() =>
        {
            Assert.That(deterministicSampler.IntegerRandomWalk(7L, 0L, 3), Is.All.EqualTo(7L));
            Assert.That(deterministicSampler.BrownianMotion(2.0, 1.0, 0.0, 0.5, 2), Is.EqualTo(new double[] { 2.0, 2.5, 3.0 }));
            Assert.That(deterministicSampler.BrownianBridge(-1.0, 4.0, 1.0, 0.5, 1), Is.EqualTo(new double[] { -1.0, 4.0 }));
            Assert.That(deterministicSampler.PoissonArrivals(0.0, 2.0), Is.Empty);
            Assert.That(deterministicSampler.MarkovChainPath(deterministicChain, 0, 3), Is.EqualTo(new int[] { 0, 1, 0, 1 }));
        });

        MarkovChain randomChain = new MarkovChain([0.5, 0.5, 0.25, 0.75], 2);
        Assert.Throws<RandomExhaustedException>(() => new StochasticProcessSampler(CreateExhausted()).IntegerRandomWalk(0L, 1L, 1));
        Assert.Throws<RandomExhaustedException>(() => new StochasticProcessSampler(CreateExhausted()).BrownianIncrements(1.0, 1));
        Assert.Throws<RandomExhaustedException>(() => new StochasticProcessSampler(CreateExhausted()).RequiredPoissonArrivalCapacity(1.0, 1.0));
        Assert.Throws<RandomExhaustedException>(() => new StochasticProcessSampler(CreateExhausted()).MarkovChainPath(randomChain, 0, 1));
    }

    static void AssertBatch<T>(Action<IStochasticProcessSampler, T[]> fill, Func<IStochasticProcessSampler, T[]> single, int pathCount, int valuesPerPath, ulong seed)
    {
        RandomGenerator batchGenerator = Create(seed);
        RandomGenerator repeatedGenerator = Create(seed);
        IStochasticProcessSampler batchSampler = new StochasticProcessSampler(batchGenerator);
        IStochasticProcessSampler repeatedSampler = new StochasticProcessSampler(repeatedGenerator);
        T[] actual = new T[pathCount * valuesPerPath];
        T[] expected = new T[actual.Length];
        fill(batchSampler, actual);
        int path = 0;
        while (path < pathCount)
        {
            single(repeatedSampler).CopyTo(expected, path * valuesPerPath);
            path++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(batchGenerator.ExportState().ToBytes(), Is.EqualTo(repeatedGenerator.ExportState().ToBytes()));
        });
    }

    static RandomGenerator Create(ulong seed)
    {
        return RandomGenerator.CreateSplitMix64(seed, RandomMathMode.Portable);
    }

    static RandomGenerator CreateExhausted()
    {
        RandomFactory factory = new RandomFactory();
        ulong[] maximum = [ulong.MaxValue, ulong.MaxValue, ulong.MaxValue, ulong.MaxValue];
        RandomGenerator generator = factory.CreatePhilox4x64TenAtCounter([1UL, 2UL], maximum, RandomMathMode.Portable);
        int lane = 0;
        while (lane < 4)
        {
            generator.NextRawWord();
            lane++;
        }

        return generator;
    }

    static void AssertArgument(string expectedName, TestDelegate action)
    {
        RandomArgumentException error = Assert.Throws<RandomArgumentException>(action);
        Assert.That(error.ArgumentName(), Is.EqualTo(expectedName));
    }
}
