using System;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Describes the outcome of a hash operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailHashResult : IEmailHashResultContract
{
    EmailHashAlgorithm algorithm;
    byte[] digest;
    long hashedByteCount;
    /// <summary>
    /// Creates a new hash result.
    /// </summary>
    /// <param name = "algorithm">The algorithm used by the operation.</param>
    /// <param name = "digest">The digest used by the operation.</param>
    /// <param name = "hashedByteCount">The number of items or bytes to process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailHashResult(EmailHashAlgorithm algorithm, byte[] digest, long hashedByteCount)
    {
        int expectedLength = GetExpectedLength(algorithm);
        if (digest == null)
        {
            throw new ArgumentNullException("digest");
        }

        if (digest.Length != expectedLength)
        {
            throw new ArgumentException("The digest length does not match the hash algorithm.", "digest");
        }

        if (hashedByteCount < 0)
        {
            throw new ArgumentOutOfRangeException("hashedByteCount");
        }

        this.algorithm = algorithm;
        this.digest = new byte[digest.Length];
        digest.CopyTo(this.digest, 0);
        this.hashedByteCount = hashedByteCount;
    }

    /// <summary>
    /// Gets the algorithm.
    /// </summary>
    /// <returns>The current algorithm.</returns>
    public EmailHashAlgorithm GetAlgorithm()
    {
        return algorithm;
    }

    /// <summary>
    /// Gets the digest byte count.
    /// </summary>
    /// <returns>The current digest byte count.</returns>
    public int GetDigestByteCount()
    {
        return digest.Length;
    }

    /// <summary>
    /// Gets the digest bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current digest bytes.</returns>
    public byte[] GetDigestBytes()
    {
        byte[] copy = new byte[digest.Length];
        digest.CopyTo(copy, 0);
        return copy;
    }

    /// <summary>
    /// Gets the hashed byte count.
    /// </summary>
    /// <returns>The current hashed byte count.</returns>
    public long GetHashedByteCount()
    {
        return hashedByteCount;
    }

    /// <summary>
    /// Gets the lowercase hex.
    /// </summary>
    /// <returns>The current lowercase hex.</returns>
    public string GetLowercaseHex()
    {
        char[] characters = new char[digest.Length * 2];
        int index = 0;
        while (index < digest.Length)
        {
            characters[index * 2] = (char)new EmailByteScanner().ToLowerHex(digest[index] >> 4);
            characters[index * 2 + 1] = (char)new EmailByteScanner().ToLowerHex(digest[index] & 15);
            index++;
        }

        return new string (characters);
    }

    /// <summary>
    /// Matches the es.
    /// </summary>
    /// <param name = "candidate">The candidate used by the operation.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool Matches(ReadOnlySpan<byte> candidate)
    {
        if (candidate.Length != digest.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(digest, candidate);
    }

    static int GetExpectedLength(EmailHashAlgorithm algorithm)
    {
        switch (algorithm)
        {
            case EmailHashAlgorithm.Sha1:
                return 20;
            case EmailHashAlgorithm.Sha256:
                return 32;
            case EmailHashAlgorithm.Sha384:
                return 48;
            case EmailHashAlgorithm.Sha512:
                return 64;
            default:
                throw new ArgumentOutOfRangeException("algorithm");
        }
    }
}
