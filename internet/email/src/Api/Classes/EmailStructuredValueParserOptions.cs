using System;

namespace email;
/// <summary>
/// Controls how structured value parser operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailStructuredValueParserOptions : IEmailStructuredValueParserOptionsContract
{
    EmailStructuredValueParseMode mode;
    int maximumParameters;
    int maximumInputBytes;
    long maximumDecodedParameterBytes;
    /// <summary>
    /// Creates a new structured value parser options object.
    /// </summary>
    public EmailStructuredValueParserOptions()
    {
        mode = EmailStructuredValueParseMode.Tolerant;
        maximumParameters = EmailParsingLimitsConstants.DefaultMaxParametersPerHeader;
        maximumInputBytes = EmailParsingLimitsConstants.DefaultMaxLineBytes;
        maximumDecodedParameterBytes = EmailRfc2231Constants.DefaultMaximumDecodedBytes;
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
    /// Gets the maximum parameters.
    /// </summary>
    /// <returns>The current maximum parameters.</returns>
    public int GetMaximumParameters()
    {
        return maximumParameters;
    }

    /// <summary>
    /// Sets the maximum parameters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumParameters(int value)
    {
        maximumParameters = EmailLimitValidation.RequirePositiveWithValueAndParameterNameUsingInt32AndString(value, "value");
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
    /// Gets the maximum decoded parameter bytes.
    /// </summary>
    /// <returns>The current maximum decoded parameter bytes.</returns>
    public long GetMaximumDecodedParameterBytes()
    {
        return maximumDecodedParameterBytes;
    }

    /// <summary>
    /// Sets the maximum decoded parameter bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetMaximumDecodedParameterBytes(long value)
    {
        maximumDecodedParameterBytes = EmailLimitValidation.RequireNonNegative(value, "value");
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailStructuredValueParserOptions Clone()
    {
        EmailStructuredValueParserOptions copy = new EmailStructuredValueParserOptions();
        copy.SetMode(mode);
        copy.SetMaximumParameters(maximumParameters);
        copy.SetMaximumInputBytes(maximumInputBytes);
        copy.SetMaximumDecodedParameterBytes(maximumDecodedParameterBytes);
        return copy;
    }
}
