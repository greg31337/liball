namespace random.Tests;

public class StochasticProcessSamplerTests
{
    [Test]
    public void RequiredLengthsDescribeEveryFixedOutputLayout()
    {
        IStochasticProcessSampler sampler = new StochasticProcessSampler(Create(101UL));

        Assert.Multiple(() =>
        {
            Assert.That(sampler.RequiredRandomWalkOutputLength(5), Is.EqualTo(6));
            Assert.That(sampler.RequiredRandomWalkBatchOutputLength(3, 5), Is.EqualTo(18));
            Assert.That(sampler.RequiredBrownianIncrementOutputLength(5), Is.EqualTo(5));
            Assert.That(sampler.RequiredBrownianIncrementBatchOutputLength(3, 5), Is.EqualTo(15));
            Assert.That(sampler.RequiredDiffusionPathOutputLength(5), Is.EqualTo(6));
            Assert.That(sampler.RequiredDiffusionPathBatchOutputLength(3, 5), Is.EqualTo(18));
            Assert.That(sampler.RequiredPoissonCountOutputLength(5), Is.EqualTo(6));
            Assert.That(sampler.RequiredPoissonCountBatchOutputLength(3, 5), Is.EqualTo(18));
            Assert.That(sampler.RequiredMarkovPathOutputLength(5), Is.EqualTo(6));
            Assert.That(sampler.RequiredMarkovPathBatchOutputLength(3, 5), Is.EqualTo(18));
        });

        AssertArgument("stepCount", () => sampler.RequiredRandomWalkOutputLength(int.MaxValue));
        AssertArgument("pathCount", () => sampler.RequiredBrownianIncrementBatchOutputLength(int.MaxValue, 2));
        AssertArgument("intervalCount", () => sampler.RequiredPoissonCountOutputLength(0));
        AssertArgument("pathCount", () => sampler.RequiredMarkovPathBatchOutputLength(int.MaxValue, 1));
    }

