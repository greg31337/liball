using System;

namespace random;

/// <summary>
/// Generates deterministic-layout stochastic-process paths from an explicit generator.
/// </summary>
/// <remarks>
/// A random walk contains its initial state followed by one state per step. Batch
/// methods store complete paths in path-major order and consume random directions
/// path by path. The supplied mutable generator is not safe for concurrent use
/// without external synchronization. Integer walks replay bit-for-bit for a named deterministic
/// engine and call sequence. Floating stochastic processes follow the generator's math mode:
/// Portable provides cross-platform bit replay and System provides statistical equivalence.
/// </remarks>
internal static class RandomProcesses
{
    /// <summary>
    /// Returns an integer random walk with symmetric fixed-magnitude steps.
    /// </summary>
    /// <param name="generator">The generator used for step directions.</param>
    /// <param name="initialState">The first path state.</param>
    /// <param name="stepSize">The nonnegative magnitude of every step.</param>
    /// <param name="stepCount">The nonnegative number of steps after the initial state.</param>
    /// <returns>A new array of exactly step count plus one states.</returns>
    /// <remarks>A true direction bit adds the step size and a false direction bit subtracts it. A zero step size consumes no random state.</remarks>
    /// <exception cref="RandomArgumentException">An argument is invalid or some possible path would exceed the signed 64-bit range.</exception>
    public static long[] RandomWalk(RandomGenerator generator, long initialState, long stepSize, int stepCount)
    {
        int requiredLength = RandomWalkOutputLength(stepCount);
        ValidateIntegerWalk(generator, initialState, stepSize, stepCount);
        long[] result = new long[requiredLength];
        FillIntegerWalks(generator, initialState, stepSize, 1, stepCount, result);
        return result;
    }

    /// <summary>
    /// Writes an integer random walk with symmetric fixed-magnitude steps.
    /// </summary>
    /// <param name="generator">The generator used for step directions.</param>
    /// <param name="initialState">The first path state.</param>
    /// <param name="stepSize">The nonnegative magnitude of every step.</param>
    /// <param name="stepCount">The nonnegative number of steps after the initial state.</param>
    /// <param name="destination">The exact destination of step count plus one states.</param>
    /// <remarks>A true direction bit adds the step size and a false direction bit subtracts it. A zero step size consumes no random state.</remarks>
    /// <exception cref="RandomArgumentException">An argument or destination is invalid, or some possible path would exceed the signed 64-bit range.</exception>
    public static void RandomWalk(RandomGenerator generator, long initialState, long stepSize, int stepCount, Span<long> destination)
    {
        int requiredLength = RandomWalkOutputLength(stepCount);
        ValidateIntegerWalk(generator, initialState, stepSize, stepCount);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        FillIntegerWalks(generator, initialState, stepSize, 1, stepCount, destination);
    }

    /// <summary>
    /// Returns an integer random-walk batch in path-major order.
    /// </summary>
    /// <param name="generator">The generator used for step directions.</param>
    /// <param name="initialState">The first state of every path.</param>
    /// <param name="stepSize">The nonnegative magnitude of every step.</param>
    /// <param name="pathCount">The nonnegative number of complete paths.</param>
    /// <param name="stepCount">The nonnegative number of steps in each path.</param>
    /// <returns>A new path-major array containing path count times step count plus one states.</returns>
    /// <remarks>Path <c>p</c>, state <c>s</c> is stored at <c>result[p * (stepCount + 1) + s]</c>.</remarks>
    /// <exception cref="RandomArgumentException">An argument is invalid, storage is too large, or some possible path would exceed the signed 64-bit range.</exception>
    public static long[] RandomWalks(RandomGenerator generator, long initialState, long stepSize, int pathCount, int stepCount)
    {
        int requiredLength = RandomWalkBatchOutputLength(pathCount, stepCount);
        ValidateIntegerWalk(generator, initialState, stepSize, stepCount);
        long[] result = new long[requiredLength];
        FillIntegerWalks(generator, initialState, stepSize, pathCount, stepCount, result);
        return result;
    }

    /// <summary>
    /// Writes an integer random-walk batch in path-major order.
    /// </summary>
    /// <param name="generator">The generator used for step directions.</param>
    /// <param name="initialState">The first state of every path.</param>
    /// <param name="stepSize">The nonnegative magnitude of every step.</param>
    /// <param name="pathCount">The nonnegative number of complete paths.</param>
    /// <param name="stepCount">The nonnegative number of steps in each path.</param>
    /// <param name="destination">The exact path-major destination.</param>
    /// <remarks>Path <c>p</c>, state <c>s</c> is stored at <c>destination[p * (stepCount + 1) + s]</c>.</remarks>
    /// <exception cref="RandomArgumentException">An argument or destination is invalid, storage is too large, or some possible path would exceed the signed 64-bit range.</exception>
    public static void RandomWalks(RandomGenerator generator, long initialState, long stepSize, int pathCount, int stepCount, Span<long> destination)
    {
        int requiredLength = RandomWalkBatchOutputLength(pathCount, stepCount);
        ValidateIntegerWalk(generator, initialState, stepSize, stepCount);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        FillIntegerWalks(generator, initialState, stepSize, pathCount, stepCount, destination);
    }

    /// <summary>
    /// Returns a floating random walk with symmetric fixed-magnitude steps.
    /// </summary>
    /// <param name="generator">The generator used for step directions.</param>
    /// <param name="initialState">The finite first path state.</param>
    /// <param name="stepSize">The finite nonnegative magnitude of every step.</param>
    /// <param name="stepCount">The nonnegative number of steps after the initial state.</param>
    /// <returns>A new array of exactly step count plus one finite states.</returns>
    /// <remarks>A true direction bit adds the step size and a false direction bit subtracts it. A zero step size consumes no random state.</remarks>
    /// <exception cref="RandomArgumentException">An argument is invalid or some possible path would produce a non-finite state.</exception>
    public static double[] RandomWalk(RandomGenerator generator, double initialState, double stepSize, int stepCount)
    {
        int requiredLength = RandomWalkOutputLength(stepCount);
        ValidateFloatingWalk(generator, initialState, stepSize, stepCount);
        double[] result = new double[requiredLength];
        FillFloatingWalks(generator, initialState, stepSize, 1, stepCount, result);
        return result;
    }

