using System;

namespace email;
/// <summary>
/// Controls how DKIM key parser operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailDkimKeyParserOptions : IEmailDkimKeyParserOptionsContract
{
    /// <summary>
    /// The default maximum tags used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumTags = 64;
    /// <summary>
    /// The default minimum RSA key bits used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMinimumRsaKeyBits = 1024;
    /// <summary>
    /// The default maximum RSA key bits used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumRsaKeyBits = 4096;
    EmailStructuredValueParseMode mode;
    int maximumTags;
    int maximumInputBytes;
    long maximumDecodedKeyBytes;
    int minimumRsaKeyBits;
    int maximumRsaKeyBits;
    /// <summary>
    /// Creates a new DKIM key parser options object.
    /// </summary>
    public EmailDkimKeyParserOptions()
    {
        mode = EmailStructuredValueParseMode.Tolerant;
        maximumTags = DefaultMaximumTags;
        maximumInputBytes = EmailParsingLimitsConstants.DefaultMaxLineBytes;
        maximumDecodedKeyBytes = EmailCryptographicLimitsConstants.DefaultMaxKeyBytes;
        minimumRsaKeyBits = DefaultMinimumRsaKeyBits;
        maximumRsaKeyBits = DefaultMaximumRsaKeyBits;
    }

    /// <summary>
    /// Gets the mode.
    /// </summary>
    /// <returns>The current mode.</returns>
    public EmailStructuredValueParseMode GetMode()
    {
        return mode;
    }

    /// <summary>
    /// Sets the mode used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMode(EmailStructuredValueParseMode value)
    {
        if (value < EmailStructuredValueParseMode.Strict || value > EmailStructuredValueParseMode.Tolerant)
            throw new ArgumentOutOfRangeException("value");
        mode = value;
    }

    /// <summary>
    /// Gets the maximum tags.
    /// </summary>
    /// <returns>The current maximum tags.</returns>
    public int GetMaximumTags()
    {
        return maximumTags;
    }

    /// <summary>
    /// Sets the maximum tags used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumTags(int value)
    {
        maximumTags = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum input bytes.
    /// </summary>
    /// <returns>The current maximum input bytes.</returns>
    public int GetMaximumInputBytes()
    {
        return maximumInputBytes;
    }

    /// <summary>
    /// Sets the maximum input bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumInputBytes(int value)
    {
        maximumInputBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum decoded key bytes.
    /// </summary>
    /// <returns>The current maximum decoded key bytes.</returns>
    public long GetMaximumDecodedKeyBytes()
    {
        return maximumDecodedKeyBytes;
    }

    /// <summary>
    /// Sets the maximum decoded key bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumDecodedKeyBytes(long value)
    {
        maximumDecodedKeyBytes = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt64AndString(value, "value");
    }

    /// <summary>
    /// Gets the minimum RSA key bits.
    /// </summary>
    /// <returns>The current minimum RSA key bits.</returns>
    public int GetMinimumRsaKeyBits()
    {
        return minimumRsaKeyBits;
    }

    /// <summary>
    /// Sets the minimum RSA key bits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMinimumRsaKeyBits(int value)
    {
        if (value < DefaultMinimumRsaKeyBits || value > maximumRsaKeyBits)
            throw new ArgumentOutOfRangeException("value");
        minimumRsaKeyBits = value;
    }

    /// <summary>
    /// Gets the maximum RSA key bits.
    /// </summary>
    /// <returns>The current maximum RSA key bits.</returns>
    public int GetMaximumRsaKeyBits()
    {
        return maximumRsaKeyBits;
    }

    /// <summary>
    /// Sets the maximum RSA key bits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumRsaKeyBits(int value)
    {
        if (value < DefaultMaximumRsaKeyBits || value < minimumRsaKeyBits)
            throw new ArgumentOutOfRangeException("value");
        maximumRsaKeyBits = value;
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailDkimKeyParserOptions Clone()
    {
        EmailDkimKeyParserOptions copy = new EmailDkimKeyParserOptions();
        copy.SetMode(mode);
        copy.SetMaximumTags(maximumTags);
        copy.SetMaximumInputBytes(maximumInputBytes);
        copy.SetMaximumDecodedKeyBytes(maximumDecodedKeyBytes);
        copy.SetMaximumRsaKeyBits(maximumRsaKeyBits);
        copy.SetMinimumRsaKeyBits(minimumRsaKeyBits);
        return copy;
    }
}
