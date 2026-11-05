using System;
using System.Security.Cryptography;

namespace dns;

internal static class TransportEntropy
{
    internal static ushort NextIdentifier()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(2);
        return (ushort)((bytes[0] << 8) | bytes[1]);
    }

    internal static int NextInt32(int maximumExclusive)
    {
        if (maximumExclusive <= 0)
        {
            throw new ArgumentOutOfRangeException("maximumExclusive");
        }

        return RandomNumberGenerator.GetInt32(maximumExclusive);
    }

    internal static byte[] CreateBytes(int length)
    {
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException("length");
        }

        return RandomNumberGenerator.GetBytes(length);
    }
}
