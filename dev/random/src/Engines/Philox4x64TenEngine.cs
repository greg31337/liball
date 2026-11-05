using System;

namespace random;

internal class Philox4x64TenEngine : IRandomEngine, ISeekableRandomEngine, IBlockReservableRandomEngine
{
    internal const int AlgorithmVersion = 1;
    internal const int StateByteCount = 88;
    internal const int BlockWordCount = 4;
    internal const int RoundCount = 10;
    internal const ulong MultiplierZero = 0xD2E7470EE14C6C93UL;
    internal const ulong MultiplierOne = 0xCA5A826395121157UL;
    internal const ulong WeylZero = 0x9E3779B97F4A7C15UL;
    internal const ulong WeylOne = 0xBB67AE8584CAA73BUL;
    ulong counterZero;
    ulong counterOne;
    ulong counterTwo;
    ulong counterThree;
    ulong keyZero;
    ulong keyOne;
    ulong bufferZero;
    ulong bufferOne;
    ulong bufferTwo;
    ulong bufferThree;
    int bufferIndex;
    bool counterExhausted;

    internal Philox4x64TenEngine(ulong[] key, ulong[] counter)
    {
        ValidateKey(key);
        ValidateCounter(counter, "counter");
        counterZero = counter[0];
        counterOne = counter[1];
        counterTwo = counter[2];
        counterThree = counter[3];
        keyZero = key[0];
        keyOne = key[1];
        bufferIndex = BlockWordCount;
    }

    internal static Philox4x64TenEngine FromKey(UInt128 key)
    {
        return new Philox4x64TenEngine(
            [WideMath.Low64(key), WideMath.High64(key)],
            [0UL, 0UL, 0UL, 0UL]);
    }

    internal static Philox4x64TenEngine FromKeyAndCounter(ulong[] key, ulong[] counter)
    {
        return new Philox4x64TenEngine(key, counter);
    }

    internal static Philox4x64TenEngine FromState(RandomState randomState)
    {
        ValidateRandomState(randomState);
        return FromStatePayload(randomState.Payload());
    }

    internal static Philox4x64TenEngine FromStatePayload(byte[] payload)
    {
        ValidatePayload(payload);
        ulong[] counter =
        [
            BinaryCodec.ReadUInt64(payload, 0),
            BinaryCodec.ReadUInt64(payload, 8),
            BinaryCodec.ReadUInt64(payload, 16),
            BinaryCodec.ReadUInt64(payload, 24)
        ];
        ulong[] key = [BinaryCodec.ReadUInt64(payload, 32), BinaryCodec.ReadUInt64(payload, 40)];
        Philox4x64TenEngine engine = new Philox4x64TenEngine(key, counter);
        engine.bufferZero = BinaryCodec.ReadUInt64(payload, 48);
        engine.bufferOne = BinaryCodec.ReadUInt64(payload, 56);
        engine.bufferTwo = BinaryCodec.ReadUInt64(payload, 64);
        engine.bufferThree = BinaryCodec.ReadUInt64(payload, 72);
        engine.bufferIndex = (int)BinaryCodec.ReadUInt32(payload, 80);
        engine.counterExhausted = BinaryCodec.ReadByte(payload, 84) != 0;
        return engine;
    }

    internal static ulong[] GenerateBlock(ulong[] counter, ulong[] key)
    {
        ValidateCounter(counter, "counter");
        ValidateKey(key);
        ulong counter0 = counter[0];
        ulong counter1 = counter[1];
        ulong counter2 = counter[2];
        ulong counter3 = counter[3];
        ulong roundKey0 = key[0];
        ulong roundKey1 = key[1];
        int round = 0;
        while (round < RoundCount)
        {
            UInt128 firstProduct = WideMath.Multiply64To128(MultiplierZero, counter0);
            UInt128 secondProduct = WideMath.Multiply64To128(MultiplierOne, counter2);
            ulong next0 = WideMath.High64(secondProduct) ^ counter1 ^ roundKey0;
            ulong next1 = WideMath.Low64(secondProduct);
            ulong next2 = WideMath.High64(firstProduct) ^ counter3 ^ roundKey1;
            ulong next3 = WideMath.Low64(firstProduct);
            counter0 = next0;
            counter1 = next1;
            counter2 = next2;
            counter3 = next3;
            if (round + 1 < RoundCount)
            {
                roundKey0 = WideMath.AddModulo64(roundKey0, WeylZero);
                roundKey1 = WideMath.AddModulo64(roundKey1, WeylOne);
            }

            round++;
        }

        return [counter0, counter1, counter2, counter3];
    }

