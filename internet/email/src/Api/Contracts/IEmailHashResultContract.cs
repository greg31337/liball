using System;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailHashResult"/>.
/// </summary>
public interface IEmailHashResultContract
{
    /// <summary>
    /// Gets the algorithm.
    /// </summary>
    /// <returns>The current algorithm.</returns>
    EmailHashAlgorithm GetAlgorithm();
    /// <summary>
    /// Gets the digest byte count.
    /// </summary>
    /// <returns>The current digest byte count.</returns>
    int GetDigestByteCount();
    /// <summary>
    /// Gets the digest bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current digest bytes.</returns>
    byte[] GetDigestBytes();
    /// <summary>
    /// Gets the hashed byte count.
    /// </summary>
    /// <returns>The current hashed byte count.</returns>
    long GetHashedByteCount();
    /// <summary>
    /// Gets the lowercase hex.
    /// </summary>
    /// <returns>The current lowercase hex.</returns>
    string GetLowercaseHex();
    /// <summary>
    /// Matches the es.
    /// </summary>
    /// <param name = "candidate">The candidate used by the operation.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool Matches(ReadOnlySpan<byte> candidate);
}
