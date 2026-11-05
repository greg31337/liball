namespace random;

internal class Xoshiro256StarStarEngine : IRandomEngine, IJumpableRandomEngine
{
    internal const int AlgorithmVersion = 1;
    internal const int StateByteCount = 32;
    const ulong JumpZero = 0x180EC6D33CFD0ABAUL;
    const ulong JumpOne = 0xD5A61266F0C9392CUL;
    const ulong JumpTwo = 0xA9582618E03FC9AAUL;
    const ulong JumpThree = 0x39ABDC4529B1661CUL;
    const ulong LongJumpZero = 0x76E15D3EFEFDCBBFUL;
    const ulong LongJumpOne = 0xC5004E441C522FB3UL;
    const ulong LongJumpTwo = 0x77710069854EE241UL;
    const ulong LongJumpThree = 0x39109BB02ACBE635UL;
    ulong stateZero;
    ulong stateOne;
    ulong stateTwo;
    ulong stateThree;

    internal Xoshiro256StarStarEngine(ulong seed)
    {
        SplitMix64Engine seeder = new SplitMix64Engine(seed);
        stateZero = seeder.NextUInt64();
        stateOne = seeder.NextUInt64();
        stateTwo = seeder.NextUInt64();
        stateThree = seeder.NextUInt64();
    }

    internal static Xoshiro256StarStarEngine FromSeed(ulong seed)
    {
        return new Xoshiro256StarStarEngine(seed);
    }

    internal static Xoshiro256StarStarEngine FromState(RandomState randomState)
    {
        ValidateRandomState(randomState);
        return FromStatePayload(randomState.Payload());
    }

    internal static Xoshiro256StarStarEngine FromStatePayload(byte[] payload)
    {
        ValidatePayload(payload);
        ulong[] stateWords =
        [
            BinaryCodec.ReadUInt64(payload, 0),
            BinaryCodec.ReadUInt64(payload, 8),
            BinaryCodec.ReadUInt64(payload, 16),
            BinaryCodec.ReadUInt64(payload, 24)
        ];
        return FromStateWords(stateWords);
    }

    internal static Xoshiro256StarStarEngine FromStateWords(ulong[] stateWords)
    {
        ValidateStateWords(stateWords);
        Xoshiro256StarStarEngine engine = new Xoshiro256StarStarEngine(0UL);
        engine.stateZero = stateWords[0];
        engine.stateOne = stateWords[1];
        engine.stateTwo = stateWords[2];
        engine.stateThree = stateWords[3];
        return engine;
    }

    internal ulong NextUInt64()
    {
        ulong multiplied = WideMath.MultiplyLow64(stateOne, 5UL);
        ulong result = BitMath.RotateLeft(multiplied, 7);
        result = WideMath.MultiplyLow64(result, 9UL);
        ulong shifted = stateOne << 17;

        stateTwo ^= stateZero;
        stateThree ^= stateOne;
        stateOne ^= stateTwo;
        stateZero ^= stateThree;
        stateTwo ^= shifted;
        stateThree = BitMath.RotateLeft(stateThree, 45);
        return result;
    }

    internal void Jump()
    {
        ApplyJump(JumpZero, JumpOne, JumpTwo, JumpThree);
    }

    internal void LongJump()
    {
        ApplyJump(LongJumpZero, LongJumpOne, LongJumpTwo, LongJumpThree);
    }

    internal Xoshiro256StarStarEngine Clone()
    {
        return FromStateWords(StateWords());
    }

    internal RandomState ExportState()
    {
        return StateCodec.Create(
            RandomAlgorithm.Xoshiro256StarStarV1,
            AlgorithmVersion,
            StateCodec.CurrentTransformVersion,
            StatePayload());
    }

    internal byte[] StatePayload()
    {
        byte[] payload = new byte[StateByteCount];
        BinaryCodec.WriteUInt64(payload, 0, stateZero);
        BinaryCodec.WriteUInt64(payload, 8, stateOne);
        BinaryCodec.WriteUInt64(payload, 16, stateTwo);
        BinaryCodec.WriteUInt64(payload, 24, stateThree);
        return payload;
    }

    internal ulong[] StateWords()
    {
        return [stateZero, stateOne, stateTwo, stateThree];
    }

    internal void RestoreState(RandomState randomState)
    {
        ValidateRandomState(randomState);
        RestoreStatePayload(randomState.Payload());
    }