    /// <summary>
    /// Writes a floating random walk with symmetric fixed-magnitude steps.
    /// </summary>
    /// <param name="generator">The generator used for step directions.</param>
    /// <param name="initialState">The finite first path state.</param>
    /// <param name="stepSize">The finite nonnegative magnitude of every step.</param>
    /// <param name="stepCount">The nonnegative number of steps after the initial state.</param>
    /// <param name="destination">The exact destination of step count plus one states.</param>
    /// <remarks>A true direction bit adds the step size and a false direction bit subtracts it. A zero step size consumes no random state.</remarks>
    /// <exception cref="RandomArgumentException">An argument or destination is invalid, or some possible path would produce a non-finite state.</exception>
    public static void RandomWalk(RandomGenerator generator, double initialState, double stepSize, int stepCount, Span<double> destination)
    {
        int requiredLength = RandomWalkOutputLength(stepCount);
        ValidateFloatingWalk(generator, initialState, stepSize, stepCount);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        FillFloatingWalks(generator, initialState, stepSize, 1, stepCount, destination);
    }

    /// <summary>
    /// Returns a floating random-walk batch in path-major order.
    /// </summary>
    /// <param name="generator">The generator used for step directions.</param>
    /// <param name="initialState">The finite first state of every path.</param>
    /// <param name="stepSize">The finite nonnegative magnitude of every step.</param>
    /// <param name="pathCount">The nonnegative number of complete paths.</param>
    /// <param name="stepCount">The nonnegative number of steps in each path.</param>
    /// <returns>A new path-major array containing path count times step count plus one finite states.</returns>
    /// <remarks>Path <c>p</c>, state <c>s</c> is stored at <c>result[p * (stepCount + 1) + s]</c>.</remarks>
    /// <exception cref="RandomArgumentException">An argument is invalid, storage is too large, or some possible path would produce a non-finite state.</exception>
    public static double[] RandomWalks(RandomGenerator generator, double initialState, double stepSize, int pathCount, int stepCount)
    {
        int requiredLength = RandomWalkBatchOutputLength(pathCount, stepCount);
        ValidateFloatingWalk(generator, initialState, stepSize, stepCount);
        double[] result = new double[requiredLength];
        FillFloatingWalks(generator, initialState, stepSize, pathCount, stepCount, result);
        return result;
    }

    /// <summary>
    /// Writes a floating random-walk batch in path-major order.
    /// </summary>
    /// <param name="generator">The generator used for step directions.</param>
    /// <param name="initialState">The finite first state of every path.</param>
    /// <param name="stepSize">The finite nonnegative magnitude of every step.</param>
    /// <param name="pathCount">The nonnegative number of complete paths.</param>
    /// <param name="stepCount">The nonnegative number of steps in each path.</param>
    /// <param name="destination">The exact path-major destination.</param>
    /// <remarks>Path <c>p</c>, state <c>s</c> is stored at <c>destination[p * (stepCount + 1) + s]</c>.</remarks>
    /// <exception cref="RandomArgumentException">An argument or destination is invalid, storage is too large, or some possible path would produce a non-finite state.</exception>
    public static void RandomWalks(RandomGenerator generator, double initialState, double stepSize, int pathCount, int stepCount, Span<double> destination)
    {
        int requiredLength = RandomWalkBatchOutputLength(pathCount, stepCount);
        ValidateFloatingWalk(generator, initialState, stepSize, stepCount);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        FillFloatingWalks(generator, initialState, stepSize, pathCount, stepCount, destination);
    }

    /// <summary>Returns one finite-state Markov-chain path from a reusable transition model.</summary>
    /// <param name="generator">The generator used for transitions.</param>
    /// <param name="chain">The non-null preprocessed transition model.</param>
    /// <param name="initialState">The first state index.</param>
    /// <param name="stepCount">The nonnegative transition count.</param>
    /// <returns>The initial state followed by every sampled state.</returns>
    /// <exception cref="RandomArgumentException">An argument is invalid or required storage is too large.</exception>
    public static int[] MarkovChain(RandomGenerator generator, MarkovChain chain, int initialState, int stepCount)
    {
        Guard.NotNull(chain, "chain");
        return chain.Path(generator, initialState, stepCount);
    }

    /// <summary>Writes one finite-state Markov-chain path from a reusable transition model.</summary>
    /// <param name="generator">The generator used for transitions.</param>
    /// <param name="chain">The non-null preprocessed transition model.</param>
    /// <param name="initialState">The first state index.</param>
    /// <param name="stepCount">The nonnegative transition count.</param>
    /// <param name="destination">The exact step count plus one state destination.</param>
    /// <exception cref="RandomArgumentException">An argument or destination is invalid, or required storage is too large.</exception>
    public static void MarkovChain(RandomGenerator generator, MarkovChain chain, int initialState, int stepCount, Span<int> destination)
    {
        Guard.NotNull(chain, "chain");
        chain.PathInto(generator, initialState, stepCount, destination);
    }

    /// <summary>Returns a path-major batch of finite-state Markov-chain paths.</summary>
    /// <param name="generator">The generator used for transitions.</param>
    /// <param name="chain">The non-null preprocessed transition model.</param>
    /// <param name="initialState">The first state index of every path.</param>
    /// <param name="pathCount">The nonnegative path count.</param>
    /// <param name="stepCount">The nonnegative transition count per path.</param>
    /// <returns>Path-major states with each initial state included.</returns>
    /// <exception cref="RandomArgumentException">An argument is invalid or required storage is too large.</exception>
    public static int[] MarkovChain(RandomGenerator generator, MarkovChain chain, int initialState, int pathCount, int stepCount)
    {
        Guard.NotNull(chain, "chain");
        return chain.Paths(generator, initialState, pathCount, stepCount);
    }

