using System;

namespace random;

/// <summary>
/// Generates stochastic-process paths from a retained deterministic generator.
/// </summary>
/// <remarks>
/// Fixed-length path batches are path-major. Random-walk, diffusion, Poisson-count,
/// and Markov paths include their initial state; Brownian-increment paths contain
/// only increments. The retained generator is not safe for concurrent use without
/// external synchronization.
/// </remarks>
public class StochasticProcessSampler : IStochasticProcessSampler
{
    RandomGenerator generator;

    /// <summary>Creates a stochastic-process sampler.</summary>
    /// <param name="generator">The deterministic generator that supplies random values.</param>
    /// <exception cref="RandomArgumentException">The generator is null.</exception>
    public StochasticProcessSampler(RandomGenerator generator)
    {
        Guard.NotNull(generator, "generator");
        this.generator = generator;
    }

    /// <summary>Returns the storage required for one integer or floating random walk.</summary>
    /// <param name="stepCount">The nonnegative number of steps.</param>
    /// <returns>Step count plus one.</returns>
    public int RequiredRandomWalkOutputLength(int stepCount)
    {
        return RandomProcesses.RandomWalkOutputLength(stepCount);
    }

    /// <summary>Returns the storage required for a path-major random-walk batch.</summary>
    /// <param name="pathCount">The nonnegative path count.</param>
    /// <param name="stepCount">The nonnegative step count per path.</param>
    /// <returns>Path count multiplied by step count plus one.</returns>
    public int RequiredRandomWalkBatchOutputLength(int pathCount, int stepCount)
    {
        return RandomProcesses.RandomWalkBatchOutputLength(pathCount, stepCount);
    }

    /// <summary>Returns the storage required for one Brownian-increment path.</summary>
    /// <param name="stepCount">The nonnegative increment count.</param>
    /// <returns>The increment count.</returns>
    public int RequiredBrownianIncrementOutputLength(int stepCount)
    {
        return RandomProcesses.BrownianIncrementOutputLength(stepCount);
    }

    /// <summary>Returns the storage required for a path-major Brownian-increment batch.</summary>
    /// <param name="pathCount">The nonnegative path count.</param>
    /// <param name="stepCount">The nonnegative increment count per path.</param>
    /// <returns>Path count multiplied by increment count.</returns>
    public int RequiredBrownianIncrementBatchOutputLength(int pathCount, int stepCount)
    {
        return RandomProcesses.BrownianIncrementBatchOutputLength(pathCount, stepCount);
    }

    /// <summary>Returns the storage required for one diffusion path.</summary>
    /// <param name="stepCount">The nonnegative diffusion step count.</param>
    /// <returns>Step count plus one.</returns>
    public int RequiredDiffusionPathOutputLength(int stepCount)
    {
        return RandomProcesses.RandomWalkOutputLength(stepCount);
    }

    /// <summary>Returns the storage required for a path-major diffusion batch.</summary>
    /// <param name="pathCount">The nonnegative path count.</param>
    /// <param name="stepCount">The nonnegative step count per path.</param>
    /// <returns>Path count multiplied by step count plus one.</returns>
    public int RequiredDiffusionPathBatchOutputLength(int pathCount, int stepCount)
    {
        return RandomProcesses.RandomWalkBatchOutputLength(pathCount, stepCount);
    }

    /// <summary>Returns the storage required for one Poisson-count path.</summary>
    /// <param name="intervalCount">The positive interval count.</param>
    /// <returns>Interval count plus one.</returns>
    public int RequiredPoissonCountOutputLength(int intervalCount)
    {
        return RandomProcesses.PoissonCountOutputLength(intervalCount);
    }

    /// <summary>Returns the storage required for a path-major Poisson-count batch.</summary>
    /// <param name="pathCount">The nonnegative path count.</param>
    /// <param name="intervalCount">The positive interval count per path.</param>
    /// <returns>Path count multiplied by interval count plus one.</returns>
    public int RequiredPoissonCountBatchOutputLength(int pathCount, int intervalCount)
    {
        return RandomProcesses.PoissonCountBatchOutputLength(pathCount, intervalCount);
    }

