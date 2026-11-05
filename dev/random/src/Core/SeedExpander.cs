using System;
using System.Security.Cryptography;
using System.Text;

namespace random;

internal class SeedExpander
{
    internal const int ExpansionVersion = 1;
    internal const int HeaderSize = 32;
    internal const int CounterSize = 4;
    internal const int BlockSize = 32;
    internal const ulong LibraryDomain = 0x3130505845444E52UL;

    internal static byte[] Expand(byte[] seedMaterial, RandomAlgorithm algorithm, int algorithmVersion, string purpose, int outputLength)
    {
        Guard.Argument(StateCodec.IsDeterministicAlgorithm(algorithm), "algorithm", "Seed expansion requires a named deterministic algorithm.");
        return ExpandCore(seedMaterial, algorithm, algorithmVersion, purpose, outputLength);
    }

    internal static byte[] ExpandSeedSequence(byte[] seedMaterial, string purpose, int outputLength)
    {
        return ExpandCore(seedMaterial, RandomAlgorithm.None, 1, purpose, outputLength);
    }

    internal static byte[] ExpandCore(byte[] seedMaterial, RandomAlgorithm algorithm, int algorithmVersion, string purpose, int outputLength)
    {
        Guard.NotNull(seedMaterial, "seedMaterial");
        Guard.Positive(algorithmVersion, "algorithmVersion");
        Guard.NotEmpty(purpose, "purpose");
        Guard.Count(outputLength, "outputLength");

        int purposeByteCount = SeedCodec.Utf8ByteCount(purpose, "purpose");
        byte[] purposeBytes = Encoding.UTF8.GetBytes(purpose);
        if (purposeBytes.Length != purposeByteCount)
        {
            throw RandomArgumentException.Create("purpose", "The purpose does not have a canonical UTF-8 representation.");
        }

        if (purposeBytes.Length > int.MaxValue - HeaderSize - CounterSize)
        {
            throw RandomArgumentException.Create("purpose", "The purpose is too large to encode.");
        }

        int remainingLength = int.MaxValue - HeaderSize - CounterSize - purposeBytes.Length;
        if (seedMaterial.Length > remainingLength)
        {
            throw RandomArgumentException.Create("seedMaterial", "The seed material is too large to expand.");
        }

        int counterOffset = HeaderSize + purposeBytes.Length + seedMaterial.Length;
        byte[] input = new byte[counterOffset + CounterSize];
        int offset = 0;
        offset = BinaryCodec.WriteUInt64(input, offset, LibraryDomain);
        offset = BinaryCodec.WriteUInt32(input, offset, (uint)ExpansionVersion);
        offset = BinaryCodec.WriteUInt32(input, offset, (uint)algorithm);
        offset = BinaryCodec.WriteUInt32(input, offset, (uint)algorithmVersion);
        offset = BinaryCodec.WriteUInt32(input, offset, (uint)purposeBytes.Length);
        offset = BinaryCodec.WriteUInt32(input, offset, (uint)seedMaterial.Length);
        offset = BinaryCodec.WriteUInt32(input, offset, (uint)outputLength);
        offset = BinaryCodec.WriteBytes(input, offset, purposeBytes);
        BinaryCodec.WriteBytes(input, offset, seedMaterial);

        byte[] output = new byte[outputLength];
        int outputOffset = 0;
        uint counter = 1U;
        while (outputOffset < output.Length)
        {
            BinaryCodec.WriteUInt32(input, counterOffset, counter);
            byte[] digest = SHA256.HashData(input);
            int remaining = output.Length - outputOffset;
            int take = remaining < BlockSize ? remaining : BlockSize;
            digest.AsSpan(0, take).CopyTo(output.AsSpan(outputOffset, take));
            outputOffset += take;
            counter++;
        }

        return output;
    }
}
