using System;

namespace email;
/// <summary>
/// Controls how received parser operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailReceivedParserOptions : IEmailReceivedParserOptionsContract
{
    /// <summary>
    /// The default maximum clauses used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumClauses = 64;
    EmailReceivedParseMode mode;
    int maximumClauses;
    int maximumInputBytes;
    /// <summary>
    /// Creates a new received parser options object.
    /// </summary>
    public EmailReceivedParserOptions()
    {
        mode = EmailReceivedParseMode.Tolerant;
        maximumClauses = DefaultMaximumClauses;
        maximumInputBytes = EmailParsingLimitsConstants.DefaultMaxLineBytes;
    }

    /// <summary>
    /// Gets the mode.
    /// </summary>
    /// <returns>The current mode.</returns>
    public EmailReceivedParseMode GetMode()
    {
        return mode;
    }

    /// <summary>
    /// Sets the mode used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMode(EmailReceivedParseMode value)
    {
        if (value < EmailReceivedParseMode.Strict || value > EmailReceivedParseMode.Tolerant)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        mode = value;
    }

    /// <summary>
    /// Gets the maximum clauses.
    /// </summary>
    /// <returns>The current maximum clauses.</returns>
    public int GetMaximumClauses()
    {
        return maximumClauses;
    }

    /// <summary>
    /// Sets the maximum clauses used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumClauses(int value)
    {
        maximumClauses = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
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
    public EmailReceivedParserOptions Clone()
    {
        EmailReceivedParserOptions copy = new EmailReceivedParserOptions();
        copy.SetMode(mode);
        copy.SetMaximumClauses(maximumClauses);
        copy.SetMaximumInputBytes(maximumInputBytes);
        return copy;
    }
}
