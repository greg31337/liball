using System;

using System.Security.Cryptography;

namespace random;

internal class SystemCryptoEngine
{
    internal const int AlgorithmVersion = 1;
    internal const int MaximumChunkBytes = 65536;

    internal SystemCryptoEngine()
    {
    }

    internal void FillBytes(Span<byte> destination)
    {
        int offset = 0;
        while (offset < destination.Length)
        {
            int count = ChunkLength(destination.Length - offset);
            RandomNumberGenerator.Fill(destination.Slice(offset, count));
            offset += count;
        }
    }

    internal RandomAlgorithmInfo Info()
    {
        return new RandomAlgorithmInfo(
            RandomAlgorithm.SystemCryptoV1,
            "System cryptographic random source",
            AlgorithmVersion,
            StateCodec.CurrentTransformVersion,
            8,
            0,
            true,
            "No library-defined period; operating-system source failures propagate",
            new RandomCapability[0]);
    }

    internal static int ChunkLength(int remainingBytes)
    {
        Guard.NonNegative(remainingBytes, "remainingBytes");
        if (remainingBytes > MaximumChunkBytes)
        {
            return MaximumChunkBytes;
        }

        return remainingBytes;
    }
}
