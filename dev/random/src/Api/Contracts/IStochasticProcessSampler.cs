using System;

namespace random;

/// <summary>
/// Defines fixed-length and variable-length stochastic-process sampling operations.
/// </summary>
public interface IStochasticProcessSampler
{
    /// <summary>Returns the storage required for one random walk.</summary>
    int RequiredRandomWalkOutputLength(int stepCount);

    /// <summary>Returns the storage required for a random-walk batch.</summary>
    int RequiredRandomWalkBatchOutputLength(int pathCount, int stepCount);

    /// <summary>Returns the storage required for one Brownian-increment path.</summary>
    int RequiredBrownianIncrementOutputLength(int stepCount);

    /// <summary>Returns the storage required for a Brownian-increment batch.</summary>
    int RequiredBrownianIncrementBatchOutputLength(int pathCount, int stepCount);

    /// <summary>Returns the storage required for one diffusion path.</summary>
    int RequiredDiffusionPathOutputLength(int stepCount);

    /// <summary>Returns the storage required for a diffusion-path batch.</summary>
    int RequiredDiffusionPathBatchOutputLength(int pathCount, int stepCount);

    /// <summary>Returns the storage required for one Poisson-count path.</summary>
    int RequiredPoissonCountOutputLength(int intervalCount);

    /// <summary>Returns the storage required for a Poisson-count batch.</summary>
    int RequiredPoissonCountBatchOutputLength(int pathCount, int intervalCount);

    /// <summary>Returns the storage required for one Markov-chain path.</summary>
    int RequiredMarkovPathOutputLength(int stepCount);

    /// <summary>Returns the storage required for a Markov-chain batch.</summary>
    int RequiredMarkovPathBatchOutputLength(int pathCount, int stepCount);

    /// <summary>Returns one symmetric integer random walk.</summary>
    long[] IntegerRandomWalk(long initialState, long stepSize, int stepCount);

    /// <summary>Writes one symmetric integer random walk.</summary>
    void IntegerRandomWalkInto(long initialState, long stepSize, int stepCount, Span<long> destination);

    /// <summary>Fills a path-major batch with symmetric integer random walks.</summary>
    void FillIntegerRandomWalks(long initialState, long stepSize, int pathCount, int stepCount, Span<long> destination);

    /// <summary>Returns one symmetric floating random walk.</summary>
    double[] FloatingRandomWalk(double initialState, double stepSize, int stepCount);

    /// <summary>Writes one symmetric floating random walk.</summary>
    void FloatingRandomWalkInto(double initialState, double stepSize, int stepCount, Span<double> destination);

    /// <summary>Fills a path-major batch with symmetric floating random walks.</summary>
    void FillFloatingRandomWalks(double initialState, double stepSize, int pathCount, int stepCount, Span<double> destination);

    /// <summary>Returns one path of independent Brownian increments.</summary>
    double[] BrownianIncrements(double timeStep, int stepCount);

    /// <summary>Writes one path of independent Brownian increments.</summary>
    void BrownianIncrementsInto(double timeStep, int stepCount, Span<double> destination);

    /// <summary>Fills a path-major batch with Brownian increments.</summary>
    void FillBrownianIncrements(double timeStep, int pathCount, int stepCount, Span<double> destination);

    /// <summary>Returns one arithmetic Brownian-motion path.</summary>
    double[] BrownianMotion(double initialState, double drift, double volatility, double timeStep, int stepCount);

    /// <summary>Writes one arithmetic Brownian-motion path.</summary>
    void BrownianMotionInto(double initialState, double drift, double volatility, double timeStep, int stepCount, Span<double> destination);

    /// <summary>Fills a path-major batch with arithmetic Brownian-motion paths.</summary>
    void FillBrownianMotions(double initialState, double drift, double volatility, double timeStep, int pathCount, int stepCount, Span<double> destination);

    /// <summary>Returns one Brownian bridge with fixed endpoints.</summary>
    double[] BrownianBridge(double initialState, double finalState, double volatility, double timeStep, int stepCount);

    /// <summary>Writes one Brownian bridge with fixed endpoints.</summary>
    void BrownianBridgeInto(double initialState, double finalState, double volatility, double timeStep, int stepCount, Span<double> destination);

    /// <summary>Fills a path-major batch with Brownian bridges.</summary>
    void FillBrownianBridges(double initialState, double finalState, double volatility, double timeStep, int pathCount, int stepCount, Span<double> destination);

    /// <summary>Returns one geometric Brownian-motion path.</summary>
    double[] GeometricBrownianMotion(double initialState, double drift, double volatility, double timeStep, int stepCount);

    /// <summary>Writes one geometric Brownian-motion path.</summary>
    void GeometricBrownianMotionInto(double initialState, double drift, double volatility, double timeStep, int stepCount, Span<double> destination);

    /// <summary>Fills a path-major batch with geometric Brownian-motion paths.</summary>
    void FillGeometricBrownianMotions(double initialState, double drift, double volatility, double timeStep, int pathCount, int stepCount, Span<double> destination);

    /// <summary>Returns one Ornstein-Uhlenbeck path.</summary>
    double[] OrnsteinUhlenbeck(double initialState, double longTermMean, double meanReversion, double volatility, double timeStep, int stepCount);

    /// <summary>Writes one Ornstein-Uhlenbeck path.</summary>
    void OrnsteinUhlenbeckInto(double initialState, double longTermMean, double meanReversion, double volatility, double timeStep, int stepCount, Span<double> destination);

    /// <summary>Fills a path-major batch with Ornstein-Uhlenbeck paths.</summary>
    void FillOrnsteinUhlenbeckPaths(double initialState, double longTermMean, double meanReversion, double volatility, double timeStep, int pathCount, int stepCount, Span<double> destination);

    /// <summary>Returns the capacity required by the next matching Poisson-arrival call.</summary>
    int RequiredPoissonArrivalCapacity(double rate, double timeHorizon);

    /// <summary>Returns homogeneous Poisson arrival times before the horizon.</summary>
    double[] PoissonArrivals(double rate, double timeHorizon);

    /// <summary>Writes homogeneous Poisson arrival times before the horizon.</summary>
    int PoissonArrivalsInto(double rate, double timeHorizon, Span<double> destination);

    /// <summary>Returns one homogeneous Poisson cumulative-count path.</summary>
    long[] PoissonCountPath(double rate, double timeHorizon, int intervalCount);

    /// <summary>Writes one homogeneous Poisson cumulative-count path.</summary>
    void PoissonCountPathInto(double rate, double timeHorizon, int intervalCount, Span<long> destination);

    /// <summary>Fills a path-major batch with homogeneous Poisson count paths.</summary>
    void FillPoissonCountPaths(double rate, double timeHorizon, int pathCount, int intervalCount, Span<long> destination);

    /// <summary>Returns one path from a finite-state Markov chain.</summary>
    int[] MarkovChainPath(MarkovChain chain, int initialState, int stepCount);

    /// <summary>Writes one path from a finite-state Markov chain.</summary>
    void MarkovChainPathInto(MarkovChain chain, int initialState, int stepCount, Span<int> destination);

    /// <summary>Fills a path-major batch with finite-state Markov-chain paths.</summary>
    void FillMarkovChainPaths(MarkovChain chain, int initialState, int pathCount, int stepCount, Span<int> destination);
}
