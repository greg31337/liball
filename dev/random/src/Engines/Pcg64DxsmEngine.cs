using System;

namespace random;

internal class Pcg64DxsmEngine : IRandomEngine, IAdvanceableRandomEngine
{
    internal const int AlgorithmVersion = 1;
    internal const int StateByteCount = 32;
    internal const ulong CheapMultiplier = 0xDA942042E4DD58B5UL;
    UInt128 state;
    UInt128 increment;

    internal Pcg64DxsmEngine(UInt128 initialState, UInt128 streamSelector)
    {
        state = (UInt128)0;
        increment = (streamSelector << 1) | (UInt128)1;
        Step();
        state = WideMath.AddModulo128(state, initialState);
        Step();
    }

    internal static Pcg64DxsmEngine FromSeed(UInt128 initialState, UInt128 streamSelector)
    {
        return new Pcg64DxsmEngine(initialState, streamSelector);
    }

    internal static Pcg64DxsmEngine FromState(RandomState randomState)
    {
        ValidateRandomState(randomState);
        return FromStatePayload(randomState.Payload());
    }

    internal static Pcg64DxsmEngine FromStatePayload(byte[] payload)
    {
        ValidatePayload(payload);
        UInt128 restoredState = BinaryCodec.ReadUInt128(payload, 0);
        UInt128 restoredIncrement = BinaryCodec.ReadUInt128(payload, 16);
        return FromStateValues(restoredState, restoredIncrement);
    }

    internal static Pcg64DxsmEngine FromStateValues(UInt128 restoredState, UInt128 restoredIncrement)
    {
        if ((restoredIncrement & (UInt128)1) == (UInt128)0)
        {
            throw RandomStateException.Create("A PCG64 DXSM stream increment must be odd.");
        }

        Pcg64DxsmEngine engine = new Pcg64DxsmEngine((UInt128)0, (UInt128)0);
        engine.state = restoredState;
        engine.increment = restoredIncrement;
        return engine;
    }

    internal ulong NextUInt64()
    {
        ulong high = WideMath.High64(state);
        ulong low = WideMath.Low64(state) | 1UL;
        high ^= high >> 32;
        high = WideMath.MultiplyLow64(high, CheapMultiplier);
        high ^= high >> 48;
        high = WideMath.MultiplyLow64(high, low);
        Step();
        return high;
    }

    internal void Advance(UInt128 distance)
    {
        UInt128 currentMultiplier = (UInt128)CheapMultiplier;
        UInt128 currentIncrement = increment;
        UInt128 accumulatedMultiplier = (UInt128)1;
        UInt128 accumulatedIncrement = (UInt128)0;
        UInt128 remaining = distance;
        while (remaining != (UInt128)0)
        {
            if ((remaining & (UInt128)1) != (UInt128)0)
            {
                accumulatedMultiplier = WideMath.MultiplyModulo128(accumulatedMultiplier, currentMultiplier);
                accumulatedIncrement = WideMath.MultiplyAddModulo128(accumulatedIncrement, currentMultiplier, currentIncrement);
            }

            UInt128 multiplierPlusOne = WideMath.AddModulo128(currentMultiplier, (UInt128)1);
            currentIncrement = WideMath.MultiplyModulo128(multiplierPlusOne, currentIncrement);
            currentMultiplier = WideMath.MultiplyModulo128(currentMultiplier, currentMultiplier);
            remaining >>= 1;
        }

        state = WideMath.MultiplyAddModulo128(state, accumulatedMultiplier, accumulatedIncrement);
    }

    internal Pcg64DxsmEngine Clone()
    {
        Pcg64DxsmEngine clone = new Pcg64DxsmEngine((UInt128)0, (UInt128)0);
        clone.state = state;
        clone.increment = increment;
        return clone;
    }

    internal RandomState ExportState()
    {
        return StateCodec.Create(
            RandomAlgorithm.Pcg64DxsmV1,
            AlgorithmVersion,
            StateCodec.CurrentTransformVersion,
            StatePayload());
    }

    internal byte[] StatePayload()
    {
        byte[] payload = new byte[StateByteCount];
        BinaryCodec.WriteUInt128(payload, 0, state);
        BinaryCodec.WriteUInt128(payload, 16, increment);
        return payload;
    }

    internal void RestoreState(RandomState randomState)
    {
        ValidateRandomState(randomState);
        RestoreStatePayload(randomState.Payload());
    }

    internal void RestoreStatePayload(byte[] payload)
    {
        ValidatePayload(payload);
        UInt128 restoredState = BinaryCodec.ReadUInt128(payload, 0);
        UInt128 restoredIncrement = BinaryCodec.ReadUInt128(payload, 16);
        state = restoredState;
        increment = restoredIncrement;
    }

    internal UInt128 StateValue()
    {
        return state;
    }

    internal UInt128 IncrementValue()
    {
        return increment;
    }

    internal RandomAlgorithmInfo Info()
    {
        return new RandomAlgorithmInfo(
            RandomAlgorithm.Pcg64DxsmV1,
            "PCG64 DXSM",
            AlgorithmVersion,
            StateCodec.CurrentTransformVersion,
            64,
            StateByteCount,
            false,
            "2^128 64-bit outputs per stream",
            [RandomCapability.State, RandomCapability.Clone, RandomCapability.Advance]);
    }

    /// <summary>Returns the engine metadata through the common engine contract.</summary>
    /// <returns>The PCG64 DXSM metadata.</returns>
    public RandomAlgorithmInfo EngineInfo()
    {
        return Info();
    }

    /// <summary>Returns one native engine word through the common engine contract.</summary>
    /// <returns>The next 64-bit PCG64 DXSM output.</returns>
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
    /// <returns>The current PCG64 DXSM state.</returns>
    public RandomState ExportEngineState()
    {
        return ExportState();
    }

    /// <summary>Advances the engine through its optional capability contract.</summary>
    /// <param name="distance">The number of state transitions.</param>
    public void AdvanceEngine(UInt128 distance)
    {
        Advance(distance);
    }

    internal void Step()
    {
        state = WideMath.MultiplyAddModulo128(state, (UInt128)CheapMultiplier, increment);
    }

    internal static void ValidateRandomState(RandomState randomState)
    {
        Guard.NotNull(randomState, "randomState");
        if (randomState.Algorithm() != RandomAlgorithm.Pcg64DxsmV1)
        {
            throw RandomStateException.Create("The state belongs to a different random algorithm.");
        }

        if (randomState.AlgorithmVersion() != AlgorithmVersion)
        {
            throw RandomStateException.Create("The PCG64 DXSM algorithm version is not supported.");
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
            throw RandomStateException.Create("A PCG64 DXSM state payload must contain exactly thirty-two bytes.");
        }

        UInt128 restoredIncrement = BinaryCodec.ReadUInt128(payload, 16);
        if ((restoredIncrement & (UInt128)1) == (UInt128)0)
        {
            throw RandomStateException.Create("A PCG64 DXSM stream increment must be odd.");
        }
    }
}