    /// <summary>Writes a path-major batch of finite-state Markov-chain paths.</summary>
    /// <param name="generator">The generator used for transitions.</param>
    /// <param name="chain">The non-null preprocessed transition model.</param>
    /// <param name="initialState">The first state index of every path.</param>
    /// <param name="pathCount">The nonnegative path count.</param>
    /// <param name="stepCount">The nonnegative transition count per path.</param>
    /// <param name="destination">The exact path-major destination.</param>
    /// <exception cref="RandomArgumentException">An argument or destination is invalid, or required storage is too large.</exception>
    public static void MarkovChain(RandomGenerator generator, MarkovChain chain, int initialState, int pathCount, int stepCount, Span<int> destination)
    {
        Guard.NotNull(chain, "chain");
        chain.PathsInto(generator, initialState, pathCount, stepCount, destination);
    }

    /// <summary>Returns all homogeneous Poisson arrival times before the exact horizon.</summary>
    /// <param name="generator">The generator used for the horizon count and conditional arrival times.</param>
    /// <param name="rate">The finite nonnegative event rate per unit time.</param>
    /// <param name="timeHorizon">The finite nonnegative terminal time.</param>
    /// <returns>Sorted arrival times in <c>[0, timeHorizon)</c>.</returns>
    /// <remarks>The horizon count is sampled first; conditional arrival times are sorted independent uniforms. No overshooting inter-arrival is consumed.</remarks>
    /// <exception cref="RandomArgumentException">An argument is invalid or the horizon mean is unsupported.</exception>
    /// <exception cref="RandomExhaustedException">The sampled arrival count exceeds array capacity.</exception>
    public static double[] PoissonArrivals(RandomGenerator generator, double rate, double timeHorizon)
    {
        int count = PoissonProcess.RequiredArrivalCapacity(generator, rate, timeHorizon);
        double[] result = new double[count];
        PoissonProcess.FillArrivals(generator, rate, timeHorizon, result);
        return result;
    }

    /// <summary>Writes all homogeneous Poisson arrival times before the exact horizon.</summary>
    /// <param name="generator">The generator used for the horizon count and conditional arrival times.</param>
    /// <param name="rate">The finite nonnegative event rate per unit time.</param>
    /// <param name="timeHorizon">The finite nonnegative terminal time.</param>
    /// <param name="destination">A destination with capacity for every sampled arrival; unused trailing entries are unchanged.</param>
    /// <returns>The number of arrival times written.</returns>
    /// <remarks>Capacity is checked through an exact cloned-stream preview before the original generator advances.</remarks>
    /// <exception cref="RandomArgumentException">An argument or destination is invalid, or the horizon mean is unsupported.</exception>
    /// <exception cref="RandomExhaustedException">The sampled arrival count exceeds array capacity.</exception>
    public static int PoissonArrivals(RandomGenerator generator, double rate, double timeHorizon, Span<double> destination)
    {
        return PoissonProcess.FillArrivals(generator, rate, timeHorizon, destination);
    }

    /// <summary>Returns the destination capacity required by the next Poisson-arrival request without advancing the generator.</summary>
    /// <param name="generator">The generator whose current stream is previewed through an exact clone.</param>
    /// <param name="rate">The finite nonnegative event rate per unit time.</param>
    /// <param name="timeHorizon">The finite nonnegative terminal time.</param>
    /// <returns>The exact next sampled horizon count.</returns>
    /// <exception cref="RandomArgumentException">An argument is invalid or the horizon mean is unsupported.</exception>
    /// <exception cref="RandomExhaustedException">The sampled arrival count exceeds array capacity.</exception>
    public static int RequiredPoissonArrivalCapacity(RandomGenerator generator, double rate, double timeHorizon)
    {
        return PoissonProcess.RequiredArrivalCapacity(generator, rate, timeHorizon);
    }

    /// <summary>Returns a cumulative homogeneous Poisson count path over equal subdivisions of an exact horizon.</summary>
    /// <param name="generator">The generator used for independent interval counts.</param>
    /// <param name="rate">The finite nonnegative event rate per unit time.</param>
    /// <param name="timeHorizon">The finite nonnegative terminal time.</param>
    /// <param name="intervalCount">The positive number of equal horizon intervals.</param>
    /// <returns>The initial zero count followed by each cumulative interval count.</returns>
    /// <exception cref="RandomArgumentException">An argument is invalid or the horizon mean is unsupported.</exception>
    /// <exception cref="RandomExhaustedException">A cumulative count exceeds the signed 64-bit range.</exception>
    public static long[] PoissonCounts(RandomGenerator generator, double rate, double timeHorizon, int intervalCount)
    {
        int requiredLength = PoissonCountOutputLength(intervalCount);
        long[] result = new long[requiredLength];
        PoissonProcess.FillCounts(generator, rate, timeHorizon, 1, intervalCount, result);
        return result;
    }

    /// <summary>Writes a cumulative homogeneous Poisson count path over equal subdivisions of an exact horizon.</summary>
    /// <param name="generator">The generator used for independent interval counts.</param>
    /// <param name="rate">The finite nonnegative event rate per unit time.</param>
    /// <param name="timeHorizon">The finite nonnegative terminal time.</param>
    /// <param name="intervalCount">The positive number of equal horizon intervals.</param>
    /// <param name="destination">The exact interval count plus one state destination.</param>
    /// <exception cref="RandomArgumentException">An argument or destination is invalid, or the horizon mean is unsupported.</exception>
    /// <exception cref="RandomExhaustedException">A cumulative count exceeds the signed 64-bit range.</exception>
    public static void PoissonCounts(RandomGenerator generator, double rate, double timeHorizon, int intervalCount, Span<long> destination)
    {
        int requiredLength = PoissonCountOutputLength(intervalCount);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        PoissonProcess.FillCounts(generator, rate, timeHorizon, 1, intervalCount, destination);
    }

