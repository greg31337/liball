using System;

namespace email;
/// <summary>
/// Controls how authentication parser operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailAuthenticationParserOptions : IEmailAuthenticationParserOptionsContract
{
    /// <summary>
    /// The default maximum results used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumResults = 64;
    /// <summary>
    /// The default maximum properties used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumProperties = 128;
    EmailStructuredValueParseMode mode;
    int maximumResults;
    int maximumProperties;
    int maximumInputBytes;
    /// <summary>
    /// Creates a new authentication parser options object.
    /// </summary>
    public EmailAuthenticationParserOptions()
    {
        mode = EmailStructuredValueParseMode.Tolerant;
        maximumResults = DefaultMaximumResults;
        maximumProperties = DefaultMaximumProperties;
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
    /// Gets the maximum results.
    /// </summary>
    /// <returns>The current maximum results.</returns>
    public int GetMaximumResults()
    {
        return maximumResults;
    }

    /// <summary>
    /// Sets the maximum results used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumResults(int value)
    {
        maximumResults = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
    }

    /// <summary>
    /// Gets the maximum properties.
    /// </summary>
    /// <returns>The current maximum properties.</returns>
    public int GetMaximumProperties()
    {
        return maximumProperties;
    }

    /// <summary>
    /// Sets the maximum properties used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumProperties(int value)
    {
        maximumProperties = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
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
    public EmailAuthenticationParserOptions Clone()
    {
        EmailAuthenticationParserOptions copy = new EmailAuthenticationParserOptions();
        copy.SetMode(mode);
        copy.SetMaximumResults(maximumResults);
        copy.SetMaximumProperties(maximumProperties);
        copy.SetMaximumInputBytes(maximumInputBytes);
        return copy;
    }
}
