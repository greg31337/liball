using System;

using System.Numerics;

namespace random;

/// <summary>
/// Provides a mutable deterministic random-number generator backed by an explicitly named algorithm.
/// </summary>
/// <remarks>
/// Instances are intended for simulation and reproducible computation, not cryptographic use.
/// An instance is owned by one caller at a time and is not implicitly thread-safe.
/// Raw engine output, integer conversions, and non-transcendental floating conversions replay
/// bit-for-bit for the same named algorithm version, transform version, seed or state, and call
/// sequence. Distribution transforms replay bit-for-bit across supported .NET 10 platforms in
/// <see cref="RandomMathMode.Portable"/> mode. <see cref="RandomMathMode.System"/> mode promises
/// statistical equivalence rather than identical last bits or an identical post-sampling state.
/// </remarks>
public class RandomGenerator : IRandomSource, IRandomGenerator
{
    IRandomEngine engine;
    IRandomEngine initialEngine;
    RandomMathMode mathMode;
    byte[] spawnRootMaterial;
    ulong[] spawnPath;
    ulong booleanReservoir;
    int booleanBitCount;
    ulong initialBooleanReservoir;
    int initialBooleanBitCount;

    internal RandomGenerator(
        IRandomEngine engine,
        IRandomEngine initialEngine,
        RandomMathMode mathMode,
        byte[] spawnRootMaterial,
        ulong[] spawnPath,
        ulong booleanReservoir,
        int booleanBitCount,
        ulong initialBooleanReservoir,
        int initialBooleanBitCount)
    {
        Guard.NotNull(engine, "engine");
        Guard.NotNull(initialEngine, "initialEngine");
        Guard.NotNull(spawnRootMaterial, "spawnRootMaterial");
        Guard.NotNull(spawnPath, "spawnPath");
        ValidateMathMode(mathMode);
        GeneratorStateCodec.ValidateReservoir(booleanReservoir, booleanBitCount, engine.EngineInfo().WordBits());
        GeneratorStateCodec.ValidateReservoir(initialBooleanReservoir, initialBooleanBitCount, initialEngine.EngineInfo().WordBits());
        this.engine = engine;
        this.initialEngine = initialEngine;
        this.mathMode = mathMode;
        this.spawnRootMaterial = (byte[])spawnRootMaterial.Clone();
        this.spawnPath = (ulong[])spawnPath.Clone();
        this.booleanReservoir = booleanReservoir;
        this.booleanBitCount = booleanBitCount;
        this.initialBooleanReservoir = initialBooleanReservoir;
        this.initialBooleanBitCount = initialBooleanBitCount;
    }

    static RandomGenerator FromEngine(IRandomEngine engine, RandomMathMode mathMode)
    {
        IRandomEngine initialEngine = engine.CloneEngine();
        byte[] rootMaterial = initialEngine.ExportEngineState().ToBytes();
        return new RandomGenerator(
            engine,
            initialEngine,
            mathMode,
            rootMaterial,
            new ulong[0],
            0UL,
            0,
            0UL,
            0);
    }

    /// <summary>
    /// Creates the library-major-version default deterministic generator.
    /// </summary>
    /// <param name="seed">The unsigned seed mapped to the PCG initial state.</param>
    /// <param name="mathMode">The explicit mathematical-kernel mode used by later distribution methods.</param>
    /// <returns>A PCG64 DXSM generator using stream selector zero.</returns>
    /// <exception cref="RandomArgumentException">The math mode is unknown.</exception>
    internal static RandomGenerator CreateDefault(ulong seed, RandomMathMode mathMode)
    {
        ValidateMathMode(mathMode);
        Pcg64DxsmEngine selected = Pcg64DxsmEngine.FromSeed((UInt128)seed, (UInt128)0);
        return FromEngine(selected, mathMode);
    }

    /// <summary>
    /// Creates a PCG64 DXSM generator from its initial state and stream selector.
    /// </summary>
    /// <param name="initialState">The 128-bit initial-state value.</param>
    /// <param name="streamSelector">The 128-bit stream selector mapped to an odd increment.</param>
    /// <param name="mathMode">The explicit mathematical-kernel mode.</param>
    /// <returns>A PCG64 DXSM generator.</returns>
    /// <exception cref="RandomArgumentException">The math mode is unknown.</exception>
    internal static RandomGenerator CreatePcg64Dxsm(UInt128 initialState, UInt128 streamSelector, RandomMathMode mathMode)
    {
        ValidateMathMode(mathMode);
        Pcg64DxsmEngine selected = Pcg64DxsmEngine.FromSeed(initialState, streamSelector);
        return FromEngine(selected, mathMode);
    }

    /// <summary>
    /// Creates a xoshiro256** generator using its documented SplitMix64 seed mapping.
    /// </summary>
    /// <param name="seed">The unsigned 64-bit seed.</param>
    /// <param name="mathMode">The explicit mathematical-kernel mode.</param>
    /// <returns>A xoshiro256** generator.</returns>
    /// <exception cref="RandomArgumentException">The math mode is unknown.</exception>
    internal static RandomGenerator CreateXoshiro256StarStar(ulong seed, RandomMathMode mathMode)
    {
        ValidateMathMode(mathMode);
        Xoshiro256StarStarEngine selected = Xoshiro256StarStarEngine.FromSeed(seed);
        return FromEngine(selected, mathMode);
    }

    /// <summary>
    /// Creates a Philox4x64-10 generator with an all-zero initial counter.
    /// </summary>
    /// <param name="key">The 128-bit Philox key.</param>
    /// <param name="mathMode">The explicit mathematical-kernel mode.</param>
    /// <returns>A Philox4x64-10 generator positioned at counter zero.</returns>
    /// <exception cref="RandomArgumentException">The math mode is unknown.</exception>
    internal static RandomGenerator CreatePhilox4x64Ten(UInt128 key, RandomMathMode mathMode)
    {
        ValidateMathMode(mathMode);
        Philox4x64TenEngine selected = Philox4x64TenEngine.FromKey(key);
        return FromEngine(selected, mathMode);
    }