    /// <summary>Returns a path-major batch of cumulative homogeneous Poisson count paths.</summary>
    /// <param name="generator">The generator used for independent interval counts.</param>
    /// <param name="rate">The finite nonnegative event rate per unit time.</param>
    /// <param name="timeHorizon">The finite nonnegative terminal time.</param>
    /// <param name="pathCount">The nonnegative number of paths.</param>
    /// <param name="intervalCount">The positive number of equal horizon intervals per path.</param>
    /// <returns>Path-major cumulative counts including an initial zero for each path.</returns>
    /// <exception cref="RandomArgumentException">An argument is invalid, storage is too large, or the horizon mean is unsupported.</exception>
    /// <exception cref="RandomExhaustedException">A cumulative count exceeds the signed 64-bit range.</exception>
    public static long[] PoissonCounts(RandomGenerator generator, double rate, double timeHorizon, int pathCount, int intervalCount)
    {
        int requiredLength = PoissonCountBatchOutputLength(pathCount, intervalCount);
        long[] result = new long[requiredLength];
        PoissonProcess.FillCounts(generator, rate, timeHorizon, pathCount, intervalCount, result);
        return result;
    }

    /// <summary>Writes a path-major batch of cumulative homogeneous Poisson count paths.</summary>
    /// <param name="generator">The generator used for independent interval counts.</param>
    /// <param name="rate">The finite nonnegative event rate per unit time.</param>
    /// <param name="timeHorizon">The finite nonnegative terminal time.</param>
    /// <param name="pathCount">The nonnegative number of paths.</param>
    /// <param name="intervalCount">The positive number of equal horizon intervals per path.</param>
    /// <param name="destination">The exact path-major destination.</param>
    /// <exception cref="RandomArgumentException">An argument or destination is invalid, storage is too large, or the horizon mean is unsupported.</exception>
    /// <exception cref="RandomExhaustedException">A cumulative count exceeds the signed 64-bit range.</exception>
    public static void PoissonCounts(RandomGenerator generator, double rate, double timeHorizon, int pathCount, int intervalCount, Span<long> destination)
    {
        int requiredLength = PoissonCountBatchOutputLength(pathCount, intervalCount);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        PoissonProcess.FillCounts(generator, rate, timeHorizon, pathCount, intervalCount, destination);
    }

    /// <summary>Returns the storage required for one cumulative Poisson count path.</summary>
    /// <param name="intervalCount">The positive number of equal intervals.</param>
    /// <returns>Interval count plus one.</returns>
    /// <exception cref="RandomArgumentException">The interval count is not positive or the state count is not representable.</exception>
    public static int PoissonCountOutputLength(int intervalCount)
    {
        Guard.PositiveCount(intervalCount, "intervalCount");
        if (intervalCount == int.MaxValue)
        {
            throw RandomArgumentException.Create("intervalCount", "The count path contains too many states.");
        }

        return intervalCount + 1;
    }

    /// <summary>Returns the storage required for a path-major cumulative Poisson count batch.</summary>
    /// <param name="pathCount">The nonnegative number of paths.</param>
    /// <param name="intervalCount">The positive number of equal intervals per path.</param>
    /// <returns>Path count multiplied by interval count plus one.</returns>
    /// <exception cref="RandomArgumentException">A count is invalid or required storage exceeds <c>int.MaxValue</c>.</exception>
    public static int PoissonCountBatchOutputLength(int pathCount, int intervalCount)
    {
        Guard.Count(pathCount, "pathCount");
        int statesPerPath = PoissonCountOutputLength(intervalCount);
        if (pathCount > int.MaxValue / statesPerPath)
        {
            throw RandomArgumentException.Create("pathCount", "The count-path batch contains too many states.");
        }

        return pathCount * statesPerPath;
    }

    /// <summary>Returns standard Brownian increments for equally spaced times.</summary>
    /// <param name="generator">The generator used for standard normal values.</param>
    /// <param name="timeStep">The finite positive time interval.</param>
    /// <param name="stepCount">The nonnegative increment count.</param>
    /// <returns>Exactly step count increments with variance equal to the time step.</returns>
    /// <exception cref="RandomArgumentException">An argument is invalid.</exception>
    public static double[] BrownianIncrements(RandomGenerator generator, double timeStep, int stepCount)
    {
        int requiredLength = BrownianIncrementOutputLength(stepCount);
        double[] result = new double[requiredLength];
        DiffusionProcesses.FillBrownianIncrements(generator, timeStep, 1, stepCount, result);
        return result;
    }

    /// <summary>Writes standard Brownian increments for equally spaced times.</summary>
    /// <param name="generator">The generator used for standard normal values.</param>
    /// <param name="timeStep">The finite positive time interval.</param>
    /// <param name="stepCount">The nonnegative increment count.</param>
    /// <param name="destination">The exact increment destination.</param>
    /// <exception cref="RandomArgumentException">An argument or destination is invalid.</exception>
    public static void BrownianIncrements(RandomGenerator generator, double timeStep, int stepCount, Span<double> destination)
    {
        int requiredLength = BrownianIncrementOutputLength(stepCount);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        DiffusionProcesses.FillBrownianIncrements(generator, timeStep, 1, stepCount, destination);
    }

    /// <summary>Returns a path-major batch of standard Brownian increments.</summary>
    /// <param name="generator">The generator used for standard normal values.</param>
    /// <param name="timeStep">The finite positive time interval.</param>
    /// <param name="pathCount">The nonnegative path count.</param>
    /// <param name="stepCount">The nonnegative increment count per path.</param>
    /// <returns>Path-major increments with each path occupying step count entries.</returns>
    /// <exception cref="RandomArgumentException">An argument is invalid or storage is too large.</exception>
    public static double[] BrownianIncrements(RandomGenerator generator, double timeStep, int pathCount, int stepCount)
    {
        int requiredLength = BrownianIncrementBatchOutputLength(pathCount, stepCount);
        double[] result = new double[requiredLength];
        DiffusionProcesses.FillBrownianIncrements(generator, timeStep, pathCount, stepCount, result);
        return result;
    }