    internal ulong NextUInt64()
    {
        if (bufferIndex == BlockWordCount)
        {
            FillBuffer();
        }

        ulong result;
        switch (bufferIndex)
        {
            case 0:
                result = bufferZero;
                break;
            case 1:
                result = bufferOne;
                break;
            case 2:
                result = bufferTwo;
                break;
            default:
                result = bufferThree;
                break;
        }

        bufferIndex++;
        if (bufferIndex == BlockWordCount)
        {
            ClearBuffer();
        }

        return result;
    }

    internal void Seek(UInt128 blockIndex)
    {
        counterZero = WideMath.Low64(blockIndex);
        counterOne = WideMath.High64(blockIndex);
        counterTwo = 0UL;
        counterThree = 0UL;
        counterExhausted = false;
        bufferIndex = BlockWordCount;
        ClearBuffer();
    }

    internal void SeekCounter(ulong[] absoluteCounter)
    {
        ValidateCounter(absoluteCounter, "absoluteCounter");
        ulong nextZero = absoluteCounter[0];
        ulong nextOne = absoluteCounter[1];
        ulong nextTwo = absoluteCounter[2];
        ulong nextThree = absoluteCounter[3];
        counterZero = nextZero;
        counterOne = nextOne;
        counterTwo = nextTwo;
        counterThree = nextThree;
        counterExhausted = false;
        bufferIndex = BlockWordCount;
        ClearBuffer();
    }

    internal ulong[] ReserveBlocks(UInt128 blockCount)
    {
        ulong[] start = CounterWords();
        if (blockCount == (UInt128)0)
        {
            return start;
        }

        if (counterExhausted)
        {
            throw RandomExhaustedException.Create("The Philox counter space is exhausted.");
        }

        UInt128 lowHalf = WideMath.FromParts(counterOne, counterZero);
        UInt128 highHalf = WideMath.FromParts(counterThree, counterTwo);
        UInt128 nextLow = WideMath.AddModulo128(lowHalf, blockCount);
        bool carry = nextLow < lowHalf;
        UInt128 nextHigh = highHalf;
        bool exhausts = false;
        if (carry)
        {
            if (highHalf == UInt128.MaxValue)
            {
                if (nextLow != (UInt128)0)
                {
                    throw RandomExhaustedException.Create("The block reservation exceeds the Philox counter space.");
                }

                exhausts = true;
            }
            else
            {
                nextHigh = highHalf + (UInt128)1;
            }
        }

        if (exhausts)
        {
            counterZero = ulong.MaxValue;
            counterOne = ulong.MaxValue;
            counterTwo = ulong.MaxValue;
            counterThree = ulong.MaxValue;
            counterExhausted = true;
        }
        else
        {
            counterZero = WideMath.Low64(nextLow);
            counterOne = WideMath.High64(nextLow);
            counterTwo = WideMath.Low64(nextHigh);
            counterThree = WideMath.High64(nextHigh);
        }

        return start;
    }

    internal Philox4x64TenEngine Clone()
    {
        return FromStatePayload(StatePayload());
    }

    internal RandomState ExportState()
    {
        return StateCodec.Create(
            RandomAlgorithm.Philox4x64TenV1,
            AlgorithmVersion,
            StateCodec.CurrentTransformVersion,
            StatePayload());
    }

