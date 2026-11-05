using System;

namespace random;

internal class SplitMix64Engine : IRandomEngine, IAdvanceableRandomEngine
{
    internal const int AlgorithmVersion = 1;
    internal const int StateByteCount = 8;
    internal const ulong Increment = 0x9E3779B97F4A7C15UL;
    internal const ulong FirstMultiplier = 0xBF58476D1CE4E5B9UL;
    internal const ulong SecondMultiplier = 0x94D049BB133111EBUL;
    ulong state;

    internal SplitMix64Engine(ulong seed)
    {
        state = seed;
    }

    internal static SplitMix64Engine FromSeed(ulong seed)
    {
        return new SplitMix64Engine(seed);
    }

    internal static SplitMix64Engine FromState(RandomState randomState)
    {
        ValidateRandomState(randomState);
        byte[] payload = randomState.Payload();
        return new SplitMix64Engine(BinaryCodec.ReadUInt64(payload, 0));
    }

    internal static SplitMix64Engine FromStatePayload(byte[] payload)
    {
        ValidatePayload(payload);
        return new SplitMix64Engine(BinaryCodec.ReadUInt64(payload, 0));
    }

    internal ulong NextUInt64()
    {
        state = WideMath.AddModulo64(state, Increment);
        ulong mixed = state ^ (state >> 30);
        mixed = WideMath.MultiplyLow64(mixed, FirstMultiplier);
        mixed ^= mixed >> 27;
        mixed = WideMath.MultiplyLow64(mixed, SecondMultiplier);
        return mixed ^ (mixed >> 31);
    }

    internal void Advance(ulong distance)
    {
        ulong displacement = WideMath.MultiplyLow64(Increment, distance);
        state = WideMath.AddModulo64(state, displacement);
    }

    internal SplitMix64Engine Clone()
    {
        return new SplitMix64Engine(state);
    }

    internal RandomState ExportState()
    {
        return StateCodec.Create(
            RandomAlgorithm.SplitMix64V1,
            AlgorithmVersion,
            StateCodec.CurrentTransformVersion,
            StatePayload());
    }

    internal byte[] StatePayload()
    {
        byte[] payload = new byte[StateByteCount];
        BinaryCodec.WriteUInt64(payload, 0, state);
        return payload;
    }

    internal void RestoreState(RandomState randomState)
    {
        ValidateRandomState(randomState);
        byte[] payload = randomState.Payload();
        state = BinaryCodec.ReadUInt64(payload, 0);
    }

    internal void RestoreStatePayload(byte[] payload)
    {
        ValidatePayload(payload);
        state = BinaryCodec.ReadUInt64(payload, 0);
    }

    internal ulong StateValue()
    {
        return state;
    }

    internal RandomAlgorithmInfo Info()
    {
        return new RandomAlgorithmInfo(
            RandomAlgorithm.SplitMix64V1,
            "SplitMix64",
            AlgorithmVersion,
            StateCodec.CurrentTransformVersion,
            64,
            StateByteCount,
            false,
            "2^64 64-bit outputs",
            [RandomCapability.State, RandomCapability.Clone, RandomCapability.Advance]);
    }

    /// <summary>Returns the engine metadata through the common engine contract.</summary>
    /// <returns>The SplitMix64 metadata.</returns>
    public RandomAlgorithmInfo EngineInfo()
    {
        return Info();
    }

    /// <summary>Returns one native engine word through the common engine contract.</summary>
    /// <returns>The next 64-bit SplitMix64 output.</returns>
    public ulong NextRawWord()
    {
        return NextUInt64();
    }

    /// <summary>Clones the engine through the common engine contract.</summary>
    /// <returns>An independent engine at the same position.</returns>
    public IRandomEngine CloneEngine()
    {
        return Clone();
    }

    /// <summary>Exports state through the common engine contract.</summary>
    /// <returns>The current SplitMix64 state.</returns>
    public RandomState ExportEngineState()
    {
        return ExportState();
    }

    /// <summary>Advances the engine through its optional capability contract.</summary>
    /// <param name="distance">The distance whose low 64 bits select the cycle offset.</param>
    public void AdvanceEngine(UInt128 distance)
    {
        Advance(WideMath.Low64(distance));
    }

    internal static void ValidateRandomState(RandomState randomState)
    {
        Guard.NotNull(randomState, "randomState");
        if (randomState.Algorithm() != RandomAlgorithm.SplitMix64V1)
        {
            throw RandomStateException.Create("The state belongs to a different random algorithm.");
        }

        if (randomState.AlgorithmVersion() != AlgorithmVersion)
        {
            throw RandomStateException.Create("The SplitMix64 algorithm version is not supported.");
        }

        if (randomState.TransformVersion() != StateCodec.CurrentTransformVersion)
        {
            throw RandomStateException.Create("The random transform version is not supported.");
        }

        ValidatePayload(randomState.Payload());
    }

    internal static void ValidatePayload(byte[] payload)
    {
        Guard.NotNull(payload, "payload");
        if (payload.Length != StateByteCount)
        {
            throw RandomStateException.Create("A SplitMix64 state payload must contain exactly eight bytes.");
        }
    }
}