    /// <summary>Writes a path-major batch of standard Brownian increments.</summary>
    /// <param name="generator">The generator used for standard normal values.</param>
    /// <param name="timeStep">The finite positive time interval.</param>
    /// <param name="pathCount">The nonnegative path count.</param>
    /// <param name="stepCount">The nonnegative increment count per path.</param>
    /// <param name="destination">The exact path-major destination.</param>
    /// <exception cref="RandomArgumentException">An argument or destination is invalid, or storage is too large.</exception>
    public static void BrownianIncrements(RandomGenerator generator, double timeStep, int pathCount, int stepCount, Span<double> destination)
    {
        int requiredLength = BrownianIncrementBatchOutputLength(pathCount, stepCount);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        DiffusionProcesses.FillBrownianIncrements(generator, timeStep, pathCount, stepCount, destination);
    }

    /// <summary>Returns a drifted Brownian-motion path.</summary>
    /// <param name="generator">The generator used for standard normal values.</param>
    /// <param name="initialState">The finite initial state.</param>
    /// <param name="drift">The finite drift rate.</param>
    /// <param name="volatility">The finite nonnegative diffusion scale.</param>
    /// <param name="timeStep">The finite positive time interval.</param>
    /// <param name="stepCount">The nonnegative step count.</param>
    /// <returns>The initial state followed by each Brownian-motion state.</returns>
    /// <exception cref="RandomArgumentException">An argument is invalid.</exception>
    public static double[] BrownianMotion(RandomGenerator generator, double initialState, double drift, double volatility, double timeStep, int stepCount)
    {
        int requiredLength = RandomWalkOutputLength(stepCount);
        double[] result = new double[requiredLength];
        DiffusionProcesses.FillBrownianMotion(generator, initialState, drift, volatility, timeStep, 1, stepCount, result);
        return result;
    }

    /// <summary>Writes a drifted Brownian-motion path.</summary>
    /// <param name="generator">The generator used for standard normal values.</param>
    /// <param name="initialState">The finite initial state.</param>
    /// <param name="drift">The finite drift rate.</param>
    /// <param name="volatility">The finite nonnegative diffusion scale.</param>
    /// <param name="timeStep">The finite positive time interval.</param>
    /// <param name="stepCount">The nonnegative step count.</param>
    /// <param name="destination">The exact path destination.</param>
    /// <exception cref="RandomArgumentException">An argument or destination is invalid.</exception>
    public static void BrownianMotion(RandomGenerator generator, double initialState, double drift, double volatility, double timeStep, int stepCount, Span<double> destination)
    {
        int requiredLength = RandomWalkOutputLength(stepCount);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        DiffusionProcesses.FillBrownianMotion(generator, initialState, drift, volatility, timeStep, 1, stepCount, destination);
    }

    /// <summary>Returns a path-major batch of drifted Brownian motions.</summary>
    /// <param name="generator">The generator used for standard normal values.</param>
    /// <param name="initialState">The finite initial state of every path.</param>
    /// <param name="drift">The finite drift rate.</param>
    /// <param name="volatility">The finite nonnegative diffusion scale.</param>
    /// <param name="timeStep">The finite positive time interval.</param>
    /// <param name="pathCount">The nonnegative path count.</param>
    /// <param name="stepCount">The nonnegative step count per path.</param>
    /// <returns>The path-major Brownian-motion states.</returns>
    /// <exception cref="RandomArgumentException">An argument is invalid or storage is too large.</exception>
    public static double[] BrownianMotion(RandomGenerator generator, double initialState, double drift, double volatility, double timeStep, int pathCount, int stepCount)
    {
        int requiredLength = RandomWalkBatchOutputLength(pathCount, stepCount);
        double[] result = new double[requiredLength];
        DiffusionProcesses.FillBrownianMotion(generator, initialState, drift, volatility, timeStep, pathCount, stepCount, result);
        return result;
    }

    /// <summary>Writes a path-major batch of drifted Brownian motions.</summary>
    /// <param name="generator">The generator used for standard normal values.</param>
    /// <param name="initialState">The finite initial state of every path.</param>
    /// <param name="drift">The finite drift rate.</param>
    /// <param name="volatility">The finite nonnegative diffusion scale.</param>
    /// <param name="timeStep">The finite positive time interval.</param>
    /// <param name="pathCount">The nonnegative path count.</param>
    /// <param name="stepCount">The nonnegative step count per path.</param>
    /// <param name="destination">The exact path-major destination.</param>
    /// <exception cref="RandomArgumentException">An argument or destination is invalid, or storage is too large.</exception>
    public static void BrownianMotion(RandomGenerator generator, double initialState, double drift, double volatility, double timeStep, int pathCount, int stepCount, Span<double> destination)
    {
        int requiredLength = RandomWalkBatchOutputLength(pathCount, stepCount);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        DiffusionProcesses.FillBrownianMotion(generator, initialState, drift, volatility, timeStep, pathCount, stepCount, destination);
    }

    /// <summary>Returns a Brownian bridge pinned to two finite endpoints.</summary>
    /// <param name="generator">The generator used for interior standard normal values.</param>
    /// <param name="initialState">The exact initial endpoint.</param>
    /// <param name="finalState">The exact final endpoint.</param>
    /// <param name="volatility">The finite nonnegative diffusion scale.</param>
    /// <param name="timeStep">The finite positive time interval.</param>
    /// <param name="stepCount">The positive interval count.</param>
    /// <returns>The initial endpoint, conditioned interior states, and final endpoint.</returns>
    /// <exception cref="RandomArgumentException">An argument is invalid.</exception>
    public static double[] BrownianBridge(RandomGenerator generator, double initialState, double finalState, double volatility, double timeStep, int stepCount)
    {
        int requiredLength = RandomWalkOutputLength(stepCount);
        double[] result = new double[requiredLength];
        DiffusionProcesses.FillBrownianBridge(generator, initialState, finalState, volatility, timeStep, 1, stepCount, result);
        return result;
    }

