using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Controls how DKIM header canonicalization operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailDkimHeaderCanonicalizationOptions : IEmailDkimHeaderCanonicalizationOptionsContract
{
    /// <summary>
    /// The default maximum selected header count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumSelectedHeaderCount = EmailParsingLimitsConstants.DefaultMaxHeaderFieldCount;
    /// <summary>
    /// The default maximum input bytes used when the caller does not provide an override.
    /// </summary>
    internal const long DefaultMaximumInputBytes = EmailParsingLimitsConstants.DefaultMaxHeaderBytes;
    /// <summary>
    /// The default maximum canonicalized bytes used when the caller does not provide an override.
    /// </summary>
    internal const long DefaultMaximumCanonicalizedBytes = EmailParsingLimitsConstants.DefaultMaxHeaderBytes;
    EmailDkimCanonicalizationKind kind;
    int maximumSelectedHeaderCount;
    long maximumInputBytes;
    long maximumCanonicalizedBytes;
    /// <summary>
    /// Creates a new DKIM header canonicalization options object.
    /// </summary>
    public EmailDkimHeaderCanonicalizationOptions()
    {
        kind = EmailDkimCanonicalizationKind.Simple;
        maximumSelectedHeaderCount = DefaultMaximumSelectedHeaderCount;
        maximumInputBytes = DefaultMaximumInputBytes;
        maximumCanonicalizedBytes = DefaultMaximumCanonicalizedBytes;
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
    /// Gets the maximum selected header count.
    /// </summary>
    /// <returns>The current maximum selected header count.</returns>
    public int GetMaximumSelectedHeaderCount()
    {
        return maximumSelectedHeaderCount;
    }

    /// <summary>
    /// Sets the maximum selected header count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumSelectedHeaderCount(int value)
    {
        maximumSelectedHeaderCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
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
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailDkimHeaderCanonicalizationOptions Clone()
    {
        EmailDkimHeaderCanonicalizationOptions copy = new EmailDkimHeaderCanonicalizationOptions();
        copy.SetKind(kind);
        copy.SetMaximumSelectedHeaderCount(maximumSelectedHeaderCount);
        copy.SetMaximumInputBytes(maximumInputBytes);
        copy.SetMaximumCanonicalizedBytes(maximumCanonicalizedBytes);
        return copy;
    }
}