    /// <summary>
    /// Creates a Philox4x64-10 generator from an exact key and counter.
    /// </summary>
    /// <param name="key">Two 64-bit key words in least-significant-word-first order.</param>
    /// <param name="counter">Four 64-bit counter words in least-significant-word-first order.</param>
    /// <param name="mathMode">The explicit mathematical-kernel mode.</param>
    /// <returns>A Philox4x64-10 generator at the requested counter.</returns>
    /// <exception cref="RandomArgumentException">An array is null, has an invalid length, or the math mode is unknown.</exception>
    internal static RandomGenerator CreatePhilox4x64Ten(ulong[] key, ulong[] counter, RandomMathMode mathMode)
    {
        ValidateMathMode(mathMode);
        Philox4x64TenEngine selected = Philox4x64TenEngine.FromKeyAndCounter(key, counter);
        return FromEngine(selected, mathMode);
    }

    /// <summary>
    /// Creates an MT19937 generator using the standard reference scalar initializer.
    /// </summary>
    /// <param name="seed">The unsigned 32-bit reference seed.</param>
    /// <param name="mathMode">The explicit mathematical-kernel mode.</param>
    /// <returns>A reference-initialized MT19937 generator.</returns>
    /// <exception cref="RandomArgumentException">The math mode is unknown.</exception>
    internal static RandomGenerator CreateMt19937(uint seed, RandomMathMode mathMode)
    {
        ValidateMathMode(mathMode);
        Mt19937Engine selected = Mt19937Engine.FromSeed(seed);
        return FromEngine(selected, mathMode);
    }

    /// <summary>
    /// Creates an MT19937 generator using the standard reference array initializer.
    /// </summary>
    /// <param name="seed">The nonempty array of unsigned 32-bit reference seed words.</param>
    /// <param name="mathMode">The explicit mathematical-kernel mode.</param>
    /// <returns>A reference-array-initialized MT19937 generator.</returns>
    /// <exception cref="RandomArgumentException">The seed is null or empty, or the math mode is unknown.</exception>
    internal static RandomGenerator CreateMt19937(uint[] seed, RandomMathMode mathMode)
    {
        ValidateMathMode(mathMode);
        Mt19937Engine selected = Mt19937Engine.FromSeedArray(seed);
        return FromEngine(selected, mathMode);
    }

    /// <summary>
    /// Creates an MT19937 generator using NumPy-compatible <c>SeedSequence</c> initialization.
    /// </summary>
    /// <param name="seed">The unsigned 32-bit NumPy entropy value.</param>
    /// <param name="mathMode">The explicit mathematical-kernel mode.</param>
    /// <returns>A separately identified NumPy-compatible MT19937 generator.</returns>
    /// <exception cref="RandomArgumentException">The math mode is unknown.</exception>
    internal static RandomGenerator CreateMt19937Numpy(uint seed, RandomMathMode mathMode)
    {
        ValidateMathMode(mathMode);
        Mt19937NumpyEngine selected = Mt19937NumpyEngine.FromSeed(seed);
        return FromEngine(selected, mathMode);
    }

    /// <summary>
    /// Creates an MT19937 generator using NumPy-compatible array entropy.
    /// </summary>
    /// <param name="seed">The unsigned 32-bit NumPy entropy words.</param>
    /// <param name="mathMode">The explicit mathematical-kernel mode.</param>
    /// <returns>A separately identified NumPy-compatible MT19937 generator.</returns>
    /// <exception cref="RandomArgumentException">The seed is null or the math mode is unknown.</exception>
    internal static RandomGenerator CreateMt19937Numpy(uint[] seed, RandomMathMode mathMode)
    {
        ValidateMathMode(mathMode);
        Mt19937NumpyEngine selected = Mt19937NumpyEngine.FromSeedArray(seed);
        return FromEngine(selected, mathMode);
    }

    /// <summary>
    /// Creates a SplitMix64 generator with the supplied exact initial state.
    /// </summary>
    /// <param name="seed">The unsigned 64-bit initial state.</param>
    /// <param name="mathMode">The explicit mathematical-kernel mode.</param>
    /// <returns>A SplitMix64 generator.</returns>
    /// <exception cref="RandomArgumentException">The math mode is unknown.</exception>
    internal static RandomGenerator CreateSplitMix64(ulong seed, RandomMathMode mathMode)
    {
        ValidateMathMode(mathMode);
        SplitMix64Engine selected = SplitMix64Engine.FromSeed(seed);
        return FromEngine(selected, mathMode);
    }

    /// <summary>
    /// Creates a named deterministic generator from canonical seed-sequence identity.
    /// </summary>
    /// <param name="algorithm">The named deterministic algorithm.</param>
    /// <param name="seedSequence">The canonical root seed and complete spawn path.</param>
    /// <param name="mathMode">The explicit mathematical-kernel mode.</param>
    /// <returns>A generator derived with algorithm and algorithm-version domain separation.</returns>
    /// <exception cref="RandomArgumentException">The algorithm is not deterministic, the seed sequence is null, or the math mode is unknown.</exception>
    internal static RandomGenerator CreateFromSeedSequence(RandomAlgorithm algorithm, SeedSequence seedSequence, RandomMathMode mathMode)
    {
        ValidateSeedAlgorithm(algorithm);
        Guard.NotNull(seedSequence, "seedSequence");
        ValidateMathMode(mathMode);
        byte[] rootMaterial = seedSequence.RootMaterial();
        ulong[] path = seedSequence.Path();
        IRandomEngine selected = EngineSpawner.Create(algorithm, rootMaterial, path);
        return new RandomGenerator(
            selected,
            selected.CloneEngine(),
            mathMode,
            rootMaterial,
            path,
            0UL,
            0,
            0UL,
            0);
    }

    /// <summary>
    /// Creates a generator positioned at an imported deterministic state.
    /// </summary>
    /// <param name="randomState">The validated algorithm-specific state.</param>
    /// <param name="mathMode">The explicit mathematical-kernel mode, which is not encoded in engine state.</param>
    /// <returns>A generator whose next word resumes the imported stream.</returns>
    /// <exception cref="RandomArgumentException">The state is null or the math mode is unknown.</exception>
    /// <exception cref="RandomStateException">The state does not describe a supported valid engine.</exception>
    internal static RandomGenerator FromState(RandomState randomState, RandomMathMode mathMode)
    {
        ValidateMathMode(mathMode);
        GeneratorStateData decoded = GeneratorStateCodec.Decode(randomState);
        IRandomEngine selected = decoded.Engine();
        ulong reservoir = decoded.BooleanReservoir();
        int bitCount = decoded.BooleanBitCount();
        return new RandomGenerator(
            selected,
            selected.CloneEngine(),
            mathMode,
            randomState.ToBytes(),
            new ulong[0],
            reservoir,
            bitCount,
            reservoir,
            bitCount);
    }

