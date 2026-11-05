using System;

namespace email;
/// <summary>
/// Controls how DKIM body canonicalization operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailDkimBodyCanonicalizationOptions : IEmailDkimBodyCanonicalizationOptionsContract
{
    /// <summary>
    /// The default maximum input bytes used when the caller does not provide an override.
    /// </summary>
    internal const long DefaultMaximumInputBytes = EmailCryptographicLimitsConstants.DefaultMaxInputBytes;
    /// <summary>
    /// The default maximum canonicalized bytes used when the caller does not provide an override.
    /// </summary>
    internal const long DefaultMaximumCanonicalizedBytes = EmailCryptographicLimitsConstants.DefaultMaxInputBytes + 2;
    /// <summary>
    /// The default maximum hashed bytes used when the caller does not provide an override.
    /// </summary>
    internal const long DefaultMaximumHashedBytes = DefaultMaximumCanonicalizedBytes;
    EmailDkimCanonicalizationKind kind;
    EmailHashAlgorithm hashAlgorithm;
    long maximumInputBytes;
    long maximumCanonicalizedBytes;
    long maximumHashedBytes;
    bool hasBodyLengthLimit;
    long bodyLengthLimit;
    /// <summary>
    /// Creates a new DKIM body canonicalization options object.
    /// </summary>
    public EmailDkimBodyCanonicalizationOptions()
    {
        kind = EmailDkimCanonicalizationKind.Simple;
        hashAlgorithm = EmailHashAlgorithm.Sha256;
        maximumInputBytes = DefaultMaximumInputBytes;
        maximumCanonicalizedBytes = DefaultMaximumCanonicalizedBytes;
        maximumHashedBytes = DefaultMaximumHashedBytes;
        hasBodyLengthLimit = false;
        bodyLengthLimit = 0;
    }

    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    public EmailDkimCanonicalizationKind GetKind()
    {
        return kind;
    }

    /// <summary>
    /// Sets the kind used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetKind(EmailDkimCanonicalizationKind value)
    {
        if (value != EmailDkimCanonicalizationKind.Simple && value != EmailDkimCanonicalizationKind.Relaxed)
            throw new ArgumentOutOfRangeException("value");
        kind = value;
    }

    /// <summary>
    /// Gets the hash algorithm.
    /// </summary>
    /// <returns>The current hash algorithm.</returns>
    public EmailHashAlgorithm GetHashAlgorithm()
    {
        return hashAlgorithm;
    }

    /// <summary>
    /// Sets the hash algorithm used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetHashAlgorithm(EmailHashAlgorithm value)
    {
        if (value != EmailHashAlgorithm.Sha1 && (value < EmailHashAlgorithm.Sha256 || value > EmailHashAlgorithm.Sha512))
            throw new ArgumentOutOfRangeException("value");
        hashAlgorithm = value;
    }

    /// <summary>
    /// Gets the maximum input bytes.
    /// </summary>
    /// <returns>The current maximum input bytes.</returns>
    public long GetMaximumInputBytes()
    {
        return maximumInputBytes;
    }

    /// <summary>
    /// Sets the maximum input bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumInputBytes(long value)
    {
        maximumInputBytes = EmailLimitValidation.RequireNonNegative(value, "value");
    }

    /// <summary>
    /// Gets the maximum canonicalized bytes.
    /// </summary>
    /// <returns>The current maximum canonicalized bytes.</returns>
    public long GetMaximumCanonicalizedBytes()
    {
        return maximumCanonicalizedBytes;
    }

    /// <summary>
    /// Sets the maximum canonicalized bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumCanonicalizedBytes(long value)
    {
        maximumCanonicalizedBytes = EmailLimitValidation.RequireNonNegative(value, "value");
    }

    /// <summary>
    /// Gets the maximum hashed bytes.
    /// </summary>
    /// <returns>The current maximum hashed bytes.</returns>
    public long GetMaximumHashedBytes()
    {
        return maximumHashedBytes;
    }

    /// <summary>
    /// Sets the maximum hashed bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumHashedBytes(long value)
    {
        maximumHashedBytes = EmailLimitValidation.RequireNonNegative(value, "value");
    }

    /// <summary>
    /// Determines whether the body length limit is present or available.
    /// </summary>
    /// <returns><c>true</c> when the body length limit is available; otherwise, <c>false</c>.</returns>
    public bool HasBodyLengthLimit()
    {
        return hasBodyLengthLimit;
    }

    /// <summary>
    /// Gets the body length limit.
    /// </summary>
    /// <returns>The current body length limit.</returns>
    public long GetBodyLengthLimit()
    {
        return bodyLengthLimit;
    }

    /// <summary>
    /// Sets the body length limit used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetBodyLengthLimit(long value)
    {
        bodyLengthLimit = EmailLimitValidation.RequireNonNegative(value, "value");
        hasBodyLengthLimit = true;
    }

    /// <summary>
    /// Clears the stored body length limit value or values.
    /// </summary>
    public void ClearBodyLengthLimit()
    {
        hasBodyLengthLimit = false;
        bodyLengthLimit = 0;
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailDkimBodyCanonicalizationOptions Clone()
    {
        EmailDkimBodyCanonicalizationOptions copy = new EmailDkimBodyCanonicalizationOptions();
        copy.SetKind(kind);
        copy.SetHashAlgorithm(hashAlgorithm);
        copy.SetMaximumInputBytes(maximumInputBytes);
        copy.SetMaximumCanonicalizedBytes(maximumCanonicalizedBytes);
        copy.SetMaximumHashedBytes(maximumHashedBytes);
        if (hasBodyLengthLimit)
            copy.SetBodyLengthLimit(bodyLengthLimit);
        return copy;
    }
}
