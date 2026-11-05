namespace random;

internal class Mt19937Engine : IRandomEngine
{
    internal const int AlgorithmVersion = 1;
    internal const int StateWordCount = 624;
    internal const int StateByteCount = 2500;
    internal const int MiddleWord = 397;
    internal const uint MatrixA = 0x9908B0DFU;
    internal const uint UpperMask = 0x80000000U;
    internal const uint LowerMask = 0x7FFFFFFFU;
    uint[] state;
    int index;

    internal Mt19937Engine(uint seed)
    {
        state = new uint[StateWordCount];
        Initialize(seed);
    }

    internal static Mt19937Engine FromSeed(uint seed)
    {
        return new Mt19937Engine(seed);
    }

    internal static Mt19937Engine FromSeedArray(uint[] seed)
    {
        Guard.NotNull(seed, "seed");
        if (seed.Length == 0)
        {
            throw RandomArgumentException.Create("seed", "An MT19937 array seed must contain at least one word.");
        }

        Mt19937Engine engine = new Mt19937Engine(0U);
        engine.InitializeByArray(seed);
        return engine;
    }

    internal static Mt19937Engine FromState(RandomState randomState)
    {
        ValidateRandomState(randomState);
        return FromStatePayload(randomState.Payload());
    }

    internal static Mt19937Engine FromStatePayload(byte[] payload)
    {
        ValidatePayload(payload);
        uint[] restoredState = ReadStateWords(payload);
        int restoredIndex = (int)BinaryCodec.ReadUInt32(payload, 0);
        return FromStateWords(restoredState, restoredIndex);
    }

    internal static Mt19937Engine FromStateWords(uint[] stateWords, int stateIndex)
    {
        ValidateStateWords(stateWords, stateIndex);
        Mt19937Engine engine = new Mt19937Engine(0U);
        engine.state = (uint[])stateWords.Clone();
        engine.index = stateIndex;
        return engine;
    }

    internal uint NextUInt32()
    {
        if (index >= StateWordCount)
        {
            Twist();
        }

        uint value = state[index];
        index++;
        value ^= value >> 11;
        value ^= (value << 7) & 0x9D2C5680U;
        value ^= (value << 15) & 0xEFC60000U;
        value ^= value >> 18;
        return value;
    }

    internal Mt19937Engine Clone()
    {
        return FromStateWords(state, index);
    }

    internal RandomState ExportState()
    {
        return StateCodec.Create(
            RandomAlgorithm.Mt19937V1,
            AlgorithmVersion,
            StateCodec.CurrentTransformVersion,
            StatePayload());
    }

    internal byte[] StatePayload()
    {
        byte[] payload = new byte[StateByteCount];
        BinaryCodec.WriteUInt32(payload, 0, (uint)index);
        int wordIndex = 0;
        while (wordIndex < StateWordCount)
        {
            BinaryCodec.WriteUInt32(payload, 4 + (wordIndex * 4), state[wordIndex]);
            wordIndex++;
        }

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
        uint[] restoredState = ReadStateWords(payload);
        int restoredIndex = (int)BinaryCodec.ReadUInt32(payload, 0);
        state = restoredState;
        index = restoredIndex;
    }

    internal uint[] StateWords()
    {
        return (uint[])state.Clone();
    }

    internal int StateIndex()
    {
        return index;
    }

    internal RandomAlgorithmInfo Info()
    {
        return new RandomAlgorithmInfo(
            RandomAlgorithm.Mt19937V1,
            "MT19937",
            AlgorithmVersion,
            StateCodec.CurrentTransformVersion,
            32,
            StateByteCount,
            false,
            "2^19937 - 1 32-bit outputs",
            [RandomCapability.State, RandomCapability.Clone]);
    }

    /// <summary>Returns the engine metadata through the common engine contract.</summary>
    /// <returns>The MT19937 metadata.</returns>
    public RandomAlgorithmInfo EngineInfo()
    {
        return Info();
    }

    /// <summary>Returns one native engine word through the common engine contract.</summary>
    /// <returns>The next 32-bit MT19937 output widened to 64 bits.</returns>
    public ulong NextRawWord()
    {
        return (ulong)NextUInt32();
    }

    /// <summary>Clones the engine through the common engine contract.</summary>
    /// <returns>An independent engine at the same position.</returns>
    public IRandomEngine CloneEngine()
    {
        return Clone();
    }

    /// <summary>Exports state through the common engine contract.</summary>
    /// <returns>The current MT19937 state.</returns>
    public RandomState ExportEngineState()
    {
        return ExportState();
    }

    internal void Initialize(uint seed)
    {
        state[0] = seed;
        int stateIndex = 1;
        while (stateIndex < StateWordCount)
        {
            uint previous = state[stateIndex - 1];
            uint mixed = previous ^ (previous >> 30);
            state[stateIndex] = AddModulo32(MultiplyModulo32(1812433253U, mixed), (uint)stateIndex);
            stateIndex++;
        }

        index = StateWordCount;
    }

