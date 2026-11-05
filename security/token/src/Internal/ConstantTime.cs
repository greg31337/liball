using System;
using System.Security.Cryptography;

namespace token;

/// <summary>
/// Provides constant-time comparison helpers for byte sequences.
/// </summary>
internal static class ConstantTime
{
    /// <summary>
    /// Compares two byte arrays without short-circuiting based on their contents.
    /// </summary>
    /// <param name="left">The first byte array.</param>
    /// <param name="right">The second byte array.</param>
    /// <returns><c>true</c> when both arrays contain the same bytes; otherwise, <c>false</c>.</returns>
    internal static bool AreEqual(byte[] left, byte[] right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(left, right);
    }

    /// <summary>
    /// Compares two read-only byte spans without short-circuiting based on their contents.
    /// </summary>
    /// <param name="left">The first byte span.</param>
    /// <param name="right">The second byte span.</param>
    /// <returns><c>true</c> when both spans contain the same bytes; otherwise, <c>false</c>.</returns>
    internal static bool AreEqual(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        return CryptographicOperations.FixedTimeEquals(left, right);
    }
}