    internal byte[] StatePayload()
    {
        byte[] payload = new byte[StateByteCount];
        BinaryCodec.WriteUInt64(payload, 0, counterZero);
        BinaryCodec.WriteUInt64(payload, 8, counterOne);
        BinaryCodec.WriteUInt64(payload, 16, counterTwo);
        BinaryCodec.WriteUInt64(payload, 24, counterThree);
        BinaryCodec.WriteUInt64(payload, 32, keyZero);
        BinaryCodec.WriteUInt64(payload, 40, keyOne);
        BinaryCodec.WriteUInt64(payload, 48, bufferZero);
        BinaryCodec.WriteUInt64(payload, 56, bufferOne);
        BinaryCodec.WriteUInt64(payload, 64, bufferTwo);
        BinaryCodec.WriteUInt64(payload, 72, bufferThree);
        BinaryCodec.WriteUInt32(payload, 80, (uint)bufferIndex);
        BinaryCodec.WriteByte(payload, 84, counterExhausted ? (byte)1 : (byte)0);
        return payload;
    }

    internal void RestoreState(RandomState randomState)
    {
        ValidateRandomState(randomState);
        RestoreStatePayload(randomState.Payload());
    }

    internal void RestoreStatePayload(byte[] payload)
    {
        Philox4x64TenEngine restored = FromStatePayload(payload);
        counterZero = restored.counterZero;
        counterOne = restored.counterOne;
        counterTwo = restored.counterTwo;
        counterThree = restored.counterThree;
        keyZero = restored.keyZero;
        keyOne = restored.keyOne;
        bufferZero = restored.bufferZero;
        bufferOne = restored.bufferOne;
        bufferTwo = restored.bufferTwo;
        bufferThree = restored.bufferThree;
        bufferIndex = restored.bufferIndex;
        counterExhausted = restored.counterExhausted;
    }

    internal ulong[] CounterWords()
    {
        return [counterZero, counterOne, counterTwo, counterThree];
    }

    internal ulong[] KeyWords()
    {
        return [keyZero, keyOne];
    }

    internal int BufferedLaneIndex()
    {
        return bufferIndex;
    }

    internal bool CounterExhausted()
    {
        return counterExhausted;
    }

    internal RandomAlgorithmInfo Info()
    {
        return new RandomAlgorithmInfo(
            RandomAlgorithm.Philox4x64TenV1,
            "Philox4x64-10",
            AlgorithmVersion,
            StateCodec.CurrentTransformVersion,
            64,
            StateByteCount,
            false,
            "2^256 counter blocks of four 64-bit outputs per key; exhausted after the final block",
            [RandomCapability.State, RandomCapability.Clone, RandomCapability.Seek, RandomCapability.BlockReservation]);
    }

    /// <summary>Returns the engine metadata through the common engine contract.</summary>
    /// <returns>The Philox4x64-10 metadata.</returns>
    public RandomAlgorithmInfo EngineInfo()
    {
        return Info();
    }

    /// <summary>Returns one native engine word through the common engine contract.</summary>
    /// <returns>The next 64-bit Philox output.</returns>
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
    /// <returns>The current Philox state.</returns>
    public RandomState ExportEngineState()
    {
        return ExportState();
    }

    /// <summary>Seeks by a 128-bit block index through the optional capability contract.</summary>
    /// <param name="blockIndex">The absolute low 128-bit block index.</param>
    public void SeekEngine(UInt128 blockIndex)
    {
        Seek(blockIndex);
    }

    /// <summary>Seeks by a full counter through the optional capability contract.</summary>
    /// <param name="absoluteCounter">The exact least-significant-word-first counter.</param>
    public void SeekEngineCounter(ulong[] absoluteCounter)
    {
        SeekCounter(absoluteCounter);
    }

    /// <summary>Reserves counter blocks through the optional capability contract.</summary>
    /// <param name="blockCount">The number of consecutive blocks to reserve.</param>
    /// <returns>The full-width start counter in least-significant-word-first order.</returns>
    public ulong[] ReserveBlocksEngine(UInt128 blockCount)
    {
        return ReserveBlocks(blockCount);
    }

