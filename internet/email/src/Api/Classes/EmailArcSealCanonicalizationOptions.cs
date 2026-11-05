using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Controls how ARC seal canonicalization operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailArcSealCanonicalizationOptions : IEmailArcSealCanonicalizationOptionsContract
{
    /// <summary>
    /// The default maximum header count used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumHeaderCount = 150;
    /// <summary>
    /// The default maximum input bytes used when the caller does not provide an override.
    /// </summary>
    internal const long DefaultMaximumInputBytes = EmailParsingLimitsConstants.DefaultMaxHeaderBytes;
    /// <summary>
    /// The default maximum canonicalized bytes used when the caller does not provide an override.
    /// </summary>
    internal const long DefaultMaximumCanonicalizedBytes = EmailParsingLimitsConstants.DefaultMaxHeaderBytes;
    int maximumHeaderCount;
    long maximumInputBytes;
    long maximumCanonicalizedBytes;
    /// <summary>
    /// Creates a new ARC seal canonicalization options object.
    /// </summary>
    public EmailArcSealCanonicalizationOptions()
    {
        maximumHeaderCount = DefaultMaximumHeaderCount;
        maximumInputBytes = DefaultMaximumInputBytes;
        maximumCanonicalizedBytes = DefaultMaximumCanonicalizedBytes;
    }

    /// <summary>
    /// Gets the maximum header count.
    /// </summary>
    /// <returns>The current maximum header count.</returns>
    public int GetMaximumHeaderCount()
    {
        return maximumHeaderCount;
    }

    /// <summary>
    /// Sets the maximum header count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumHeaderCount(int value)
    {
        maximumHeaderCount = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
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
    public EmailArcSealCanonicalizationOptions Clone()
    {
        EmailArcSealCanonicalizationOptions copy = new EmailArcSealCanonicalizationOptions();
        copy.SetMaximumHeaderCount(maximumHeaderCount);
        copy.SetMaximumInputBytes(maximumInputBytes);
        copy.SetMaximumCanonicalizedBytes(maximumCanonicalizedBytes);
        return copy;
    }
}