    /// <summary>
    /// Returns metadata for the selected named algorithm and generator lifecycle.
    /// </summary>
    /// <returns>Algorithm metadata whose capabilities include deterministic reset.</returns>
    public RandomAlgorithmInfo Info()
    {
        RandomAlgorithmInfo engineInfo = engine.EngineInfo();
        RandomCapability[] engineCapabilities = engineInfo.Capabilities();
        bool hasReset = engineInfo.HasCapability(RandomCapability.Reset);
        bool hasSpawn = engineInfo.HasCapability(RandomCapability.Spawn);
        int additionalCount = 0;
        if (!hasReset)
        {
            additionalCount++;
        }

        if (!hasSpawn)
        {
            additionalCount++;
        }

        RandomCapability[] capabilities = new RandomCapability[engineCapabilities.Length + additionalCount];
        Array.Copy(engineCapabilities, capabilities, engineCapabilities.Length);
        int capabilityIndex = engineCapabilities.Length;
        if (!hasReset)
        {
            capabilities[capabilityIndex] = RandomCapability.Reset;
            capabilityIndex++;
        }

        if (!hasSpawn)
        {
            capabilities[capabilityIndex] = RandomCapability.Spawn;
        }

        return new RandomAlgorithmInfo(
            engineInfo.Algorithm(),
            engineInfo.Name(),
            engineInfo.Version(),
            engineInfo.TransformVersion(),
            engineInfo.WordBits(),
            engineInfo.StateBytes(),
            engineInfo.IsCryptographic(),
            engineInfo.PeriodDescription(),
            capabilities);
    }

    /// <summary>
    /// Returns the explicit mathematical-kernel mode selected at construction.
    /// </summary>
    /// <returns>The portable or system math mode.</returns>
    public RandomMathMode MathMode()
    {
        return mathMode;
    }

    /// <summary>
    /// Creates an exact copy of the current stream position and its reset point.
    /// </summary>
    /// <returns>An independently mutable generator with identical current and initial states.</returns>
    public RandomGenerator Clone()
    {
        return new RandomGenerator(
            engine.CloneEngine(),
            initialEngine.CloneEngine(),
            mathMode,
            spawnRootMaterial,
            spawnPath,
            booleanReservoir,
            booleanBitCount,
            initialBooleanReservoir,
            initialBooleanBitCount);
    }

    /// <summary>
    /// Creates the deterministic child identified by an unsigned index at the current spawn path.
    /// </summary>
    /// <param name="index">The child index appended to the complete current spawn path.</param>
    /// <returns>An independent child generator using the same named algorithm and math mode.</returns>
    /// <remarks>
    /// Child identity depends on the factory or imported root state and complete path, not on
    /// creation order or the parent's current output position. PCG children use distinct derived
    /// native stream selectors, and Philox children use derived high-counter partitions.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The spawn path is too long.</exception>
    public RandomGenerator Spawn(ulong index)
    {
        if (spawnPath.Length == int.MaxValue)
        {
            throw RandomArgumentException.Create("index", "The spawn path is too long.");
        }

        ulong[] childPath = new ulong[spawnPath.Length + 1];
        Array.Copy(spawnPath, childPath, spawnPath.Length);
        childPath[spawnPath.Length] = index;
        IRandomEngine childEngine = EngineSpawner.Create(engine.EngineInfo().Algorithm(), spawnRootMaterial, childPath);
        return new RandomGenerator(
            childEngine,
            childEngine.CloneEngine(),
            mathMode,
            spawnRootMaterial,
            childPath,
            0UL,
            0,
            0UL,
            0);
    }

    /// <summary>
    /// Creates children with indexes zero through count minus one at the current spawn path.
    /// </summary>
    /// <param name="count">The nonnegative number of indexed children.</param>
    /// <returns>Independent child generators in ascending index order.</returns>
    /// <exception cref="RandomArgumentException">The count is negative or the spawn path is too long.</exception>
    public RandomGenerator[] SpawnMany(int count)
    {
        Guard.Count(count, "count");
        RandomGenerator[] children = new RandomGenerator[count];
        int index = 0;
        while (index < count)
        {
            children[index] = Spawn((ulong)index);
            index++;
        }

        return children;
    }

    /// <summary>
    /// Returns an unbiased byte below an exclusive bound.
    /// </summary>
    /// <param name="bound">The positive exclusive upper bound.</param>
    /// <returns>A uniformly distributed byte in <c>[0, bound)</c>.</returns>
    /// <remarks>A bound of one returns zero without consuming random state.</remarks>
    /// <exception cref="RandomArgumentException">The bound is zero.</exception>
    public byte NextByteBounded(byte bound)
    {
        return IntegerUniform.NextByteBounded(this, bound);
    }

    /// <summary>
    /// Returns an unbiased byte in a half-open range.
    /// </summary>
    /// <param name="minimum">The inclusive lower bound.</param>
    /// <param name="maximum">The exclusive upper bound.</param>
    /// <returns>A uniformly distributed byte in <c>[minimum, maximum)</c>.</returns>
    /// <remarks>A range containing one value returns it without consuming random state.</remarks>
    /// <exception cref="RandomArgumentException">The maximum is not greater than the minimum.</exception>
    public byte NextByteInRange(byte minimum, byte maximum)
    {
        return IntegerUniform.NextByteInRange(this, minimum, maximum);
    }

    /// <summary>
    /// Returns 32 raw random bits.
    /// </summary>
    /// <returns>The low 32 bits of one native word.</returns>
    /// <remarks>
    /// A 64-bit engine consumes one native word. A 32-bit engine returns its complete native word.
    /// Any unused cached boolean bits are discarded first.
    /// </remarks>
    public uint NextUInt32()
    {
        ClearBooleanReservoir();
        return (uint)(engine.NextRawWord() & uint.MaxValue);
    }