    /// <summary>Returns the storage required for one Markov-chain path.</summary>
    /// <param name="stepCount">The nonnegative transition count.</param>
    /// <returns>Transition count plus one.</returns>
    public int RequiredMarkovPathOutputLength(int stepCount)
    {
        return RandomProcesses.RandomWalkOutputLength(stepCount);
    }

    /// <summary>Returns the storage required for a path-major Markov-chain batch.</summary>
    /// <param name="pathCount">The nonnegative path count.</param>
    /// <param name="stepCount">The nonnegative transition count per path.</param>
    /// <returns>Path count multiplied by transition count plus one.</returns>
    public int RequiredMarkovPathBatchOutputLength(int pathCount, int stepCount)
    {
        return RandomProcesses.RandomWalkBatchOutputLength(pathCount, stepCount);
    }

    /// <summary>Returns one symmetric integer random walk.</summary>
    /// <param name="initialState">The first state.</param>
    /// <param name="stepSize">The nonnegative fixed step magnitude.</param>
    /// <param name="stepCount">The nonnegative step count.</param>
    /// <returns>The initial state followed by every sampled state.</returns>
    public long[] IntegerRandomWalk(long initialState, long stepSize, int stepCount)
    {
        return RandomProcesses.RandomWalk(generator, initialState, stepSize, stepCount);
    }

    /// <summary>Writes one symmetric integer random walk.</summary>
    /// <param name="initialState">The first state.</param>
    /// <param name="stepSize">The nonnegative fixed step magnitude.</param>
    /// <param name="stepCount">The nonnegative step count.</param>
    /// <param name="destination">The exact path destination.</param>
    public void IntegerRandomWalkInto(long initialState, long stepSize, int stepCount, Span<long> destination)
    {
        RandomProcesses.RandomWalk(generator, initialState, stepSize, stepCount, destination);
    }

    /// <summary>Fills a path-major batch with symmetric integer random walks.</summary>
    /// <param name="initialState">The first state of every path.</param>
    /// <param name="stepSize">The nonnegative fixed step magnitude.</param>
    /// <param name="pathCount">The nonnegative path count.</param>
    /// <param name="stepCount">The nonnegative step count per path.</param>
    /// <param name="destination">The exact path-major destination.</param>
    public void FillIntegerRandomWalks(long initialState, long stepSize, int pathCount, int stepCount, Span<long> destination)
    {
        RandomProcesses.RandomWalks(generator, initialState, stepSize, pathCount, stepCount, destination);
    }

    /// <summary>Returns one symmetric floating random walk.</summary>
    /// <param name="initialState">The finite first state.</param>
    /// <param name="stepSize">The finite nonnegative fixed step magnitude.</param>
    /// <param name="stepCount">The nonnegative step count.</param>
    /// <returns>The initial state followed by every sampled state.</returns>
    public double[] FloatingRandomWalk(double initialState, double stepSize, int stepCount)
    {
        return RandomProcesses.RandomWalk(generator, initialState, stepSize, stepCount);
    }

    /// <summary>Writes one symmetric floating random walk.</summary>
    /// <param name="initialState">The finite first state.</param>
    /// <param name="stepSize">The finite nonnegative fixed step magnitude.</param>
    /// <param name="stepCount">The nonnegative step count.</param>
    /// <param name="destination">The exact path destination.</param>
    public void FloatingRandomWalkInto(double initialState, double stepSize, int stepCount, Span<double> destination)
    {
        RandomProcesses.RandomWalk(generator, initialState, stepSize, stepCount, destination);
    }

