using System;

namespace random;

internal static class EngineSpawner
{
    internal const string Purpose = "indexed-child-engine-v1";

    internal static IRandomEngine Create(RandomAlgorithm algorithm, byte[] rootMaterial, ulong[] spawnPath)
    {
        Guard.NotNull(rootMaterial, "rootMaterial");
        Guard.NotNull(spawnPath, "spawnPath");
        SeedSequence identity = new SeedSequence(rootMaterial, spawnPath);
        byte[] material = identity.Material();
        switch (algorithm)
        {
            case RandomAlgorithm.Pcg64DxsmV1:
                return CreatePcg64Dxsm(material);
            case RandomAlgorithm.Xoshiro256StarStarV1:
                return CreateXoshiro256StarStar(material);
            case RandomAlgorithm.Philox4x64TenV1:
                return CreatePhilox4x64Ten(material);
            case RandomAlgorithm.Mt19937V1:
                return CreateMt19937(material);
            case RandomAlgorithm.Mt19937NumpyV1:
                return CreateMt19937Numpy(material);
            case RandomAlgorithm.SplitMix64V1:
                return CreateSplitMix64(material);
            default:
                throw RandomCapabilityException.Create(algorithm, RandomCapability.Spawn, "The selected algorithm does not support indexed spawning.");
        }
    }

    internal static IRandomEngine CreatePcg64Dxsm(byte[] material)
    {
        byte[] expanded = Expand(material, RandomAlgorithm.Pcg64DxsmV1, 32);
        UInt128 initialState = BinaryCodec.ReadUInt128(expanded, 0);
        UInt128 streamSelector = BinaryCodec.ReadUInt128(expanded, 16);
        streamSelector &= ((UInt128)1 << 127) - (UInt128)1;
        return Pcg64DxsmEngine.FromSeed(initialState, streamSelector);
    }

    internal static IRandomEngine CreateXoshiro256StarStar(byte[] material)
    {
        byte[] expanded = Expand(material, RandomAlgorithm.Xoshiro256StarStarV1, 32);
        ulong[] stateWords =
        [
            BinaryCodec.ReadUInt64(expanded, 0),
            BinaryCodec.ReadUInt64(expanded, 8),
            BinaryCodec.ReadUInt64(expanded, 16),
            BinaryCodec.ReadUInt64(expanded, 24)
        ];
        EnsureNonzero(stateWords);
        return Xoshiro256StarStarEngine.FromStateWords(stateWords);
    }

    internal static IRandomEngine CreatePhilox4x64Ten(byte[] material)
    {
        byte[] expanded = Expand(material, RandomAlgorithm.Philox4x64TenV1, 32);
        ulong[] key = [BinaryCodec.ReadUInt64(expanded, 0), BinaryCodec.ReadUInt64(expanded, 8)];
        ulong[] counter = [0UL, 0UL, BinaryCodec.ReadUInt64(expanded, 16), BinaryCodec.ReadUInt64(expanded, 24)];
        return Philox4x64TenEngine.FromKeyAndCounter(key, counter);
    }

    internal static IRandomEngine CreateMt19937(byte[] material)
    {
        byte[] expanded = Expand(material, RandomAlgorithm.Mt19937V1, Mt19937Engine.StateWordCount * 4);
        uint[] stateWords = ReadUInt32Words(expanded);
        EnsureNonzero(stateWords);
        return Mt19937Engine.FromStateWords(stateWords, Mt19937Engine.StateWordCount);
    }

    internal static IRandomEngine CreateMt19937Numpy(byte[] material)
    {
        byte[] expanded = Expand(material, RandomAlgorithm.Mt19937NumpyV1, Mt19937Engine.StateWordCount * 4);
        uint[] stateWords = ReadUInt32Words(expanded);
        stateWords[0] = Mt19937Engine.UpperMask;
        return Mt19937NumpyEngine.FromNumpyState(stateWords, Mt19937Engine.StateWordCount - 1);
    }

    internal static IRandomEngine CreateSplitMix64(byte[] material)
    {
        byte[] expanded = Expand(material, RandomAlgorithm.SplitMix64V1, 8);
        return SplitMix64Engine.FromSeed(BinaryCodec.ReadUInt64(expanded, 0));
    }

    internal static byte[] Expand(byte[] material, RandomAlgorithm algorithm, int length)
    {
        return SeedExpander.Expand(material, algorithm, AlgorithmVersion(algorithm), Purpose, length);
    }

    internal static int AlgorithmVersion(RandomAlgorithm algorithm)
    {
        switch (algorithm)
        {
            case RandomAlgorithm.Pcg64DxsmV1:
                return Pcg64DxsmEngine.AlgorithmVersion;
            case RandomAlgorithm.Xoshiro256StarStarV1:
                return Xoshiro256StarStarEngine.AlgorithmVersion;
            case RandomAlgorithm.Philox4x64TenV1:
                return Philox4x64TenEngine.AlgorithmVersion;
            case RandomAlgorithm.Mt19937V1:
                return Mt19937Engine.AlgorithmVersion;
            case RandomAlgorithm.Mt19937NumpyV1:
                return Mt19937NumpyEngine.AlgorithmVersion;
            case RandomAlgorithm.SplitMix64V1:
                return SplitMix64Engine.AlgorithmVersion;
            default:
                throw RandomArgumentException.Create("algorithm", "The deterministic algorithm version is not known.");
        }
    }

    internal static uint[] ReadUInt32Words(byte[] expanded)
    {
        uint[] words = new uint[expanded.Length / 4];
        int index = 0;
        while (index < words.Length)
        {
            words[index] = BinaryCodec.ReadUInt32(expanded, index * 4);
            index++;
        }

        return words;
    }

    internal static void EnsureNonzero(ulong[] words)
    {
        int index = 0;
        while (index < words.Length)
        {
            if (words[index] != 0UL)
            {
                return;
            }

            index++;
        }

        words[0] = 1UL;
    }

    internal static void EnsureNonzero(uint[] words)
    {
        int index = 0;
        while (index < words.Length)
        {
            if (words[index] != 0U)
            {
                return;
            }

            index++;
        }

        words[0] = 1U;
    }
}