    [Test]
    public void RandomWalkOperationsReplayTheImplementationLayerExactly()
    {
        RandomGenerator actualGenerator = Create(202UL);
        RandomGenerator expectedGenerator = Create(202UL);
        IStochasticProcessSampler sampler = new StochasticProcessSampler(actualGenerator);

        long[] integer = sampler.IntegerRandomWalk(7L, 3L, 11);
        long[] expectedInteger = RandomProcesses.RandomWalk(expectedGenerator, 7L, 3L, 11);
        long[] integerDestination = new long[sampler.RequiredRandomWalkOutputLength(9)];
        long[] expectedIntegerDestination = new long[integerDestination.Length];
        sampler.IntegerRandomWalkInto(-4L, 2L, 9, integerDestination);
        RandomProcesses.RandomWalk(expectedGenerator, -4L, 2L, 9, expectedIntegerDestination);
        long[] integerBatch = new long[sampler.RequiredRandomWalkBatchOutputLength(4, 7)];
        long[] expectedIntegerBatch = new long[integerBatch.Length];
        sampler.FillIntegerRandomWalks(12L, 5L, 4, 7, integerBatch);
        RandomProcesses.RandomWalks(expectedGenerator, 12L, 5L, 4, 7, expectedIntegerBatch);

        double[] floating = sampler.FloatingRandomWalk(-1.5, 0.25, 13);
        double[] expectedFloating = RandomProcesses.RandomWalk(expectedGenerator, -1.5, 0.25, 13);
        double[] floatingDestination = new double[sampler.RequiredRandomWalkOutputLength(8)];
        double[] expectedFloatingDestination = new double[floatingDestination.Length];
        sampler.FloatingRandomWalkInto(3.0, 0.5, 8, floatingDestination);
        RandomProcesses.RandomWalk(expectedGenerator, 3.0, 0.5, 8, expectedFloatingDestination);
        double[] floatingBatch = new double[sampler.RequiredRandomWalkBatchOutputLength(3, 6)];
        double[] expectedFloatingBatch = new double[floatingBatch.Length];
        sampler.FillFloatingRandomWalks(-2.0, 0.125, 3, 6, floatingBatch);
        RandomProcesses.RandomWalks(expectedGenerator, -2.0, 0.125, 3, 6, expectedFloatingBatch);

        Assert.Multiple(() =>
        {
            Assert.That(integer, Is.EqualTo(expectedInteger));
            Assert.That(integerDestination, Is.EqualTo(expectedIntegerDestination));
            Assert.That(integerBatch, Is.EqualTo(expectedIntegerBatch));
            Assert.That(floating, Is.EqualTo(expectedFloating));
            Assert.That(floatingDestination, Is.EqualTo(expectedFloatingDestination));
            Assert.That(floatingBatch, Is.EqualTo(expectedFloatingBatch));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(expectedGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void DiffusionOperationsReplayTheImplementationLayerExactly()
    {
        RandomGenerator actualGenerator = Create(303UL);
        RandomGenerator expectedGenerator = Create(303UL);
        IStochasticProcessSampler sampler = new StochasticProcessSampler(actualGenerator);

        double[] increments = sampler.BrownianIncrements(0.25, 7);
        double[] expectedIncrements = RandomProcesses.BrownianIncrements(expectedGenerator, 0.25, 7);
        double[] incrementDestination = new double[6];
        double[] expectedIncrementDestination = new double[6];
        sampler.BrownianIncrementsInto(0.5, 6, incrementDestination);
        RandomProcesses.BrownianIncrements(expectedGenerator, 0.5, 6, expectedIncrementDestination);
        double[] incrementBatch = new double[sampler.RequiredBrownianIncrementBatchOutputLength(3, 5)];
        double[] expectedIncrementBatch = new double[incrementBatch.Length];
        sampler.FillBrownianIncrements(0.125, 3, 5, incrementBatch);
        RandomProcesses.BrownianIncrements(expectedGenerator, 0.125, 3, 5, expectedIncrementBatch);

        double[] brownian = sampler.BrownianMotion(1.0, 0.2, 0.7, 0.25, 5);
        double[] expectedBrownian = RandomProcesses.BrownianMotion(expectedGenerator, 1.0, 0.2, 0.7, 0.25, 5);
        double[] brownianDestination = new double[5];
        double[] expectedBrownianDestination = new double[5];
        sampler.BrownianMotionInto(-1.0, -0.1, 0.5, 0.5, 4, brownianDestination);
        RandomProcesses.BrownianMotion(expectedGenerator, -1.0, -0.1, 0.5, 0.5, 4, expectedBrownianDestination);
        double[] brownianBatch = new double[sampler.RequiredDiffusionPathBatchOutputLength(2, 4)];
        double[] expectedBrownianBatch = new double[brownianBatch.Length];
        sampler.FillBrownianMotions(2.0, 0.0, 0.4, 0.25, 2, 4, brownianBatch);
        RandomProcesses.BrownianMotion(expectedGenerator, 2.0, 0.0, 0.4, 0.25, 2, 4, expectedBrownianBatch);

        double[] bridge = sampler.BrownianBridge(-2.0, 3.0, 0.8, 0.2, 5);
        double[] expectedBridge = RandomProcesses.BrownianBridge(expectedGenerator, -2.0, 3.0, 0.8, 0.2, 5);
        double[] bridgeDestination = new double[6];
        double[] expectedBridgeDestination = new double[6];
        sampler.BrownianBridgeInto(0.0, 1.0, 0.6, 0.25, 5, bridgeDestination);
        RandomProcesses.BrownianBridge(expectedGenerator, 0.0, 1.0, 0.6, 0.25, 5, expectedBridgeDestination);
        double[] bridgeBatch = new double[sampler.RequiredDiffusionPathBatchOutputLength(2, 4)];
        double[] expectedBridgeBatch = new double[bridgeBatch.Length];
        sampler.FillBrownianBridges(-1.0, 2.0, 0.5, 0.25, 2, 4, bridgeBatch);
        RandomProcesses.BrownianBridge(expectedGenerator, -1.0, 2.0, 0.5, 0.25, 2, 4, expectedBridgeBatch);

        double[] geometric = sampler.GeometricBrownianMotion(10.0, 0.03, 0.2, 0.25, 5);
        double[] expectedGeometric = RandomProcesses.GeometricBrownianMotion(expectedGenerator, 10.0, 0.03, 0.2, 0.25, 5);
        double[] geometricDestination = new double[5];
        double[] expectedGeometricDestination = new double[5];
        sampler.GeometricBrownianMotionInto(8.0, -0.02, 0.3, 0.5, 4, geometricDestination);
        RandomProcesses.GeometricBrownianMotion(expectedGenerator, 8.0, -0.02, 0.3, 0.5, 4, expectedGeometricDestination);
        double[] geometricBatch = new double[sampler.RequiredDiffusionPathBatchOutputLength(2, 4)];
        double[] expectedGeometricBatch = new double[geometricBatch.Length];
        sampler.FillGeometricBrownianMotions(12.0, 0.05, 0.25, 0.25, 2, 4, geometricBatch);
        RandomProcesses.GeometricBrownianMotion(expectedGenerator, 12.0, 0.05, 0.25, 0.25, 2, 4, expectedGeometricBatch);

        double[] ornstein = sampler.OrnsteinUhlenbeck(-1.0, 2.0, 0.6, 0.4, 0.25, 5);
        double[] expectedOrnstein = RandomProcesses.OrnsteinUhlenbeck(expectedGenerator, -1.0, 2.0, 0.6, 0.4, 0.25, 5);
        double[] ornsteinDestination = new double[5];
        double[] expectedOrnsteinDestination = new double[5];
        sampler.OrnsteinUhlenbeckInto(3.0, -2.0, 0.2, 0.7, 0.5, 4, ornsteinDestination);
        RandomProcesses.OrnsteinUhlenbeck(expectedGenerator, 3.0, -2.0, 0.2, 0.7, 0.5, 4, expectedOrnsteinDestination);
        double[] ornsteinBatch = new double[sampler.RequiredDiffusionPathBatchOutputLength(2, 4)];
        double[] expectedOrnsteinBatch = new double[ornsteinBatch.Length];
        sampler.FillOrnsteinUhlenbeckPaths(1.0, 0.0, 0.5, 0.3, 0.25, 2, 4, ornsteinBatch);
        RandomProcesses.OrnsteinUhlenbeck(expectedGenerator, 1.0, 0.0, 0.5, 0.3, 0.25, 2, 4, expectedOrnsteinBatch);

        Assert.Multiple(() =>
        {
            Assert.That(increments, Is.EqualTo(expectedIncrements));
            Assert.That(incrementDestination, Is.EqualTo(expectedIncrementDestination));
            Assert.That(incrementBatch, Is.EqualTo(expectedIncrementBatch));
            Assert.That(brownian, Is.EqualTo(expectedBrownian));
            Assert.That(brownianDestination, Is.EqualTo(expectedBrownianDestination));
            Assert.That(brownianBatch, Is.EqualTo(expectedBrownianBatch));
            Assert.That(bridge, Is.EqualTo(expectedBridge));
            Assert.That(bridgeDestination, Is.EqualTo(expectedBridgeDestination));
            Assert.That(bridgeBatch, Is.EqualTo(expectedBridgeBatch));
            Assert.That(geometric, Is.EqualTo(expectedGeometric));
            Assert.That(geometricDestination, Is.EqualTo(expectedGeometricDestination));
            Assert.That(geometricBatch, Is.EqualTo(expectedGeometricBatch));
            Assert.That(ornstein, Is.EqualTo(expectedOrnstein));
            Assert.That(ornsteinDestination, Is.EqualTo(expectedOrnsteinDestination));
            Assert.That(ornsteinBatch, Is.EqualTo(expectedOrnsteinBatch));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(expectedGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void PoissonAndMarkovOperationsReplayTheImplementationLayerExactly()
    {
        RandomGenerator actualGenerator = Create(404UL);
        RandomGenerator expectedGenerator = Create(404UL);
        IStochasticProcessSampler sampler = new StochasticProcessSampler(actualGenerator);

        int arrivalCapacity = sampler.RequiredPoissonArrivalCapacity(4.0, 3.0);
        int expectedArrivalCapacity = RandomProcesses.RequiredPoissonArrivalCapacity(expectedGenerator, 4.0, 3.0);
        double[] arrivals = sampler.PoissonArrivals(4.0, 3.0);
        double[] expectedArrivals = RandomProcesses.PoissonArrivals(expectedGenerator, 4.0, 3.0);
        int nextCapacity = sampler.RequiredPoissonArrivalCapacity(3.0, 2.0);
        int expectedNextCapacity = RandomProcesses.RequiredPoissonArrivalCapacity(expectedGenerator, 3.0, 2.0);
        double[] arrivalDestination = new double[nextCapacity];
        double[] expectedArrivalDestination = new double[expectedNextCapacity];
        int written = sampler.PoissonArrivalsInto(3.0, 2.0, arrivalDestination);
        int expectedWritten = RandomProcesses.PoissonArrivals(expectedGenerator, 3.0, 2.0, expectedArrivalDestination);

        long[] counts = sampler.PoissonCountPath(2.0, 4.0, 6);
        long[] expectedCounts = RandomProcesses.PoissonCounts(expectedGenerator, 2.0, 4.0, 6);
        long[] countDestination = new long[sampler.RequiredPoissonCountOutputLength(5)];
        long[] expectedCountDestination = new long[countDestination.Length];
        sampler.PoissonCountPathInto(1.5, 3.0, 5, countDestination);
        RandomProcesses.PoissonCounts(expectedGenerator, 1.5, 3.0, 5, expectedCountDestination);
        long[] countBatch = new long[sampler.RequiredPoissonCountBatchOutputLength(3, 4)];
        long[] expectedCountBatch = new long[countBatch.Length];
        sampler.FillPoissonCountPaths(2.5, 2.0, 3, 4, countBatch);
        RandomProcesses.PoissonCounts(expectedGenerator, 2.5, 2.0, 3, 4, expectedCountBatch);

        MarkovChain chain = new MarkovChain([0.25, 0.75, 0.6, 0.4], 2);
        int[] markov = sampler.MarkovChainPath(chain, 0, 8);
        int[] expectedMarkov = RandomProcesses.MarkovChain(expectedGenerator, chain, 0, 8);
        int[] markovDestination = new int[sampler.RequiredMarkovPathOutputLength(6)];
        int[] expectedMarkovDestination = new int[markovDestination.Length];
        sampler.MarkovChainPathInto(chain, 1, 6, markovDestination);
        RandomProcesses.MarkovChain(expectedGenerator, chain, 1, 6, expectedMarkovDestination);
        int[] markovBatch = new int[sampler.RequiredMarkovPathBatchOutputLength(3, 5)];
        int[] expectedMarkovBatch = new int[markovBatch.Length];
        sampler.FillMarkovChainPaths(chain, 0, 3, 5, markovBatch);
        RandomProcesses.MarkovChain(expectedGenerator, chain, 0, 3, 5, expectedMarkovBatch);

        Assert.Multiple(() =>
        {
            Assert.That(arrivalCapacity, Is.EqualTo(expectedArrivalCapacity));
            Assert.That(arrivals, Is.EqualTo(expectedArrivals));
            Assert.That(nextCapacity, Is.EqualTo(expectedNextCapacity));
            Assert.That(written, Is.EqualTo(expectedWritten));
            Assert.That(arrivalDestination, Is.EqualTo(expectedArrivalDestination));
            Assert.That(counts, Is.EqualTo(expectedCounts));
            Assert.That(countDestination, Is.EqualTo(expectedCountDestination));
            Assert.That(countBatch, Is.EqualTo(expectedCountBatch));
            Assert.That(markov, Is.EqualTo(expectedMarkov));
            Assert.That(markovDestination, Is.EqualTo(expectedMarkovDestination));
            Assert.That(markovBatch, Is.EqualTo(expectedMarkovBatch));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(expectedGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void InvalidConstructionAndDestinationRequestsAreAtomic()
    {
        AssertArgument("generator", () => new StochasticProcessSampler(NullReference.Value<global::random.RandomGenerator>()));

        RandomGenerator generator = Create(505UL);
        IStochasticProcessSampler sampler = new StochasticProcessSampler(generator);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        double[] destination = [2.0, 3.0];

        AssertArgument("destination", () => sampler.BrownianMotionInto(0.0, 0.0, 1.0, 1.0, 3, destination));

        Assert.Multiple(() =>
        {
            Assert.That(destination, Is.EqualTo(new double[] { 2.0, 3.0 }));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
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