    /// <summary>
    /// Returns an unbiased unsigned 32-bit integer below an exclusive bound.
    /// </summary>
    /// <param name="bound">The positive exclusive upper bound.</param>
    /// <returns>A uniformly distributed value in <c>[0, bound)</c>.</returns>
    /// <remarks>A bound of one returns zero without consuming random state.</remarks>
    /// <exception cref="RandomArgumentException">The bound is zero.</exception>
    public uint NextUInt32Bounded(uint bound)
    {
        return IntegerUniform.NextUInt32(this, bound);
    }

    /// <summary>
    /// Returns an unbiased unsigned 32-bit integer in a half-open range.
    /// </summary>
    /// <param name="minimum">The inclusive lower bound.</param>
    /// <param name="maximum">The exclusive upper bound.</param>
    /// <returns>A uniformly distributed value in <c>[minimum, maximum)</c>.</returns>
    /// <remarks>A range containing one value returns it without consuming random state.</remarks>
    /// <exception cref="RandomArgumentException">The maximum is not greater than the minimum.</exception>
    public uint NextUInt32InRange(uint minimum, uint maximum)
    {
        return IntegerUniform.NextUInt32(this, minimum, maximum);
    }

    /// <summary>
    /// Returns 64 raw random bits.
    /// </summary>
    /// <returns>A raw unsigned 64-bit value.</returns>
    /// <remarks>
    /// A 64-bit engine consumes one native word. For a 32-bit engine, the first native
    /// word supplies bits 0 through 31 and the second supplies bits 32 through 63.
    /// Any unused cached boolean bits are discarded first.
    /// </remarks>
    public ulong NextUInt64()
    {
        ClearBooleanReservoir();
        if (engine.EngineInfo().WordBits() == 64)
        {
            return engine.NextRawWord();
        }

        ulong low = engine.NextRawWord();
        ulong high = engine.NextRawWord();
        return low | (high << 32);
    }

    /// <summary>
    /// Returns an unbiased unsigned 64-bit integer below an exclusive bound.
    /// </summary>
    /// <param name="bound">The positive exclusive upper bound.</param>
    /// <returns>A uniformly distributed value in <c>[0, bound)</c>.</returns>
    /// <remarks>A bound of one returns zero without consuming random state.</remarks>
    /// <exception cref="RandomArgumentException">The bound is zero.</exception>
    public ulong NextUInt64Bounded(ulong bound)
    {
        return IntegerUniform.NextUInt64(this, bound);
    }

    /// <summary>
    /// Returns an unbiased unsigned 64-bit integer in a half-open range.
    /// </summary>
    /// <param name="minimum">The inclusive lower bound.</param>
    /// <param name="maximum">The exclusive upper bound.</param>
    /// <returns>A uniformly distributed value in <c>[minimum, maximum)</c>.</returns>
    /// <remarks>A range containing one value returns it without consuming random state.</remarks>
    /// <exception cref="RandomArgumentException">The maximum is not greater than the minimum.</exception>
    public ulong NextUInt64InRange(ulong minimum, ulong maximum)
    {
        return IntegerUniform.NextUInt64(this, minimum, maximum);
    }

    /// <summary>
    /// Returns a signed value containing 32 raw random bits.
    /// </summary>
    /// <returns>The same bit pattern as <see cref="NextUInt32()"/>, interpreted as a signed integer.</returns>
    public int NextInt32()
    {
        uint value = NextUInt32();
        if (value <= int.MaxValue)
        {
            return (int)value;
        }

        return int.MinValue + (int)(value - 0x80000000U);
    }

    /// <summary>
    /// Returns an unbiased nonnegative signed 32-bit integer below an exclusive bound.
    /// </summary>
    /// <param name="bound">The positive exclusive upper bound.</param>
    /// <returns>A uniformly distributed value in <c>[0, bound)</c>.</returns>
    /// <remarks>A bound of one returns zero without consuming random state.</remarks>
    /// <exception cref="RandomArgumentException">The bound is not positive.</exception>
    public int NextInt32Bounded(int bound)
    {
        return IntegerUniform.NextInt32(this, bound);
    }

    /// <summary>
    /// Returns an unbiased signed 32-bit integer in a half-open range.
    /// </summary>
    /// <param name="minimum">The inclusive lower bound.</param>
    /// <param name="maximum">The exclusive upper bound.</param>
    /// <returns>A uniformly distributed value in <c>[minimum, maximum)</c>.</returns>
    /// <remarks>A range containing one value returns it without consuming random state.</remarks>
    /// <exception cref="RandomArgumentException">The maximum is not greater than the minimum.</exception>
    public int NextInt32InRange(int minimum, int maximum)
    {
        return IntegerUniform.NextInt32(this, minimum, maximum);
    }

    /// <summary>
    /// Returns a signed value containing 64 raw random bits.
    /// </summary>
    /// <returns>The same bit pattern as <see cref="NextUInt64()"/>, interpreted as a signed integer.</returns>
    public long NextInt64()
    {
        ulong value = NextUInt64();
        if (value <= long.MaxValue)
        {
            return (long)value;
        }

        return long.MinValue + (long)(value - 0x8000000000000000UL);
    }

    /// <summary>
    /// Returns an unbiased nonnegative signed 64-bit integer below an exclusive bound.
    /// </summary>
    /// <param name="bound">The positive exclusive upper bound.</param>
    /// <returns>A uniformly distributed value in <c>[0, bound)</c>.</returns>
    /// <remarks>A bound of one returns zero without consuming random state.</remarks>
    /// <exception cref="RandomArgumentException">The bound is not positive.</exception>
    public long NextInt64Bounded(long bound)
    {
        return IntegerUniform.NextInt64(this, bound);
    }

    /// <summary>
    /// Returns an unbiased signed 64-bit integer in a half-open range.
    /// </summary>
    /// <param name="minimum">The inclusive lower bound.</param>
    /// <param name="maximum">The exclusive upper bound.</param>
    /// <returns>A uniformly distributed value in <c>[minimum, maximum)</c>.</returns>
    /// <remarks>A range containing one value returns it without consuming random state.</remarks>
    /// <exception cref="RandomArgumentException">The maximum is not greater than the minimum.</exception>
    public long NextInt64InRange(long minimum, long maximum)
    {
        return IntegerUniform.NextInt64(this, minimum, maximum);
    }

