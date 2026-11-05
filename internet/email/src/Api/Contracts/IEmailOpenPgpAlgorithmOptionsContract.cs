using System;
using System.IO.Compression;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailOpenPgpAlgorithmOptions"/>.
/// </summary>
public interface IEmailOpenPgpAlgorithmOptionsContract
{
    /// <summary>
    /// Gets the limits.
    /// </summary>
    /// <returns>The current limits.</returns>
    EmailCryptographicLimits GetLimits();
    /// <summary>
    /// Sets the limits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetLimits(EmailCryptographicLimits value);
    /// <summary>
    /// Gets the allow deprecated RSA generation.
    /// </summary>
    /// <returns><c>true</c> when the allow deprecated RSA generation setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowDeprecatedRsaGeneration();
    /// <summary>
    /// Sets the allow deprecated RSA generation used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow deprecated RSA generation behavior.</param>
    void SetAllowDeprecatedRsaGeneration(bool value);
    /// <summary>
    /// Gets the allow deprecated string to key.
    /// </summary>
    /// <returns><c>true</c> when the allow deprecated string to key setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowDeprecatedStringToKey();
    /// <summary>
    /// Sets the allow deprecated string to key used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow deprecated string to key behavior.</param>
    void SetAllowDeprecatedStringToKey(bool value);
    /// <summary>
    /// Gets the allow sha 1 verification.
    /// </summary>
    /// <returns><c>true</c> when the allow sha 1 verification setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowSha1Verification();
    /// <summary>
    /// Sets the allow sha 1 verification used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow sha 1 verification behavior.</param>
    void SetAllowSha1Verification(bool value);
    /// <summary>
    /// Gets the maximum string to key bytes.
    /// </summary>
    /// <returns>The current maximum string to key bytes.</returns>
    long GetMaximumStringToKeyBytes();
    /// <summary>
    /// Sets the maximum string to key bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumStringToKeyBytes(long value);
    /// <summary>
    /// Gets the compression level.
    /// </summary>
    /// <returns>The current compression level.</returns>
    CompressionLevel GetCompressionLevel();
    /// <summary>
    /// Sets the compression level used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetCompressionLevel(CompressionLevel value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailOpenPgpAlgorithmOptions Clone();
}
