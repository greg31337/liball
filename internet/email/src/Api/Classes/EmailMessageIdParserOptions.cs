using System;

namespace email;
/// <summary>
/// Controls how message ID parser operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailMessageIdParserOptions : IEmailMessageIdParserOptionsContract
{
    EmailMessageIdParseMode mode;
    int maximumIdentifiers;
    int maximumInputBytes;
    /// <summary>
    /// Creates a new message ID parser options object.
    /// </summary>
    public EmailMessageIdParserOptions()
    {
        mode = EmailMessageIdParseMode.Tolerant;
        maximumIdentifiers = EmailParsingLimitsConstants.DefaultMaxAddressesPerHeader;
        maximumInputBytes = EmailParsingLimitsConstants.DefaultMaxLineBytes;
    }

    /// <summary>
    /// Gets the mode.
    /// </summary>
    /// <returns>The current mode.</returns>
    public EmailMessageIdParseMode GetMode()
    {
        return mode;
    }

    /// <summary>
    /// Sets the mode used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMode(EmailMessageIdParseMode value)
    {
        if (value < EmailMessageIdParseMode.Strict || value > EmailMessageIdParseMode.Tolerant)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        mode = value;
    }

    /// <summary>
    /// Gets the maximum identifiers.
    /// </summary>
    /// <returns>The current maximum identifiers.</returns>
    public int GetMaximumIdentifiers()
    {
        return maximumIdentifiers;
    }

    /// <summary>
    /// Sets the maximum identifiers used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumIdentifiers(int value)
    {
        maximumIdentifiers = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
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
    public EmailMessageIdParserOptions Clone()
    {
        EmailMessageIdParserOptions copy = new EmailMessageIdParserOptions();
        copy.SetMode(mode);
        copy.SetMaximumIdentifiers(maximumIdentifiers);
        copy.SetMaximumInputBytes(maximumInputBytes);
        return copy;
    }
}