    internal void RestoreStatePayload(byte[] payload)
    {
        ValidatePayload(payload);
        ulong restoredZero = BinaryCodec.ReadUInt64(payload, 0);
        ulong restoredOne = BinaryCodec.ReadUInt64(payload, 8);
        ulong restoredTwo = BinaryCodec.ReadUInt64(payload, 16);
        ulong restoredThree = BinaryCodec.ReadUInt64(payload, 24);
        stateZero = restoredZero;
        stateOne = restoredOne;
        stateTwo = restoredTwo;
        stateThree = restoredThree;
    }

    internal RandomAlgorithmInfo Info()
    {
        return new RandomAlgorithmInfo(
            RandomAlgorithm.Xoshiro256StarStarV1,
            "xoshiro256**",
            AlgorithmVersion,
            StateCodec.CurrentTransformVersion,
            64,
            StateByteCount,
            false,
            "2^256 - 1 64-bit outputs",
            [RandomCapability.State, RandomCapability.Clone, RandomCapability.Jump, RandomCapability.LongJump]);
    }

    /// <summary>Returns the engine metadata through the common engine contract.</summary>
    /// <returns>The xoshiro256** metadata.</returns>
    public RandomAlgorithmInfo EngineInfo()
    {
        return Info();
    }

    /// <summary>Returns one native engine word through the common engine contract.</summary>
    /// <returns>The next 64-bit xoshiro256** output.</returns>
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
    /// <returns>The current xoshiro256** state.</returns>
    public RandomState ExportEngineState()
    {
        return ExportState();
    }

    /// <summary>Applies the engine's fixed jump through its optional capability contract.</summary>
    public void JumpEngine()
    {
        Jump();
    }

    /// <summary>Applies the engine's fixed long jump through its optional capability contract.</summary>
    public void LongJumpEngine()
    {
        LongJump();
    }

    internal void ApplyJump(ulong first, ulong second, ulong third, ulong fourth)
    {
        ulong accumulatedZero = 0UL;
        ulong accumulatedOne = 0UL;
        ulong accumulatedTwo = 0UL;
        ulong accumulatedThree = 0UL;
        int word = 0;
        while (word < 4)
        {
            ulong polynomial;
            switch (word)
            {
                case 0:
                    polynomial = first;
                    break;
                case 1:
                    polynomial = second;
                    break;
                case 2:
                    polynomial = third;
                    break;
                default:
                    polynomial = fourth;
                    break;
            }

            int bit = 0;
            while (bit < 64)
            {
                if ((polynomial & (1UL << bit)) != 0UL)
                {
                    accumulatedZero ^= stateZero;
                    accumulatedOne ^= stateOne;
                    accumulatedTwo ^= stateTwo;
                    accumulatedThree ^= stateThree;
                }

                NextUInt64();
                bit++;
            }

            word++;
        }

        stateZero = accumulatedZero;
        stateOne = accumulatedOne;
        stateTwo = accumulatedTwo;
        stateThree = accumulatedThree;
    }

    internal static void ValidateRandomState(RandomState randomState)
    {
        Guard.NotNull(randomState, "randomState");
        if (randomState.Algorithm() != RandomAlgorithm.Xoshiro256StarStarV1)
        {
            throw RandomStateException.Create("The state belongs to a different random algorithm.");
        }

        if (randomState.AlgorithmVersion() != AlgorithmVersion)
        {
            throw RandomStateException.Create("The xoshiro256** algorithm version is not supported.");
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
            throw RandomStateException.Create("A xoshiro256** state payload must contain exactly thirty-two bytes.");
        }

        ulong combined = BinaryCodec.ReadUInt64(payload, 0);
        combined |= BinaryCodec.ReadUInt64(payload, 8);
        combined |= BinaryCodec.ReadUInt64(payload, 16);
        combined |= BinaryCodec.ReadUInt64(payload, 24);
        if (combined == 0UL)
        {
            throw RandomStateException.Create("A xoshiro256** state must not be all zero.");
        }
    }

    internal static void ValidateStateWords(ulong[] stateWords)
    {
        Guard.NotNull(stateWords, "stateWords");
        if (stateWords.Length != 4)
        {
            throw RandomStateException.Create("A xoshiro256** state must contain exactly four words.");
        }

        if ((stateWords[0] | stateWords[1] | stateWords[2] | stateWords[3]) == 0UL)
        {
            throw RandomStateException.Create("A xoshiro256** state must not be all zero.");
        }
    }
}