    /// <summary>Fills a path-major batch with symmetric floating random walks.</summary>
    /// <param name="initialState">The finite first state of every path.</param>
    /// <param name="stepSize">The finite nonnegative fixed step magnitude.</param>
    /// <param name="pathCount">The nonnegative path count.</param>
    /// <param name="stepCount">The nonnegative step count per path.</param>
    /// <param name="destination">The exact path-major destination.</param>
    public void FillFloatingRandomWalks(double initialState, double stepSize, int pathCount, int stepCount, Span<double> destination)
    {
        RandomProcesses.RandomWalks(generator, initialState, stepSize, pathCount, stepCount, destination);
    }

    /// <summary>Returns one path of independent Brownian increments.</summary>
    /// <param name="timeStep">The finite positive interval.</param>
    /// <param name="stepCount">The nonnegative increment count.</param>
    /// <returns>The sampled increments.</returns>
    public double[] BrownianIncrements(double timeStep, int stepCount)
    {
        return RandomProcesses.BrownianIncrements(generator, timeStep, stepCount);
    }

    /// <summary>Writes one path of independent Brownian increments.</summary>
    /// <param name="timeStep">The finite positive interval.</param>
    /// <param name="stepCount">The nonnegative increment count.</param>
    /// <param name="destination">The exact increment destination.</param>
    public void BrownianIncrementsInto(double timeStep, int stepCount, Span<double> destination)
    {
        RandomProcesses.BrownianIncrements(generator, timeStep, stepCount, destination);
    }

    /// <summary>Fills a path-major batch with Brownian increments.</summary>
    /// <param name="timeStep">The finite positive interval.</param>
    /// <param name="pathCount">The nonnegative path count.</param>
    /// <param name="stepCount">The nonnegative increment count per path.</param>
    /// <param name="destination">The exact path-major destination.</param>
    public void FillBrownianIncrements(double timeStep, int pathCount, int stepCount, Span<double> destination)
    {
        RandomProcesses.BrownianIncrements(generator, timeStep, pathCount, stepCount, destination);
    }

    /// <summary>Returns one arithmetic Brownian-motion path.</summary>
    /// <param name="initialState">The finite first state.</param>
    /// <param name="drift">The finite drift rate.</param>
    /// <param name="volatility">The finite nonnegative diffusion scale.</param>
    /// <param name="timeStep">The finite positive interval.</param>
    /// <param name="stepCount">The nonnegative step count.</param>
    /// <returns>The complete state path.</returns>
    public double[] BrownianMotion(double initialState, double drift, double volatility, double timeStep, int stepCount)
    {
        return RandomProcesses.BrownianMotion(generator, initialState, drift, volatility, timeStep, stepCount);
    }

    /// <summary>Writes one arithmetic Brownian-motion path.</summary>
    /// <param name="initialState">The finite first state.</param>
    /// <param name="drift">The finite drift rate.</param>
    /// <param name="volatility">The finite nonnegative diffusion scale.</param>
    /// <param name="timeStep">The finite positive interval.</param>
    /// <param name="stepCount">The nonnegative step count.</param>
    /// <param name="destination">The exact path destination.</param>
    public void BrownianMotionInto(double initialState, double drift, double volatility, double timeStep, int stepCount, Span<double> destination)
    {
        RandomProcesses.BrownianMotion(generator, initialState, drift, volatility, timeStep, stepCount, destination);
    }

    /// <summary>Fills a path-major batch with arithmetic Brownian-motion paths.</summary>
    /// <param name="initialState">The finite first state of every path.</param>
    /// <param name="drift">The finite drift rate.</param>
    /// <param name="volatility">The finite nonnegative diffusion scale.</param>
    /// <param name="timeStep">The finite positive interval.</param>
    /// <param name="pathCount">The nonnegative path count.</param>
    /// <param name="stepCount">The nonnegative step count per path.</param>
    /// <param name="destination">The exact path-major destination.</param>
    public void FillBrownianMotions(double initialState, double drift, double volatility, double timeStep, int pathCount, int stepCount, Span<double> destination)
    {
        RandomProcesses.BrownianMotion(generator, initialState, drift, volatility, timeStep, pathCount, stepCount, destination);
    }