    /// <summary>Writes a Brownian bridge pinned to two finite endpoints.</summary>
    /// <param name="generator">The generator used for interior standard normal values.</param>
    /// <param name="initialState">The exact initial endpoint.</param>
    /// <param name="finalState">The exact final endpoint.</param>
    /// <param name="volatility">The finite nonnegative diffusion scale.</param>
    /// <param name="timeStep">The finite positive time interval.</param>
    /// <param name="stepCount">The positive interval count.</param>
    /// <param name="destination">The exact path destination.</param>
    /// <exception cref="RandomArgumentException">An argument or destination is invalid.</exception>
    public static void BrownianBridge(RandomGenerator generator, double initialState, double finalState, double volatility, double timeStep, int stepCount, Span<double> destination)
    {
        int requiredLength = RandomWalkOutputLength(stepCount);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        DiffusionProcesses.FillBrownianBridge(generator, initialState, finalState, volatility, timeStep, 1, stepCount, destination);
    }

    /// <summary>Returns a path-major batch of Brownian bridges.</summary>
    /// <param name="generator">The generator used for interior standard normal values.</param>
    /// <param name="initialState">The exact initial endpoint of every path.</param>
    /// <param name="finalState">The exact final endpoint of every path.</param>
    /// <param name="volatility">The finite nonnegative diffusion scale.</param>
    /// <param name="timeStep">The finite positive time interval.</param>
    /// <param name="pathCount">The nonnegative path count.</param>
    /// <param name="stepCount">The positive interval count per path.</param>
    /// <returns>The path-major bridge states.</returns>
    /// <exception cref="RandomArgumentException">An argument is invalid or storage is too large.</exception>
    public static double[] BrownianBridge(RandomGenerator generator, double initialState, double finalState, double volatility, double timeStep, int pathCount, int stepCount)
    {
        int requiredLength = RandomWalkBatchOutputLength(pathCount, stepCount);
        double[] result = new double[requiredLength];
        DiffusionProcesses.FillBrownianBridge(generator, initialState, finalState, volatility, timeStep, pathCount, stepCount, result);
        return result;
    }

    /// <summary>Writes a path-major batch of Brownian bridges.</summary>
    /// <param name="generator">The generator used for interior standard normal values.</param>
    /// <param name="initialState">The exact initial endpoint of every path.</param>
    /// <param name="finalState">The exact final endpoint of every path.</param>
    /// <param name="volatility">The finite nonnegative diffusion scale.</param>
    /// <param name="timeStep">The finite positive time interval.</param>
    /// <param name="pathCount">The nonnegative path count.</param>
    /// <param name="stepCount">The positive interval count per path.</param>
    /// <param name="destination">The exact path-major destination.</param>
    /// <exception cref="RandomArgumentException">An argument or destination is invalid, or storage is too large.</exception>
    public static void BrownianBridge(RandomGenerator generator, double initialState, double finalState, double volatility, double timeStep, int pathCount, int stepCount, Span<double> destination)
    {
        int requiredLength = RandomWalkBatchOutputLength(pathCount, stepCount);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        DiffusionProcesses.FillBrownianBridge(generator, initialState, finalState, volatility, timeStep, pathCount, stepCount, destination);
    }

    /// <summary>Returns an exact-step geometric Brownian-motion path.</summary>
    /// <param name="generator">The generator used for standard normal values.</param>
    /// <param name="initialState">The finite positive initial state.</param>
    /// <param name="drift">The finite proportional drift.</param>
    /// <param name="volatility">The finite nonnegative proportional volatility.</param>
    /// <param name="timeStep">The finite positive time interval.</param>
    /// <param name="stepCount">The nonnegative step count.</param>
    /// <returns>The initial state followed by exact exponential-transition states.</returns>
    /// <exception cref="RandomArgumentException">An argument is invalid.</exception>
    public static double[] GeometricBrownianMotion(RandomGenerator generator, double initialState, double drift, double volatility, double timeStep, int stepCount)
    {
        int requiredLength = RandomWalkOutputLength(stepCount);
        double[] result = new double[requiredLength];
        DiffusionProcesses.FillGeometricBrownianMotion(generator, initialState, drift, volatility, timeStep, 1, stepCount, result);
        return result;
    }

    /// <summary>Writes an exact-step geometric Brownian-motion path.</summary>
    /// <param name="generator">The generator used for standard normal values.</param>
    /// <param name="initialState">The finite positive initial state.</param>
    /// <param name="drift">The finite proportional drift.</param>
    /// <param name="volatility">The finite nonnegative proportional volatility.</param>
    /// <param name="timeStep">The finite positive time interval.</param>
    /// <param name="stepCount">The nonnegative step count.</param>
    /// <param name="destination">The exact path destination.</param>
    /// <exception cref="RandomArgumentException">An argument or destination is invalid.</exception>
    public static void GeometricBrownianMotion(RandomGenerator generator, double initialState, double drift, double volatility, double timeStep, int stepCount, Span<double> destination)
    {
        int requiredLength = RandomWalkOutputLength(stepCount);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        DiffusionProcesses.FillGeometricBrownianMotion(generator, initialState, drift, volatility, timeStep, 1, stepCount, destination);
    }

    /// <summary>Returns a path-major batch of exact-step geometric Brownian motions.</summary>
    /// <param name="generator">The generator used for standard normal values.</param>
    /// <param name="initialState">The finite positive initial state of every path.</param>
    /// <param name="drift">The finite proportional drift.</param>
    /// <param name="volatility">The finite nonnegative proportional volatility.</param>
    /// <param name="timeStep">The finite positive time interval.</param>
    /// <param name="pathCount">The nonnegative path count.</param>
    /// <param name="stepCount">The nonnegative step count per path.</param>
    /// <returns>The path-major geometric Brownian-motion states.</returns>
    /// <exception cref="RandomArgumentException">An argument is invalid or storage is too large.</exception>
    public static double[] GeometricBrownianMotion(RandomGenerator generator, double initialState, double drift, double volatility, double timeStep, int pathCount, int stepCount)
    {
        int requiredLength = RandomWalkBatchOutputLength(pathCount, stepCount);
        double[] result = new double[requiredLength];
        DiffusionProcesses.FillGeometricBrownianMotion(generator, initialState, drift, volatility, timeStep, pathCount, stepCount, result);
        return result;
    }