    internal void FillBuffer()
    {
        if (counterExhausted)
        {
            throw RandomExhaustedException.Create("The Philox counter space is exhausted.");
        }

        ulong[] generated = GenerateBlock(CounterWords(), KeyWords());
        bool finalCounter = CounterIsMaximum();
        if (!finalCounter)
        {
            IncrementCounter();
        }

        bufferZero = generated[0];
        bufferOne = generated[1];
        bufferTwo = generated[2];
        bufferThree = generated[3];
        bufferIndex = 0;
        counterExhausted = finalCounter;
    }

    internal void IncrementCounter()
    {
        counterZero = WideMath.AddModulo64(counterZero, 1UL);
        if (counterZero != 0UL)
        {
            return;
        }

        counterOne = WideMath.AddModulo64(counterOne, 1UL);
        if (counterOne != 0UL)
        {
            return;
        }

        counterTwo = WideMath.AddModulo64(counterTwo, 1UL);
        if (counterTwo != 0UL)
        {
            return;
        }

        counterThree = WideMath.AddModulo64(counterThree, 1UL);
    }

    internal bool CounterIsMaximum()
    {
        return counterZero == ulong.MaxValue && counterOne == ulong.MaxValue && counterTwo == ulong.MaxValue && counterThree == ulong.MaxValue;
    }

    internal void ClearBuffer()
    {
        bufferZero = 0UL;
        bufferOne = 0UL;
        bufferTwo = 0UL;
        bufferThree = 0UL;
    }

    internal static void ValidateRandomState(RandomState randomState)
    {
        Guard.NotNull(randomState, "randomState");
        if (randomState.Algorithm() != RandomAlgorithm.Philox4x64TenV1)
        {
            throw RandomStateException.Create("The state belongs to a different random algorithm.");
        }

        if (randomState.AlgorithmVersion() != AlgorithmVersion)
        {
            throw RandomStateException.Create("The Philox4x64-10 algorithm version is not supported.");
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
            throw RandomStateException.Create("A Philox4x64-10 state payload must contain exactly eighty-eight bytes.");
        }

        uint restoredIndex = BinaryCodec.ReadUInt32(payload, 80);
        if (restoredIndex > BlockWordCount)
        {
            throw RandomStateException.Create("The Philox buffered lane index is invalid.");
        }

        byte exhausted = BinaryCodec.ReadByte(payload, 84);
        if (exhausted > 1)
        {
            throw RandomStateException.Create("The Philox counter exhaustion flag is invalid.");
        }

        if (BinaryCodec.ReadByte(payload, 85) != 0 || BinaryCodec.ReadByte(payload, 86) != 0 || BinaryCodec.ReadByte(payload, 87) != 0)
        {
            throw RandomStateException.Create("The Philox state reserved bytes must be zero.");
        }

        if (restoredIndex == BlockWordCount)
        {
            ulong buffered = BinaryCodec.ReadUInt64(payload, 48);
            buffered |= BinaryCodec.ReadUInt64(payload, 56);
            buffered |= BinaryCodec.ReadUInt64(payload, 64);
            buffered |= BinaryCodec.ReadUInt64(payload, 72);
            if (buffered != 0UL)
            {
                throw RandomStateException.Create("An empty Philox buffer must use canonical zero storage.");
            }
        }

        if (exhausted == 1)
        {
            ulong combined = BinaryCodec.ReadUInt64(payload, 0);
            combined &= BinaryCodec.ReadUInt64(payload, 8);
            combined &= BinaryCodec.ReadUInt64(payload, 16);
            combined &= BinaryCodec.ReadUInt64(payload, 24);
            if (combined != ulong.MaxValue)
            {
                throw RandomStateException.Create("An exhausted Philox state must identify the final counter block.");
            }
        }
    }

    internal static void ValidateCounter(ulong[] counter, string argumentName)
    {
        Guard.NotNull(counter, argumentName);
        if (counter.Length != 4)
        {
            throw RandomArgumentException.Create(argumentName, "A Philox counter must contain exactly four words.");
        }
    }

    internal static void ValidateKey(ulong[] key)
    {
        Guard.NotNull(key, "key");
        if (key.Length != 2)
        {
            throw RandomArgumentException.Create("key", "A Philox key must contain exactly two words.");
        }
    }
}