    /// <summary>Returns one Brownian bridge with fixed endpoints.</summary>
    /// <param name="initialState">The finite first state.</param>
    /// <param name="finalState">The finite pinned final state.</param>
    /// <param name="volatility">The finite nonnegative diffusion scale.</param>
    /// <param name="timeStep">The finite positive interval.</param>
    /// <param name="stepCount">The positive step count.</param>
    /// <returns>The complete pinned path.</returns>
    public double[] BrownianBridge(double initialState, double finalState, double volatility, double timeStep, int stepCount)
    {
        return RandomProcesses.BrownianBridge(generator, initialState, finalState, volatility, timeStep, stepCount);
    }

    /// <summary>Writes one Brownian bridge with fixed endpoints.</summary>
    /// <param name="initialState">The finite first state.</param>
    /// <param name="finalState">The finite pinned final state.</param>
    /// <param name="volatility">The finite nonnegative diffusion scale.</param>
    /// <param name="timeStep">The finite positive interval.</param>
    /// <param name="stepCount">The positive step count.</param>
    /// <param name="destination">The exact path destination.</param>
    public void BrownianBridgeInto(double initialState, double finalState, double volatility, double timeStep, int stepCount, Span<double> destination)
    {
        RandomProcesses.BrownianBridge(generator, initialState, finalState, volatility, timeStep, stepCount, destination);
    }

    /// <summary>Fills a path-major batch with Brownian bridges.</summary>
    /// <param name="initialState">The finite first state of every path.</param>
    /// <param name="finalState">The finite pinned final state of every path.</param>
    /// <param name="volatility">The finite nonnegative diffusion scale.</param>
    /// <param name="timeStep">The finite positive interval.</param>
    /// <param name="pathCount">The nonnegative path count.</param>
    /// <param name="stepCount">The positive step count per path.</param>
    /// <param name="destination">The exact path-major destination.</param>
    public void FillBrownianBridges(double initialState, double finalState, double volatility, double timeStep, int pathCount, int stepCount, Span<double> destination)
    {
        RandomProcesses.BrownianBridge(generator, initialState, finalState, volatility, timeStep, pathCount, stepCount, destination);
    }

    /// <summary>Returns one geometric Brownian-motion path.</summary>
    /// <param name="initialState">The finite positive first state.</param>
    /// <param name="drift">The finite drift rate.</param>
    /// <param name="volatility">The finite nonnegative diffusion scale.</param>
    /// <param name="timeStep">The finite positive interval.</param>
    /// <param name="stepCount">The nonnegative step count.</param>
    /// <returns>The complete positive path.</returns>
    public double[] GeometricBrownianMotion(double initialState, double drift, double volatility, double timeStep, int stepCount)
    {
        return RandomProcesses.GeometricBrownianMotion(generator, initialState, drift, volatility, timeStep, stepCount);
    }

    /// <summary>Writes one geometric Brownian-motion path.</summary>
    /// <param name="initialState">The finite positive first state.</param>
    /// <param name="drift">The finite drift rate.</param>
    /// <param name="volatility">The finite nonnegative diffusion scale.</param>
    /// <param name="timeStep">The finite positive interval.</param>
    /// <param name="stepCount">The nonnegative step count.</param>
    /// <param name="destination">The exact path destination.</param>
    public void GeometricBrownianMotionInto(double initialState, double drift, double volatility, double timeStep, int stepCount, Span<double> destination)
    {
        RandomProcesses.GeometricBrownianMotion(generator, initialState, drift, volatility, timeStep, stepCount, destination);
    }

    /// <summary>Fills a path-major batch with geometric Brownian-motion paths.</summary>
    /// <param name="initialState">The finite positive first state of every path.</param>
    /// <param name="drift">The finite drift rate.</param>
    /// <param name="volatility">The finite nonnegative diffusion scale.</param>
    /// <param name="timeStep">The finite positive interval.</param>
    /// <param name="pathCount">The nonnegative path count.</param>
    /// <param name="stepCount">The nonnegative step count per path.</param>
    /// <param name="destination">The exact path-major destination.</param>
    public void FillGeometricBrownianMotions(double initialState, double drift, double volatility, double timeStep, int pathCount, int stepCount, Span<double> destination)
    {
        RandomProcesses.GeometricBrownianMotion(generator, initialState, drift, volatility, timeStep, pathCount, stepCount, destination);
    }

