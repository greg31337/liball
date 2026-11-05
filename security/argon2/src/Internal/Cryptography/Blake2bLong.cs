using System;
using System.Security.Cryptography;

namespace argon2;

internal static class Blake2bLong
{
    internal static void ComputeHash(ReadOnlySpan<byte> input, Span<byte> destination)
    {
        if (destination.Length < Argon2Limits.MinimumTagLength)
        {
            throw new Argon2ParameterException("Argon2 H-prime output length must be at least 4 bytes.");
        }

        byte[] lengthPrefix = new byte[4];
        uint outputLength = (uint)destination.Length;
        lengthPrefix[0] = (byte)(outputLength & 0xFFU);
        lengthPrefix[1] = (byte)((outputLength >> 8) & 0xFFU);
        lengthPrefix[2] = (byte)((outputLength >> 16) & 0xFFU);
        lengthPrefix[3] = (byte)((outputLength >> 24) & 0xFFU);

        if (destination.Length <= Argon2Limits.Blake2bMaximumDigestLength)
        {
            Blake2b shortHash = new Blake2b(destination.Length, ReadOnlySpan<byte>.Empty);
            shortHash.Update(lengthPrefix);
            shortHash.Update(input);
            shortHash.Finish(destination);
            CryptographicOperations.ZeroMemory(lengthPrefix);
            return;
        }

        byte[] current = new byte[Argon2Limits.Blake2bMaximumDigestLength];
        byte[] next = new byte[Argon2Limits.Blake2bMaximumDigestLength];
        Blake2b initialHash = new Blake2b(
            Argon2Limits.Blake2bMaximumDigestLength,
            ReadOnlySpan<byte>.Empty);
        initialHash.Update(lengthPrefix);
        initialHash.Update(input);
        initialHash.Finish(current);

        int outputOffset = 0;
        int remaining = destination.Length;
        while (remaining > Argon2Limits.Blake2bMaximumDigestLength)
        {
            new ReadOnlySpan<byte>(current, 0, 32).CopyTo(destination.Slice(outputOffset, 32));
            outputOffset = outputOffset + 32;
            remaining = remaining - 32;

            if (remaining > Argon2Limits.Blake2bMaximumDigestLength)
            {
                Blake2b.ComputeHash(current, next);
                byte[] previous = current;
                current = next;
                next = previous;
                CryptographicOperations.ZeroMemory(next);
            }
        }

        Blake2b finalHash = new Blake2b(remaining, ReadOnlySpan<byte>.Empty);
        finalHash.Update(current);
        finalHash.Finish(destination.Slice(outputOffset, remaining));

        CryptographicOperations.ZeroMemory(lengthPrefix);
        CryptographicOperations.ZeroMemory(current);
        CryptographicOperations.ZeroMemory(next);
    }
}