    /// <summary>
    /// Returns an unbiased nonnegative arbitrary-precision integer below an exclusive bound.
    /// </summary>
    /// <param name="bound">The positive exclusive upper bound.</param>
    /// <returns>A uniformly distributed value in <c>[0, bound)</c>.</returns>
    /// <remarks>A bound of one returns zero without consuming random state.</remarks>
    /// <exception cref="RandomArgumentException">The bound is not positive.</exception>
    public BigInteger NextBigInteger(BigInteger bound)
    {
        return IntegerUniform.NextBigInteger(this, bound);
    }

    /// <summary>
    /// Returns an unbiased arbitrary-precision integer in a half-open range.
    /// </summary>
    /// <param name="minimum">The inclusive lower bound.</param>
    /// <param name="maximum">The exclusive upper bound.</param>
    /// <returns>A uniformly distributed value in <c>[minimum, maximum)</c>.</returns>
    /// <remarks>A range containing one value returns it without consuming random state.</remarks>
    /// <exception cref="RandomArgumentException">The maximum is not greater than the minimum.</exception>
    public BigInteger NextBigIntegerInRange(BigInteger minimum, BigInteger maximum)
    {
        return IntegerUniform.NextBigInteger(this, minimum, maximum);
    }

    /// <summary>
    /// Returns a single-precision value in the closed-open unit interval <c>[0, 1)</c>.
    /// </summary>
    /// <returns>A uniformly selected value from the 24-bit closed-open unit grid.</returns>
    public float NextSingle()
    {
        return FloatingUniform.NextSingleClosedOpen(this);
    }

    /// <summary>
    /// Returns a single-precision value in the closed-open unit interval <c>[0, 1)</c>.
    /// </summary>
    /// <returns>A uniformly selected value from the 24-bit closed-open unit grid.</returns>
    public float NextSingleClosedOpen()
    {
        return FloatingUniform.NextSingleClosedOpen(this);
    }

    /// <summary>
    /// Returns a single-precision value in the open unit interval <c>(0, 1)</c>.
    /// </summary>
    /// <returns>A uniformly selected nonzero value from the 24-bit closed-open unit grid.</returns>
    /// <remarks>Zero-valued bit samples are rejected so neither endpoint can be returned.</remarks>
    public float NextSingleOpenOpen()
    {
        return FloatingUniform.NextSingleOpenOpen(this);
    }

    /// <summary>
    /// Returns a single-precision value in the open-closed unit interval <c>(0, 1]</c>.
    /// </summary>
    /// <returns>A uniformly selected value from the shifted 24-bit unit grid.</returns>
    public float NextSingleOpenClosed()
    {
        return FloatingUniform.NextSingleOpenClosed(this);
    }

    /// <summary>
    /// Returns a single-precision value in the closed unit interval <c>[0, 1]</c>.
    /// </summary>
    /// <returns>A uniformly selected value from the 24-bit unit grid including both endpoints.</returns>
    public float NextSingleClosedClosed()
    {
        return FloatingUniform.NextSingleClosedClosed(this);
    }

    /// <summary>
    /// Returns a single-precision value in the finite closed-open range <c>[minimum, maximum)</c>.
    /// </summary>
    /// <param name="minimum">The finite inclusive lower bound.</param>
    /// <param name="maximum">The finite exclusive upper bound.</param>
    /// <returns>A value in the requested range, or the exact minimum when both bounds compare equal.</returns>
    /// <remarks>
    /// This method uses the closed-open endpoint policy. Equal bounds consume no random state.
    /// Interpolation avoids overflowing the bound difference and cannot round to the excluded maximum.
    /// </remarks>
    /// <exception cref="RandomArgumentException">A bound is non-finite or the maximum is less than the minimum.</exception>
    public float NextSingleInRange(float minimum, float maximum)
    {
        return FloatingUniform.NextSingle(this, minimum, maximum);
    }

    /// <summary>
    /// Returns a double-precision value in the closed-open unit interval <c>[0, 1)</c>.
    /// </summary>
    /// <returns>A uniformly selected value from the 53-bit closed-open unit grid.</returns>
    public double NextDouble()
    {
        return FloatingUniform.NextDoubleClosedOpen(this);
    }

    /// <summary>
    /// Returns a double-precision value in the closed-open unit interval <c>[0, 1)</c>.
    /// </summary>
    /// <returns>A uniformly selected value from the 53-bit closed-open unit grid.</returns>
    public double NextDoubleClosedOpen()
    {
        return FloatingUniform.NextDoubleClosedOpen(this);
    }

    /// <summary>
    /// Returns a double-precision value in the open unit interval <c>(0, 1)</c>.
    /// </summary>
    /// <returns>A uniformly selected nonzero value from the 53-bit closed-open unit grid.</returns>
    /// <remarks>Zero-valued bit samples are rejected so neither endpoint can be returned.</remarks>
    public double NextDoubleOpenOpen()
    {
        return FloatingUniform.NextDoubleOpenOpen(this);
    }

    /// <summary>
    /// Returns a double-precision value in the open-closed unit interval <c>(0, 1]</c>.
    /// </summary>
    /// <returns>A uniformly selected value from the shifted 53-bit unit grid.</returns>
    public double NextDoubleOpenClosed()
    {
        return FloatingUniform.NextDoubleOpenClosed(this);
    }

    /// <summary>
    /// Returns a double-precision value in the closed unit interval <c>[0, 1]</c>.
    /// </summary>
    /// <returns>A uniformly selected value from the 53-bit unit grid including both endpoints.</returns>
    public double NextDoubleClosedClosed()
    {
        return FloatingUniform.NextDoubleClosedClosed(this);
    }