    /// <summary>Returns one Ornstein-Uhlenbeck path.</summary>
    /// <param name="initialState">The finite first state.</param>
    /// <param name="longTermMean">The finite long-term mean.</param>
    /// <param name="meanReversion">The finite nonnegative reversion rate.</param>
    /// <param name="volatility">The finite nonnegative diffusion scale.</param>
    /// <param name="timeStep">The finite positive interval.</param>
    /// <param name="stepCount">The nonnegative step count.</param>
    /// <returns>The complete state path.</returns>
    public double[] OrnsteinUhlenbeck(double initialState, double longTermMean, double meanReversion, double volatility, double timeStep, int stepCount)
    {
        return RandomProcesses.OrnsteinUhlenbeck(generator, initialState, longTermMean, meanReversion, volatility, timeStep, stepCount);
    }

    /// <summary>Writes one Ornstein-Uhlenbeck path.</summary>
    /// <param name="initialState">The finite first state.</param>
    /// <param name="longTermMean">The finite long-term mean.</param>
    /// <param name="meanReversion">The finite nonnegative reversion rate.</param>
    /// <param name="volatility">The finite nonnegative diffusion scale.</param>
    /// <param name="timeStep">The finite positive interval.</param>
    /// <param name="stepCount">The nonnegative step count.</param>
    /// <param name="destination">The exact path destination.</param>
    public void OrnsteinUhlenbeckInto(double initialState, double longTermMean, double meanReversion, double volatility, double timeStep, int stepCount, Span<double> destination)
    {
        RandomProcesses.OrnsteinUhlenbeck(generator, initialState, longTermMean, meanReversion, volatility, timeStep, stepCount, destination);
    }

    /// <summary>Fills a path-major batch with Ornstein-Uhlenbeck paths.</summary>
    /// <param name="initialState">The finite first state of every path.</param>
    /// <param name="longTermMean">The finite long-term mean.</param>
    /// <param name="meanReversion">The finite nonnegative reversion rate.</param>
    /// <param name="volatility">The finite nonnegative diffusion scale.</param>
    /// <param name="timeStep">The finite positive interval.</param>
    /// <param name="pathCount">The nonnegative path count.</param>
    /// <param name="stepCount">The nonnegative step count per path.</param>
    /// <param name="destination">The exact path-major destination.</param>
    public void FillOrnsteinUhlenbeckPaths(double initialState, double longTermMean, double meanReversion, double volatility, double timeStep, int pathCount, int stepCount, Span<double> destination)
    {
        RandomProcesses.OrnsteinUhlenbeck(generator, initialState, longTermMean, meanReversion, volatility, timeStep, pathCount, stepCount, destination);
    }

    /// <summary>Returns the exact arrival capacity for the generator's next Poisson path.</summary>
    /// <param name="rate">The finite nonnegative event rate.</param>
    /// <param name="timeHorizon">The finite nonnegative horizon.</param>
    /// <returns>The number of arrivals that the next matching call will produce.</returns>
    /// <remarks>This query does not advance the retained generator.</remarks>
    public int RequiredPoissonArrivalCapacity(double rate, double timeHorizon)
    {
        return RandomProcesses.RequiredPoissonArrivalCapacity(generator, rate, timeHorizon);
    }

    /// <summary>Returns homogeneous Poisson arrival times before the horizon.</summary>
    /// <param name="rate">The finite nonnegative event rate.</param>
    /// <param name="timeHorizon">The finite nonnegative horizon.</param>
    /// <returns>The sorted arrival times.</returns>
    public double[] PoissonArrivals(double rate, double timeHorizon)
    {
        return RandomProcesses.PoissonArrivals(generator, rate, timeHorizon);
    }

