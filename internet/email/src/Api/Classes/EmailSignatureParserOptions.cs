using System;

namespace email;
/// <summary>
/// Controls how signature parser operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailSignatureParserOptions : IEmailSignatureParserOptionsContract
{
    /// <summary>
    /// The default maximum tags used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumTags = 64;
    EmailStructuredValueParseMode mode;
    int maximumTags;
    int maximumInputBytes;
    /// <summary>
    /// Creates a new signature parser options object.
    /// </summary>
    public EmailSignatureParserOptions()
    {
        mode = EmailStructuredValueParseMode.Tolerant;
        maximumTags = DefaultMaximumTags;
        maximumInputBytes = EmailParsingLimitsConstants.DefaultMaxLineBytes;
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
        {
            throw new ArgumentOutOfRangeException("value");
        }

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
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailSignatureParserOptions Clone()
    {
        EmailSignatureParserOptions copy = new EmailSignatureParserOptions();
        copy.SetMode(mode);
        copy.SetMaximumTags(maximumTags);
        copy.SetMaximumInputBytes(maximumInputBytes);
        return copy;
    }
}
