using System;

namespace email;
/// <summary>
/// Controls how metadata parser operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailMetadataParserOptions : IEmailMetadataParserOptionsContract
{
    /// <summary>
    /// The default maximum items used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumItems = 128;
    EmailStructuredValueParseMode mode;
    int maximumItems;
    int maximumInputBytes;
    /// <summary>
    /// Creates a new metadata parser options object.
    /// </summary>
    public EmailMetadataParserOptions()
    {
        mode = EmailStructuredValueParseMode.Tolerant;
        maximumItems = DefaultMaximumItems;
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
    /// Gets the maximum items.
    /// </summary>
    /// <returns>The current maximum items.</returns>
    public int GetMaximumItems()
    {
        return maximumItems;
    }

    /// <summary>
    /// Sets the maximum items used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumItems(int value)
    {
        maximumItems = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
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
    public EmailMetadataParserOptions Clone()
    {
        EmailMetadataParserOptions copy = new EmailMetadataParserOptions();
        copy.SetMode(mode);
        copy.SetMaximumItems(maximumItems);
        copy.SetMaximumInputBytes(maximumInputBytes);
        return copy;
    }
}