    /// <summary>Writes a path-major batch of exact-step geometric Brownian motions.</summary>
    /// <param name="generator">The generator used for standard normal values.</param>
    /// <param name="initialState">The finite positive initial state of every path.</param>
    /// <param name="drift">The finite proportional drift.</param>
    /// <param name="volatility">The finite nonnegative proportional volatility.</param>
    /// <param name="timeStep">The finite positive time interval.</param>
    /// <param name="pathCount">The nonnegative path count.</param>
    /// <param name="stepCount">The nonnegative step count per path.</param>
    /// <param name="destination">The exact path-major destination.</param>
    /// <exception cref="RandomArgumentException">An argument or destination is invalid, or storage is too large.</exception>
    public static void GeometricBrownianMotion(RandomGenerator generator, double initialState, double drift, double volatility, double timeStep, int pathCount, int stepCount, Span<double> destination)
    {
        int requiredLength = RandomWalkBatchOutputLength(pathCount, stepCount);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        DiffusionProcesses.FillGeometricBrownianMotion(generator, initialState, drift, volatility, timeStep, pathCount, stepCount, destination);
    }

    /// <summary>Returns an exact-transition Ornstein-Uhlenbeck path.</summary>
    /// <param name="generator">The generator used for standard normal values.</param>
    /// <param name="initialState">The finite initial state.</param>
    /// <param name="longTermMean">The finite reversion level.</param>
    /// <param name="meanReversion">The finite nonnegative reversion rate.</param>
    /// <param name="volatility">The finite nonnegative diffusion scale.</param>
    /// <param name="timeStep">The finite positive time interval.</param>
    /// <param name="stepCount">The nonnegative step count.</param>
    /// <returns>The initial state followed by exact Gaussian-transition states.</returns>
    /// <exception cref="RandomArgumentException">An argument is invalid.</exception>
    public static double[] OrnsteinUhlenbeck(RandomGenerator generator, double initialState, double longTermMean, double meanReversion, double volatility, double timeStep, int stepCount)
    {
        int requiredLength = RandomWalkOutputLength(stepCount);
        double[] result = new double[requiredLength];
        DiffusionProcesses.FillOrnsteinUhlenbeck(generator, initialState, longTermMean, meanReversion, volatility, timeStep, 1, stepCount, result);
        return result;
    }

    /// <summary>Writes an exact-transition Ornstein-Uhlenbeck path.</summary>
    /// <param name="generator">The generator used for standard normal values.</param>
    /// <param name="initialState">The finite initial state.</param>
    /// <param name="longTermMean">The finite reversion level.</param>
    /// <param name="meanReversion">The finite nonnegative reversion rate.</param>
    /// <param name="volatility">The finite nonnegative diffusion scale.</param>
    /// <param name="timeStep">The finite positive time interval.</param>
    /// <param name="stepCount">The nonnegative step count.</param>
    /// <param name="destination">The exact path destination.</param>
    /// <exception cref="RandomArgumentException">An argument or destination is invalid.</exception>
    public static void OrnsteinUhlenbeck(RandomGenerator generator, double initialState, double longTermMean, double meanReversion, double volatility, double timeStep, int stepCount, Span<double> destination)
    {
        int requiredLength = RandomWalkOutputLength(stepCount);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        DiffusionProcesses.FillOrnsteinUhlenbeck(generator, initialState, longTermMean, meanReversion, volatility, timeStep, 1, stepCount, destination);
    }

    /// <summary>Returns a path-major batch of exact-transition Ornstein-Uhlenbeck paths.</summary>
    /// <param name="generator">The generator used for standard normal values.</param>
    /// <param name="initialState">The finite initial state of every path.</param>
    /// <param name="longTermMean">The finite reversion level.</param>
    /// <param name="meanReversion">The finite nonnegative reversion rate.</param>
    /// <param name="volatility">The finite nonnegative diffusion scale.</param>
    /// <param name="timeStep">The finite positive time interval.</param>
    /// <param name="pathCount">The nonnegative path count.</param>
    /// <param name="stepCount">The nonnegative step count per path.</param>
    /// <returns>The path-major Ornstein-Uhlenbeck states.</returns>
    /// <exception cref="RandomArgumentException">An argument is invalid or storage is too large.</exception>
    public static double[] OrnsteinUhlenbeck(RandomGenerator generator, double initialState, double longTermMean, double meanReversion, double volatility, double timeStep, int pathCount, int stepCount)
    {
        int requiredLength = RandomWalkBatchOutputLength(pathCount, stepCount);
        double[] result = new double[requiredLength];
        DiffusionProcesses.FillOrnsteinUhlenbeck(generator, initialState, longTermMean, meanReversion, volatility, timeStep, pathCount, stepCount, result);
        return result;
    }

    /// <summary>Writes a path-major batch of exact-transition Ornstein-Uhlenbeck paths.</summary>
    /// <param name="generator">The generator used for standard normal values.</param>
    /// <param name="initialState">The finite initial state of every path.</param>
    /// <param name="longTermMean">The finite reversion level.</param>
    /// <param name="meanReversion">The finite nonnegative reversion rate.</param>
    /// <param name="volatility">The finite nonnegative diffusion scale.</param>
    /// <param name="timeStep">The finite positive time interval.</param>
    /// <param name="pathCount">The nonnegative path count.</param>
    /// <param name="stepCount">The nonnegative step count per path.</param>
    /// <param name="destination">The exact path-major destination.</param>
    /// <exception cref="RandomArgumentException">An argument or destination is invalid, or storage is too large.</exception>
    public static void OrnsteinUhlenbeck(RandomGenerator generator, double initialState, double longTermMean, double meanReversion, double volatility, double timeStep, int pathCount, int stepCount, Span<double> destination)
    {
        int requiredLength = RandomWalkBatchOutputLength(pathCount, stepCount);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        DiffusionProcesses.FillOrnsteinUhlenbeck(generator, initialState, longTermMean, meanReversion, volatility, timeStep, pathCount, stepCount, destination);
    }