    /// <summary>
    /// Returns a double-precision value in the finite closed-open range <c>[minimum, maximum)</c>.
    /// </summary>
    /// <param name="minimum">The finite inclusive lower bound.</param>
    /// <param name="maximum">The finite exclusive upper bound.</param>
    /// <returns>A value in the requested range, or the exact minimum when both bounds compare equal.</returns>
    /// <remarks>
    /// This method uses the closed-open endpoint policy. Equal bounds consume no random state.
    /// Interpolation avoids overflowing the bound difference and cannot round to the excluded maximum.
    /// </remarks>
    /// <exception cref="RandomArgumentException">A bound is non-finite or the maximum is less than the minimum.</exception>
    public double NextDoubleInRange(double minimum, double maximum)
    {
        return FloatingUniform.NextDouble(this, minimum, maximum);
    }

    /// <summary>
    /// Returns a decimal value in the closed-open unit interval <c>[0, 1)</c>.
    /// </summary>
    /// <returns>A uniformly selected value <c>k * 10^-28</c> for an integer <c>k</c> from zero through <c>10^28 - 1</c>.</returns>
    /// <remarks>
    /// Each candidate is formed from 96 raw bits. Rejection restricts that source domain
    /// to an exact multiple of the scale-28 decimal grid size before reduction.
    /// </remarks>
    public decimal NextDecimal()
    {
        return DecimalUniform.NextDecimal(this);
    }

    /// <summary>
    /// Returns a decimal value in the closed-open range <c>[minimum, maximum)</c>.
    /// </summary>
    /// <param name="minimum">The inclusive lower bound.</param>
    /// <param name="maximum">The exclusive upper bound.</param>
    /// <returns>A value in the requested range, or the exact minimum when both bounds compare equal.</returns>
    /// <remarks>
    /// This method interpolates from the uniform scale-28 decimal grid. Equal bounds consume no
    /// random state, full-width ranges avoid subtraction overflow, and rounding cannot include the maximum.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The maximum is less than the minimum.</exception>
    public decimal NextDecimalInRange(decimal minimum, decimal maximum)
    {
        return DecimalUniform.NextDecimal(this, minimum, maximum);
    }

    /// <summary>
    /// Returns the requested number of low-order random bits.
    /// </summary>
    /// <param name="bitCount">The number of bits from zero through 64 inclusive.</param>
    /// <returns>A value whose bits above the requested count are zero.</returns>
    /// <remarks>
    /// Zero consumes nothing. A request fitting in one native word consumes one word;
    /// larger requests on a 32-bit engine concatenate two words low word first.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The bit count is outside zero through 64.</exception>
    public ulong NextBits(int bitCount)
    {
        ValidateBitCount(bitCount);

        if (bitCount == 0)
        {
            return 0UL;
        }

        ClearBooleanReservoir();
        int wordBits = engine.EngineInfo().WordBits();
        ulong value = engine.NextRawWord();
        if (wordBits == 32 && bitCount > 32)
        {
            value |= engine.NextRawWord() << 32;
        }

        return value & BitMath.LowMask64(bitCount);
    }

    /// <summary>
    /// Returns one independently consumed random bit.
    /// </summary>
    /// <returns><c>true</c> when the next low-order reservoir bit is one; otherwise, <c>false</c>.</returns>
    /// <remarks>Native words are cached and consumed least-significant bit first.</remarks>
    public bool NextBoolean()
    {
        if (booleanBitCount == 0)
        {
            booleanReservoir = engine.NextRawWord();
            booleanBitCount = engine.EngineInfo().WordBits();
        }

        bool result = (booleanReservoir & 1UL) != 0UL;
        booleanReservoir >>= 1;
        booleanBitCount--;
        if (booleanBitCount == 0)
        {
            booleanReservoir = 0UL;
        }

        return result;
    }

    /// <summary>
    /// Returns true with exactly the supplied double-precision probability.
    /// </summary>
    /// <param name="probability">The finite probability from zero through one inclusive.</param>
    /// <returns><c>true</c> with the exact binary probability; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// The probability is compared as its exact reduced binary fraction rather than quantized to a
    /// fixed unit grid. Zero and one return their certain results without consuming random state.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The probability is non-finite or outside zero through one.</exception>
    public bool NextBooleanWithDoubleProbability(double probability)
    {
        return ProbabilityUniform.NextBoolean(this, probability);
    }

    /// <summary>
    /// Returns true with exactly the supplied single-precision probability.
    /// </summary>
    /// <param name="probability">The finite probability from zero through one inclusive.</param>
    /// <returns><c>true</c> with the exact binary probability; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// The probability is compared as its exact reduced binary fraction rather than quantized to a
    /// fixed unit grid. Zero and one return their certain results without consuming random state.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The probability is non-finite or outside zero through one.</exception>
    public bool NextBooleanWithSingleProbability(float probability)
    {
        return ProbabilityUniform.NextBoolean(this, probability);
    }

    /// <summary>
    /// Returns a uniformly random arithmetic sign.
    /// </summary>
    /// <returns>Negative one or positive one with equal probability.</returns>
    public int NextSign()
    {
        return NextBoolean() ? 1 : -1;
    }

    /// <summary>
    /// Returns a value from the Rademacher distribution.
    /// </summary>
    /// <returns>Negative one or positive one with equal probability.</returns>
    public int NextRademacher()
    {
        return NextSign();
    }

    /// <summary>
    /// Creates an array filled from native words in canonical little-endian byte order.
    /// </summary>
    /// <param name="length">The nonnegative byte count.</param>
    /// <returns>A new byte array of the requested length.</returns>
    /// <exception cref="RandomArgumentException">The length is negative.</exception>
    public byte[] NextBytes(int length)
    {
        Guard.Count(length, "length");
        byte[] result = new byte[length];
        FillBytes(result.AsSpan());
        return result;
    }

    /// <summary>
    /// Fills a destination from native words in canonical little-endian byte order.
    /// </summary>
    /// <param name="destination">The destination bytes.</param>
    /// <remarks>
    /// Each native word contributes its least-significant byte first. A final partial
    /// word contributes only the requested low-order bytes. An empty destination consumes nothing.
    /// </remarks>
    public void FillBytes(Span<byte> destination)
    {
        if (destination.Length == 0)
        {
            return;
        }

        ClearBooleanReservoir();
        int bytesPerWord = engine.EngineInfo().WordBits() / 8;
        int offset = 0;
        while (offset < destination.Length)
        {
            ulong word = engine.NextRawWord();
            int byteIndex = 0;
            while (byteIndex < bytesPerWord && offset < destination.Length)
            {
                destination[offset] = (byte)(word & 0xFFUL);
                word >>= 8;
                byteIndex++;
                offset++;
            }
        }
    }

