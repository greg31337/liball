using System;

namespace email;
/// <summary>
/// Controls how date parser operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailDateParserOptions : IEmailDateParserOptionsContract
{
    EmailDateParseMode mode;
    int maximumInputBytes;
    /// <summary>
    /// Creates a new date parser options object.
    /// </summary>
    public EmailDateParserOptions()
    {
        mode = EmailDateParseMode.Tolerant;
        maximumInputBytes = EmailParsingLimitsConstants.DefaultMaxLineBytes;
    }

    /// <summary>
    /// Gets the mode.
    /// </summary>
    /// <returns>The current mode.</returns>
    public EmailDateParseMode GetMode()
    {
        return mode;
    }

    /// <summary>
    /// Sets the mode used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMode(EmailDateParseMode value)
    {
        if (value < EmailDateParseMode.Strict || value > EmailDateParseMode.Tolerant)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        mode = value;
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
    public EmailDateParserOptions Clone()
    {
        EmailDateParserOptions copy = new EmailDateParserOptions();
        copy.SetMode(mode);
        copy.SetMaximumInputBytes(maximumInputBytes);
        return copy;
    }
}
