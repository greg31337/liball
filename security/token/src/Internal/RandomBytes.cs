using System;
using System.Security.Cryptography;

namespace token;

/// <summary>
/// Provides cryptographically secure random byte generation.
/// </summary>
internal static class RandomBytes
{
    /// <summary>
    /// Generates a new byte array containing cryptographically secure random values.
    /// </summary>
    /// <param name="length">The number of random bytes to generate.</param>
    /// <returns>A new byte array containing the requested number of random bytes.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>length</c> is negative.</exception>
    internal static byte[] Generate(int length)
    {
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException("length");
        }

        return RandomNumberGenerator.GetBytes(length);
    }

    /// <summary>
    /// Fills a byte span with cryptographically secure random values.
    /// </summary>
    /// <param name="destination">The byte span to fill.</param>
    internal static void Fill(Span<byte> destination)
    {
        RandomNumberGenerator.Fill(destination);
    }
}
