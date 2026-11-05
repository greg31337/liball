using System;
using System.Security.Cryptography;

namespace fake;

internal static class CryptoRandom
{
    internal static void Fill(Span<byte> destination)
    {
        RandomNumberGenerator.Fill(destination);
    }

    internal static ulong Next()
    {
        byte[] bytes = new byte[8];
        RandomNumberGenerator.Fill(bytes);
        ulong value = (ulong)bytes[0];
        value = value | ((ulong)bytes[1] << 8);
        value = value | ((ulong)bytes[2] << 16);
        value = value | ((ulong)bytes[3] << 24);
        value = value | ((ulong)bytes[4] << 32);
        value = value | ((ulong)bytes[5] << 40);
        value = value | ((ulong)bytes[6] << 48);
        value = value | ((ulong)bytes[7] << 56);
        return value;
    }
}
