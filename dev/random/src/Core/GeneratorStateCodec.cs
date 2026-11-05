namespace random;

internal class GeneratorStateData
{
    IRandomEngine engine;
    ulong booleanReservoir;
    int booleanBitCount;

    internal GeneratorStateData(IRandomEngine engine, ulong booleanReservoir, int booleanBitCount)
    {
        this.engine = engine;
        this.booleanReservoir = booleanReservoir;
        this.booleanBitCount = booleanBitCount;
    }

    internal IRandomEngine Engine()
    {
        return engine;
    }

    internal ulong BooleanReservoir()
    {
        return booleanReservoir;
    }

    internal int BooleanBitCount()
    {
        return booleanBitCount;
    }
}

internal static class GeneratorStateCodec
{
    internal const int FormatVersion = 1;
    internal const int HeaderSize = 28;
    internal const ulong Magic = 0x3130574152444E52UL;

    internal static RandomState Encode(IRandomEngine engine, ulong booleanReservoir, int booleanBitCount)
    {
        Guard.NotNull(engine, "engine");
        ValidateReservoir(booleanReservoir, booleanBitCount, engine.EngineInfo().WordBits());
        RandomState engineState = engine.ExportEngineState();
        if (booleanBitCount == 0)
        {
            return engineState;
        }

        byte[] enginePayload = engineState.Payload();
        byte[] payload = new byte[HeaderSize + enginePayload.Length];
        BinaryCodec.WriteUInt64(payload, 0, Magic);
        BinaryCodec.WriteUInt32(payload, 8, (uint)FormatVersion);
        BinaryCodec.WriteUInt32(payload, 12, (uint)enginePayload.Length);
        BinaryCodec.WriteUInt64(payload, 16, booleanReservoir);
        BinaryCodec.WriteUInt32(payload, 24, (uint)booleanBitCount);
        BinaryCodec.WriteBytes(payload, HeaderSize, enginePayload);
        return StateCodec.Create(
            engineState.Algorithm(),
            engineState.AlgorithmVersion(),
            engineState.TransformVersion(),
            payload);
    }

    internal static GeneratorStateData Decode(RandomState randomState)
    {
        Guard.NotNull(randomState, "randomState");
        byte[] payload = randomState.Payload();
        int enginePayloadLength = EnginePayloadLength(randomState.Algorithm());
        if (payload.Length == enginePayloadLength)
        {
            return new GeneratorStateData(RandomEngineFactory.FromState(randomState), 0UL, 0);
        }

        if (payload.Length != HeaderSize + enginePayloadLength)
        {
            throw RandomStateException.Create("The generator state payload has an invalid length.");
        }

        if (BinaryCodec.ReadUInt64(payload, 0) != Magic)
        {
            throw RandomStateException.Create("The generator state payload magic is invalid.");
        }

        if (BinaryCodec.ReadUInt32(payload, 8) != (uint)FormatVersion)
        {
            throw RandomStateException.Create("The generator state payload version is not supported.");
        }

        if (BinaryCodec.ReadUInt32(payload, 12) != (uint)enginePayloadLength)
        {
            throw RandomStateException.Create("The embedded engine payload length is invalid.");
        }

        ulong reservoir = BinaryCodec.ReadUInt64(payload, 16);
        uint bitCountValue = BinaryCodec.ReadUInt32(payload, 24);
        if (bitCountValue > int.MaxValue)
        {
            throw RandomStateException.Create("The boolean reservoir bit count is invalid.");
        }

        int bitCount = (int)bitCountValue;
        int wordBits = EngineWordBits(randomState.Algorithm());
        ValidateReservoirState(reservoir, bitCount, wordBits);
        byte[] enginePayload = BinaryCodec.ReadBytes(payload, HeaderSize, enginePayloadLength);
        RandomState engineState = new RandomState(
            randomState.Algorithm(),
            randomState.AlgorithmVersion(),
            randomState.TransformVersion(),
            enginePayload);
        IRandomEngine restored = RandomEngineFactory.FromState(engineState);
        return new GeneratorStateData(restored, reservoir, bitCount);
    }

    internal static int EnginePayloadLength(RandomAlgorithm algorithm)
    {
        switch (algorithm)
        {
            case RandomAlgorithm.Pcg64DxsmV1:
                return Pcg64DxsmEngine.StateByteCount;
            case RandomAlgorithm.Xoshiro256StarStarV1:
                return Xoshiro256StarStarEngine.StateByteCount;
            case RandomAlgorithm.Philox4x64TenV1:
                return Philox4x64TenEngine.StateByteCount;
            case RandomAlgorithm.Mt19937V1:
            case RandomAlgorithm.Mt19937NumpyV1:
                return Mt19937Engine.StateByteCount;
            case RandomAlgorithm.SplitMix64V1:
                return SplitMix64Engine.StateByteCount;
            default:
                throw RandomStateException.Create("The generator state algorithm is not supported.");
        }
    }

    internal static int EngineWordBits(RandomAlgorithm algorithm)
    {
        switch (algorithm)
        {
            case RandomAlgorithm.Mt19937V1:
            case RandomAlgorithm.Mt19937NumpyV1:
                return 32;
            case RandomAlgorithm.Pcg64DxsmV1:
            case RandomAlgorithm.Xoshiro256StarStarV1:
            case RandomAlgorithm.Philox4x64TenV1:
            case RandomAlgorithm.SplitMix64V1:
                return 64;
            default:
                throw RandomStateException.Create("The generator state algorithm is not supported.");
        }
    }

    internal static void ValidateReservoir(ulong reservoir, int bitCount, int wordBits)
    {
        if (bitCount < 0 || bitCount > wordBits)
        {
            throw RandomStateException.Create("The boolean reservoir bit count is invalid.");
        }

        if (bitCount == 0)
        {
            if (reservoir != 0UL)
            {
                throw RandomStateException.Create("An empty boolean reservoir must contain zero bits.");
            }

            return;
        }

        if ((reservoir & ~BitMath.LowMask64(bitCount)) != 0UL)
        {
            throw RandomStateException.Create("The boolean reservoir contains bits outside its declared count.");
        }
    }

    internal static void ValidateReservoirState(ulong reservoir, int bitCount, int wordBits)
    {
        if (bitCount <= 0 || bitCount > wordBits)
        {
            throw RandomStateException.Create("A wrapped generator state must contain a nonempty valid boolean reservoir.");
        }

        if ((reservoir & ~BitMath.LowMask64(bitCount)) != 0UL)
        {
            throw RandomStateException.Create("The boolean reservoir contains bits outside its declared count.");
        }
    }
}
