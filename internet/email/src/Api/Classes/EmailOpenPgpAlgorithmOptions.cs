using System;
using System.IO.Compression;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Controls how openPGP algorithm operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailOpenPgpAlgorithmOptions : IEmailOpenPgpAlgorithmOptionsContract
{
    EmailCryptographicLimits limits;
    bool allowDeprecatedRsaGeneration;
    bool allowDeprecatedStringToKey;
    bool allowSha1Verification;
    long maximumStringToKeyBytes;
    CompressionLevel compressionLevel;
    /// <summary>
    /// Creates a new openPGP algorithm options object.
    /// </summary>
    public EmailOpenPgpAlgorithmOptions()
    {
        limits = new EmailCryptographicLimits();
        allowDeprecatedRsaGeneration = false;
        allowDeprecatedStringToKey = false;
        allowSha1Verification = false;
        maximumStringToKeyBytes = 16777216;
        compressionLevel = CompressionLevel.Optimal;
    }

    /// <summary>
    /// Gets the limits.
    /// </summary>
    /// <returns>The current limits.</returns>
    public EmailCryptographicLimits GetLimits()
    {
        return limits.Clone();
    }

    /// <summary>
    /// Sets the limits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetLimits(EmailCryptographicLimits value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        limits = value.Clone();
    }

    /// <summary>
    /// Gets the allow deprecated RSA generation.
    /// </summary>
    /// <returns><c>true</c> when the allow deprecated RSA generation setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowDeprecatedRsaGeneration()
    {
        return allowDeprecatedRsaGeneration;
    }

    /// <summary>
    /// Sets the allow deprecated RSA generation used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow deprecated RSA generation behavior.</param>
    public void SetAllowDeprecatedRsaGeneration(bool value)
    {
        allowDeprecatedRsaGeneration = value;
    }

    /// <summary>
    /// Gets the allow deprecated string to key.
    /// </summary>
    /// <returns><c>true</c> when the allow deprecated string to key setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowDeprecatedStringToKey()
    {
        return allowDeprecatedStringToKey;
    }

    /// <summary>
    /// Sets the allow deprecated string to key used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow deprecated string to key behavior.</param>
    public void SetAllowDeprecatedStringToKey(bool value)
    {
        allowDeprecatedStringToKey = value;
    }

    /// <summary>
    /// Gets the allow sha 1 verification.
    /// </summary>
    /// <returns><c>true</c> when the allow sha 1 verification setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetAllowSha1Verification()
    {
        return allowSha1Verification;
    }

    /// <summary>
    /// Sets the allow sha 1 verification used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow sha 1 verification behavior.</param>
    public void SetAllowSha1Verification(bool value)
    {
        allowSha1Verification = value;
    }

    /// <summary>
    /// Gets the maximum string to key bytes.
    /// </summary>
    /// <returns>The current maximum string to key bytes.</returns>
    public long GetMaximumStringToKeyBytes()
    {
        return maximumStringToKeyBytes;
    }

    /// <summary>
    /// Sets the maximum string to key bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumStringToKeyBytes(long value)
    {
        maximumStringToKeyBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt64AndString(value, "value");
    }

    /// <summary>
    /// Gets the compression level.
    /// </summary>
    /// <returns>The current compression level.</returns>
    public CompressionLevel GetCompressionLevel()
    {
        return compressionLevel;
    }

    /// <summary>
    /// Sets the compression level used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetCompressionLevel(CompressionLevel value)
    {
        if (value < CompressionLevel.Optimal || value > CompressionLevel.SmallestSize)
            throw new ArgumentOutOfRangeException("value");
        compressionLevel = value;
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailOpenPgpAlgorithmOptions Clone()
    {
        EmailOpenPgpAlgorithmOptions copy = new EmailOpenPgpAlgorithmOptions();
        copy.SetLimits(limits);
        copy.SetAllowDeprecatedRsaGeneration(allowDeprecatedRsaGeneration);
        copy.SetAllowDeprecatedStringToKey(allowDeprecatedStringToKey);
        copy.SetAllowSha1Verification(allowSha1Verification);
        copy.SetMaximumStringToKeyBytes(maximumStringToKeyBytes);
        copy.SetCompressionLevel(compressionLevel);
        return copy;
    }
}