    /// <summary>Writes homogeneous Poisson arrival times before the horizon.</summary>
    /// <param name="rate">The finite nonnegative event rate.</param>
    /// <param name="timeHorizon">The finite nonnegative horizon.</param>
    /// <param name="destination">A destination at least as long as the required capacity.</param>
    /// <returns>The number of arrival times written.</returns>
    public int PoissonArrivalsInto(double rate, double timeHorizon, Span<double> destination)
    {
        return RandomProcesses.PoissonArrivals(generator, rate, timeHorizon, destination);
    }

    /// <summary>Returns one homogeneous Poisson cumulative-count path.</summary>
    /// <param name="rate">The finite nonnegative event rate.</param>
    /// <param name="timeHorizon">The finite nonnegative horizon.</param>
    /// <param name="intervalCount">The positive equal-interval count.</param>
    /// <returns>The initial zero followed by every cumulative count.</returns>
    public long[] PoissonCountPath(double rate, double timeHorizon, int intervalCount)
    {
        return RandomProcesses.PoissonCounts(generator, rate, timeHorizon, intervalCount);
    }

    /// <summary>Writes one homogeneous Poisson cumulative-count path.</summary>
    /// <param name="rate">The finite nonnegative event rate.</param>
    /// <param name="timeHorizon">The finite nonnegative horizon.</param>
    /// <param name="intervalCount">The positive equal-interval count.</param>
    /// <param name="destination">The exact count-path destination.</param>
    public void PoissonCountPathInto(double rate, double timeHorizon, int intervalCount, Span<long> destination)
    {
        RandomProcesses.PoissonCounts(generator, rate, timeHorizon, intervalCount, destination);
    }

    /// <summary>Fills a path-major batch with homogeneous Poisson count paths.</summary>
    /// <param name="rate">The finite nonnegative event rate.</param>
    /// <param name="timeHorizon">The finite nonnegative horizon.</param>
    /// <param name="pathCount">The nonnegative path count.</param>
    /// <param name="intervalCount">The positive equal-interval count per path.</param>
    /// <param name="destination">The exact path-major destination.</param>
    public void FillPoissonCountPaths(double rate, double timeHorizon, int pathCount, int intervalCount, Span<long> destination)
    {
        RandomProcesses.PoissonCounts(generator, rate, timeHorizon, pathCount, intervalCount, destination);
    }

    /// <summary>Returns one path from a finite-state Markov chain.</summary>
    /// <param name="chain">The preprocessed transition model.</param>
    /// <param name="initialState">The first state index.</param>
    /// <param name="stepCount">The nonnegative transition count.</param>
    /// <returns>The initial state followed by every sampled state.</returns>
    public int[] MarkovChainPath(MarkovChain chain, int initialState, int stepCount)
    {
        return RandomProcesses.MarkovChain(generator, chain, initialState, stepCount);
    }

    /// <summary>Writes one path from a finite-state Markov chain.</summary>
    /// <param name="chain">The preprocessed transition model.</param>
    /// <param name="initialState">The first state index.</param>
    /// <param name="stepCount">The nonnegative transition count.</param>
    /// <param name="destination">The exact state-path destination.</param>
    public void MarkovChainPathInto(MarkovChain chain, int initialState, int stepCount, Span<int> destination)
    {
        RandomProcesses.MarkovChain(generator, chain, initialState, stepCount, destination);
    }

    /// <summary>Fills a path-major batch with finite-state Markov-chain paths.</summary>
    /// <param name="chain">The preprocessed transition model.</param>
    /// <param name="initialState">The first state index of every path.</param>
    /// <param name="pathCount">The nonnegative path count.</param>
    /// <param name="stepCount">The nonnegative transition count per path.</param>
    /// <param name="destination">The exact path-major destination.</param>
    public void FillMarkovChainPaths(MarkovChain chain, int initialState, int pathCount, int stepCount, Span<int> destination)
    {
        RandomProcesses.MarkovChain(generator, chain, initialState, pathCount, stepCount, destination);
    }
}