    /// <summary>Returns the storage required for one Brownian-increment path.</summary>
    /// <param name="stepCount">The nonnegative increment count.</param>
    /// <returns>The step count.</returns>
    /// <exception cref="RandomArgumentException">The step count is negative.</exception>
    public static int BrownianIncrementOutputLength(int stepCount)
    {
        Guard.Count(stepCount, "stepCount");
        return stepCount;
    }

    /// <summary>Returns the storage required for a path-major Brownian-increment batch.</summary>
    /// <param name="pathCount">The nonnegative path count.</param>
    /// <param name="stepCount">The nonnegative increment count per path.</param>
    /// <returns>Path count multiplied by step count.</returns>
    /// <exception cref="RandomArgumentException">A count is negative or required storage exceeds <c>int.MaxValue</c>.</exception>
    public static int BrownianIncrementBatchOutputLength(int pathCount, int stepCount)
    {
        Guard.Count(pathCount, "pathCount");
        Guard.Count(stepCount, "stepCount");
        if (stepCount != 0 && pathCount > int.MaxValue / stepCount)
        {
            throw RandomArgumentException.Create("pathCount", "The increment batch contains too many values.");
        }

        return pathCount * stepCount;
    }

    /// <summary>
    /// Returns the state count required for one random walk or diffusion path.
    /// </summary>
    /// <param name="stepCount">The nonnegative number of steps after the initial state.</param>
    /// <returns>Step count plus one.</returns>
    /// <exception cref="RandomArgumentException">The step count is negative or cannot be represented with its initial state.</exception>
    public static int RandomWalkOutputLength(int stepCount)
    {
        Guard.Count(stepCount, "stepCount");
        if (stepCount == int.MaxValue)
        {
            throw RandomArgumentException.Create("stepCount", "The path contains too many states.");
        }

        return stepCount + 1;
    }

    /// <summary>
    /// Returns the state count required for a path-major random-walk batch.
    /// </summary>
    /// <param name="pathCount">The nonnegative number of paths.</param>
    /// <param name="stepCount">The nonnegative number of steps in each path.</param>
    /// <returns>Path count multiplied by step count plus one.</returns>
    /// <exception cref="RandomArgumentException">A count is negative or the required storage exceeds <c>int.MaxValue</c>.</exception>
    public static int RandomWalkBatchOutputLength(int pathCount, int stepCount)
    {
        Guard.Count(pathCount, "pathCount");
        int statesPerPath = RandomWalkOutputLength(stepCount);
        if (pathCount > int.MaxValue / statesPerPath)
        {
            throw RandomArgumentException.Create("pathCount", "The path batch contains too many states.");
        }

        return pathCount * statesPerPath;
    }

    static void ValidateIntegerWalk(RandomGenerator generator, long initialState, long stepSize, int stepCount)
    {
        Guard.NotNull(generator, "generator");
        if (stepSize < 0L)
        {
            throw RandomArgumentException.Create("stepSize", "The step size must not be negative.");
        }

        decimal displacement = (decimal)stepSize * (decimal)stepCount;
        decimal lower = (decimal)initialState - displacement;
        decimal upper = (decimal)initialState + displacement;
        if (lower < long.MinValue || upper > long.MaxValue)
        {
            throw RandomArgumentException.Create("stepSize", "Some possible path would exceed the signed 64-bit range.");
        }
    }

    static void ValidateFloatingWalk(RandomGenerator generator, double initialState, double stepSize, int stepCount)
    {
        Guard.NotNull(generator, "generator");
        Guard.Finite(initialState, "initialState");
        Guard.Finite(stepSize, "stepSize");
        if (stepSize < 0.0)
        {
            throw RandomArgumentException.Create("stepSize", "The step size must not be negative.");
        }

        double displacement = stepSize * (double)stepCount;
        if (!double.IsFinite(displacement) || !double.IsFinite(initialState - displacement) || !double.IsFinite(initialState + displacement))
        {
            throw RandomArgumentException.Create("stepSize", "Some possible path would produce a non-finite state.");
        }
    }

    static void FillIntegerWalks(RandomGenerator generator, long initialState, long stepSize, int pathCount, int stepCount, Span<long> destination)
    {
        int statesPerPath = stepCount + 1;
        int path = 0;
        while (path < pathCount)
        {
            int offset = path * statesPerPath;
            long state = initialState;
            destination[offset] = state;
            int step = 1;
            while (step <= stepCount)
            {
                if (stepSize != 0L)
                {
                    if (generator.NextBoolean())
                    {
                        state += stepSize;
                    }
                    else
                    {
                        state -= stepSize;
                    }
                }

                destination[offset + step] = state;
                step++;
            }

            path++;
        }
    }

    static void FillFloatingWalks(RandomGenerator generator, double initialState, double stepSize, int pathCount, int stepCount, Span<double> destination)
    {
        int statesPerPath = stepCount + 1;
        int path = 0;
        while (path < pathCount)
        {
            int offset = path * statesPerPath;
            double state = initialState;
            destination[offset] = state;
            int step = 1;
            while (step <= stepCount)
            {
                if (stepSize != 0.0)
                {
                    if (generator.NextBoolean())
                    {
                        state += stepSize;
                    }
                    else
                    {
                        state -= stepSize;
                    }
                }

                destination[offset + step] = state;
                step++;
            }

            path++;
        }
    }
}