    internal void InitializeByArray(uint[] seed)
    {
        Initialize(19650218U);
        int stateIndex = 1;
        int seedIndex = 0;
        int remaining = StateWordCount;
        if (seed.Length > remaining)
        {
            remaining = seed.Length;
        }

        while (remaining > 0)
        {
            uint previous = state[stateIndex - 1];
            uint mixed = MultiplyModulo32(previous ^ (previous >> 30), 1664525U);
            uint value = state[stateIndex] ^ mixed;
            value = AddModulo32(value, seed[seedIndex]);
            state[stateIndex] = AddModulo32(value, (uint)seedIndex);
            stateIndex++;
            seedIndex++;
            if (stateIndex >= StateWordCount)
            {
                state[0] = state[StateWordCount - 1];
                stateIndex = 1;
            }

            if (seedIndex >= seed.Length)
            {
                seedIndex = 0;
            }

            remaining--;
        }

        remaining = StateWordCount - 1;
        while (remaining > 0)
        {
            uint previous = state[stateIndex - 1];
            uint mixed = MultiplyModulo32(previous ^ (previous >> 30), 1566083941U);
            uint value = state[stateIndex] ^ mixed;
            state[stateIndex] = SubtractModulo32(value, (uint)stateIndex);
            stateIndex++;
            if (stateIndex >= StateWordCount)
            {
                state[0] = state[StateWordCount - 1];
                stateIndex = 1;
            }

            remaining--;
        }

        state[0] = UpperMask;
        index = StateWordCount;
    }

    internal void Twist()
    {
        int stateIndex = 0;
        while (stateIndex < StateWordCount - MiddleWord)
        {
            uint combined = (state[stateIndex] & UpperMask) | (state[stateIndex + 1] & LowerMask);
            uint matrix = (combined & 1U) == 0U ? 0U : MatrixA;
            state[stateIndex] = state[stateIndex + MiddleWord] ^ (combined >> 1) ^ matrix;
            stateIndex++;
        }

        while (stateIndex < StateWordCount - 1)
        {
            uint combined = (state[stateIndex] & UpperMask) | (state[stateIndex + 1] & LowerMask);
            uint matrix = (combined & 1U) == 0U ? 0U : MatrixA;
            state[stateIndex] = state[stateIndex + MiddleWord - StateWordCount] ^ (combined >> 1) ^ matrix;
            stateIndex++;
        }

        uint finalCombined = (state[StateWordCount - 1] & UpperMask) | (state[0] & LowerMask);
        uint finalMatrix = (finalCombined & 1U) == 0U ? 0U : MatrixA;
        state[StateWordCount - 1] = state[MiddleWord - 1] ^ (finalCombined >> 1) ^ finalMatrix;
        index = 0;
    }

    internal static uint MultiplyModulo32(uint left, uint right)
    {
        return (uint)(((ulong)left * (ulong)right) & uint.MaxValue);
    }

    internal static uint AddModulo32(uint left, uint right)
    {
        return (uint)(((ulong)left + (ulong)right) & uint.MaxValue);
    }

    internal static uint SubtractModulo32(uint left, uint right)
    {
        if (left >= right)
        {
            return left - right;
        }

        return uint.MaxValue - (right - left) + 1U;
    }

    internal static uint[] ReadStateWords(byte[] payload)
    {
        uint[] restoredState = new uint[StateWordCount];
        int stateIndex = 0;
        while (stateIndex < StateWordCount)
        {
            restoredState[stateIndex] = BinaryCodec.ReadUInt32(payload, 4 + (stateIndex * 4));
            stateIndex++;
        }

        return restoredState;
    }

    internal static void ValidateRandomState(RandomState randomState)
    {
        Guard.NotNull(randomState, "randomState");
        if (randomState.Algorithm() != RandomAlgorithm.Mt19937V1)
        {
            throw RandomStateException.Create("The state belongs to a different random algorithm.");
        }

        if (randomState.AlgorithmVersion() != AlgorithmVersion)
        {
            throw RandomStateException.Create("The MT19937 algorithm version is not supported.");
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
            throw RandomStateException.Create("An MT19937 state payload must contain exactly two thousand five hundred bytes.");
        }

        uint restoredIndex = BinaryCodec.ReadUInt32(payload, 0);
        if (restoredIndex > StateWordCount)
        {
            throw RandomStateException.Create("An MT19937 state index must be between zero and six hundred twenty-four.");
        }

        bool hasNonzeroWord = false;
        int stateIndex = 0;
        while (stateIndex < StateWordCount)
        {
            if (BinaryCodec.ReadUInt32(payload, 4 + (stateIndex * 4)) != 0U)
            {
                hasNonzeroWord = true;
                break;
            }

            stateIndex++;
        }

        if (!hasNonzeroWord)
        {
            throw RandomStateException.Create("An MT19937 state must contain at least one nonzero word.");
        }
    }

    internal static void ValidateStateWords(uint[] stateWords, int stateIndex)
    {
        Guard.NotNull(stateWords, "stateWords");
        if (stateWords.Length != StateWordCount)
        {
            throw RandomStateException.Create("An MT19937 state must contain exactly six hundred twenty-four words.");
        }

        if (stateIndex < 0 || stateIndex > StateWordCount)
        {
            throw RandomStateException.Create("An MT19937 state index must be between zero and six hundred twenty-four.");
        }

        bool hasNonzeroWord = false;
        int wordIndex = 0;
        while (wordIndex < StateWordCount)
        {
            if (stateWords[wordIndex] != 0U)
            {
                hasNonzeroWord = true;
                break;
            }

            wordIndex++;
        }

        if (!hasNonzeroWord)
        {
            throw RandomStateException.Create("An MT19937 state must contain at least one nonzero word.");
        }
    }
}