    /// <summary>
    /// Fills bytes in canonical little-endian native-word order.
    /// </summary>
    /// <param name="destination">The destination bytes.</param>
    /// <remarks>This method has the same behavior and state consumption as <see cref="FillBytes"/>.</remarks>
    public void Fill(Span<byte> destination)
    {
        FillBytes(destination);
    }

    /// <summary>
    /// Fills a destination with values containing a requested number of random bits.
    /// </summary>
    /// <param name="destination">The destination values.</param>
    /// <param name="bitCount">The number of low-order bits from zero through 64 inclusive in each value.</param>
    /// <remarks>
    /// Values and final generator state match repeated calls to <see cref="NextBits"/>
    /// in ascending destination order. An empty destination consumes nothing.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The bit count is outside zero through 64.</exception>
    public void FillBits(Span<ulong> destination, int bitCount)
    {
        ValidateBitCount(bitCount);
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = NextBits(bitCount);
            index++;
        }
    }

    /// <summary>
    /// Fills a destination with raw unsigned 32-bit values.
    /// </summary>
    /// <param name="destination">The destination values.</param>
    /// <remarks>The result and state match repeated calls to <see cref="NextUInt32()"/>.</remarks>
    public void FillUInt32(Span<uint> destination)
    {
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = NextUInt32();
            index++;
        }
    }

    /// <summary>
    /// Fills a destination with raw signed 32-bit values.
    /// </summary>
    /// <param name="destination">The destination values.</param>
    /// <remarks>The result and state match repeated calls to <see cref="NextInt32()"/>.</remarks>
    public void FillInt32(Span<int> destination)
    {
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = NextInt32();
            index++;
        }
    }

    /// <summary>
    /// Fills a destination with raw unsigned 64-bit values.
    /// </summary>
    /// <param name="destination">The destination values.</param>
    /// <remarks>The result and state match repeated calls to <see cref="NextUInt64()"/>.</remarks>
    public void FillUInt64(Span<ulong> destination)
    {
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = NextUInt64();
            index++;
        }
    }

    /// <summary>
    /// Fills a destination with raw signed 64-bit values.
    /// </summary>
    /// <param name="destination">The destination values.</param>
    /// <remarks>The result and state match repeated calls to <see cref="NextInt64()"/>.</remarks>
    public void FillInt64(Span<long> destination)
    {
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = NextInt64();
            index++;
        }
    }

    /// <summary>
    /// Fills a destination with single-precision closed-open unit values.
    /// </summary>
    /// <param name="destination">The destination values.</param>
    /// <remarks>The result and state match repeated calls to <see cref="NextSingle()"/>.</remarks>
    public void FillSingles(Span<float> destination)
    {
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = NextSingle();
            index++;
        }
    }

    /// <summary>
    /// Fills a destination with double-precision closed-open unit values.
    /// </summary>
    /// <param name="destination">The destination values.</param>
    /// <remarks>The result and state match repeated calls to <see cref="NextDouble()"/>.</remarks>
    public void FillDoubles(Span<double> destination)
    {
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = NextDouble();
            index++;
        }
    }

    /// <summary>
    /// Fills a destination with scale-28 closed-open unit decimal values.
    /// </summary>
    /// <param name="destination">The destination values.</param>
    /// <remarks>The result and state match repeated calls to <see cref="NextDecimal()"/>.</remarks>
    public void FillDecimals(Span<decimal> destination)
    {
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = NextDecimal();
            index++;
        }
    }

    /// <summary>
    /// Fills a destination with independently consumed random booleans.
    /// </summary>
    /// <param name="destination">The destination values.</param>
    /// <remarks>The result and reservoir state match repeated calls to <see cref="NextBoolean()"/>.</remarks>
    public void FillBooleans(Span<bool> destination)
    {
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = NextBoolean();
            index++;
        }
    }

    /// <summary>
    /// Exports the complete current engine position as a versioned deterministic state.
    /// </summary>
    /// <returns>An independent state snapshot suitable for binary or Base64 persistence.</returns>
    /// <remarks>
    /// The selected math mode is intentionally not part of engine state and must be persisted separately.
    /// State has integrity protection but is neither secret nor authenticated.
    /// </remarks>
    public RandomState ExportState()
    {
        return GeneratorStateCodec.Encode(engine, booleanReservoir, booleanBitCount);
    }

    /// <summary>
    /// Restores the current position from a state belonging to the same named algorithm.
    /// </summary>
    /// <param name="randomState">The complete state to restore.</param>
    /// <remarks>
    /// This operation does not change the factory or import state used by <see cref="Reset"/>.
    /// Validation completes before the current engine is changed.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The state is null.</exception>
    /// <exception cref="RandomStateException">The state is invalid or belongs to a different algorithm.</exception>
    public void RestoreState(RandomState randomState)
    {
        Guard.NotNull(randomState, "randomState");
        if (randomState.Algorithm() != engine.EngineInfo().Algorithm())
        {
            throw RandomStateException.Create("The state belongs to a different random algorithm.");
        }

        GeneratorStateData decoded = GeneratorStateCodec.Decode(randomState);
        engine = decoded.Engine();
        booleanReservoir = decoded.BooleanReservoir();
        booleanBitCount = decoded.BooleanBitCount();
    }

    /// <summary>
    /// Advances an engine that supports arbitrary-distance positioning.
    /// </summary>
    /// <param name="distance">The number of native engine transitions to advance.</param>
    /// <remarks>
    /// PCG64 DXSM uses the complete 128-bit distance. SplitMix64 advances modulo its
    /// <c>2^64</c> state cycle, so its upper distance bits have no effect.
    /// </remarks>
    /// <exception cref="RandomCapabilityException">The selected algorithm does not support arbitrary advance.</exception>
    public void Advance(UInt128 distance)
    {
        if (distance == (UInt128)0)
        {
            RequireCapability(RandomCapability.Advance);
            return;
        }

        IAdvanceableRandomEngine? selected = engine as IAdvanceableRandomEngine;
        RequireCapability(RandomCapability.Advance);
        if (selected == null)
        {
            throw RandomStateException.Create("The selected engine advertises an invalid advance capability.");
        }

        selected.AdvanceEngine(distance);
        ClearBooleanReservoir();
    }

    /// <summary>
    /// Seeks a counter-based engine to an absolute 128-bit block index.
    /// </summary>
    /// <param name="blockIndex">The absolute block index; higher counter words are set to zero.</param>
    /// <remarks>Seeking discards any partially consumed counter block.</remarks>
    /// <exception cref="RandomCapabilityException">The selected algorithm does not support absolute seek.</exception>
    public void Seek(UInt128 blockIndex)
    {
        ISeekableRandomEngine? selected = engine as ISeekableRandomEngine;
        RequireCapability(RandomCapability.Seek);
        if (selected == null)
        {
            throw RandomStateException.Create("The selected engine advertises an invalid seek capability.");
        }

        selected.SeekEngine(blockIndex);
        ClearBooleanReservoir();
    }

    /// <summary>
    /// Seeks a counter-based engine to an exact full-width counter.
    /// </summary>
    /// <param name="absoluteCounter">Four least-significant-word-first 64-bit counter words.</param>
    /// <remarks>Seeking discards any partially consumed counter block.</remarks>
    /// <exception cref="RandomArgumentException">The counter is null or does not contain exactly four words.</exception>
    /// <exception cref="RandomCapabilityException">The selected algorithm does not support absolute seek.</exception>
    public void SeekCounter(ulong[] absoluteCounter)
    {
        ISeekableRandomEngine? selected = engine as ISeekableRandomEngine;
        RequireCapability(RandomCapability.Seek);
        if (selected == null)
        {
            throw RandomStateException.Create("The selected engine advertises an invalid seek capability.");
        }

        selected.SeekEngineCounter(absoluteCounter);
        ClearBooleanReservoir();
    }

    /// <summary>
    /// Reserves consecutive blocks from a counter-based engine.
    /// </summary>
    /// <param name="blockCount">The number of consecutive blocks to reserve.</param>
    /// <returns>
    /// The full 256-bit counter of the first reserved block as four
    /// least-significant-word-first 64-bit words. Together with
    /// the requested block count, this identifies the reserved range.
    /// </returns>
    /// <remarks>
    /// Philox advances its next ungenerated block beyond the reserved range. A partially
    /// consumed earlier block and any cached boolean bits remain available to this generator.
    /// A zero block count reports the next block without changing state. Capability and
    /// exhaustion checks complete before state mutation.
    /// </remarks>
    /// <exception cref="RandomCapabilityException">The selected algorithm does not support counter-block reservation.</exception>
    /// <exception cref="RandomExhaustedException">The reservation extends beyond the Philox counter space.</exception>
    public ulong[] ReserveBlocks(UInt128 blockCount)
    {
        IBlockReservableRandomEngine? selected = engine as IBlockReservableRandomEngine;
        RequireCapability(RandomCapability.BlockReservation);
        if (selected == null)
        {
            throw RandomStateException.Create("The selected engine advertises an invalid block-reservation capability.");
        }

        return selected.ReserveBlocksEngine(blockCount);
    }

    /// <summary>
    /// Applies the selected engine's documented fixed-distance jump.
    /// </summary>
    /// <remarks>For xoshiro256**, the jump is equivalent to <c>2^128</c> native state transitions.</remarks>
    /// <exception cref="RandomCapabilityException">The selected algorithm does not support a fixed jump.</exception>
    public void Jump()
    {
        IJumpableRandomEngine? selected = engine as IJumpableRandomEngine;
        RequireCapability(RandomCapability.Jump);
        if (selected == null)
        {
            throw RandomStateException.Create("The selected engine advertises an invalid jump capability.");
        }

        selected.JumpEngine();
        ClearBooleanReservoir();
    }

    /// <summary>
    /// Applies the selected engine's documented fixed-distance long jump.
    /// </summary>
    /// <remarks>For xoshiro256**, the long jump is equivalent to <c>2^192</c> native state transitions.</remarks>
    /// <exception cref="RandomCapabilityException">The selected algorithm does not support a fixed long jump.</exception>
    public void LongJump()
    {
        IJumpableRandomEngine? selected = engine as IJumpableRandomEngine;
        RequireCapability(RandomCapability.LongJump);
        if (selected == null)
        {
            throw RandomStateException.Create("The selected engine advertises an invalid long-jump capability.");
        }

        selected.LongJumpEngine();
        ClearBooleanReservoir();
    }

    /// <summary>
    /// Restores the stream to the exact state established by its factory or state import.
    /// </summary>
    public void Reset()
    {
        engine = initialEngine.CloneEngine();
        booleanReservoir = initialBooleanReservoir;
        booleanBitCount = initialBooleanBitCount;
    }

    internal ulong NextRawWord()
    {
        ClearBooleanReservoir();
        return engine.NextRawWord();
    }

    internal void ClearBooleanReservoir()
    {
        booleanReservoir = 0UL;
        booleanBitCount = 0;
    }

    internal void RequireCapability(RandomCapability capability)
    {
        RandomAlgorithmInfo engineInfo = engine.EngineInfo();
        if (!engineInfo.HasCapability(capability))
        {
            throw RandomCapabilityException.Create(engineInfo.Algorithm(), capability, $"The {engineInfo.Name()} algorithm does not support the requested {capability} operation.");
        }
    }

    internal static void ValidateMathMode(RandomMathMode mathMode)
    {
        if (mathMode != RandomMathMode.Portable && mathMode != RandomMathMode.System)
        {
            throw RandomArgumentException.Create("mathMode", "The random math mode is not supported.");
        }
    }

    internal static void ValidateSeedAlgorithm(RandomAlgorithm algorithm)
    {
        Guard.Argument(StateCodec.IsDeterministicAlgorithm(algorithm), "algorithm", "Canonical seeding requires a named deterministic algorithm.");
    }

    internal static void ValidateBitCount(int bitCount)
    {
        if (bitCount < 0 || bitCount > 64)
        {
            throw RandomArgumentException.Create("bitCount", "The bit count must be between zero and 64 inclusive.");
        }
    }
}
