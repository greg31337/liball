using System;

namespace email;
/// <summary>
/// Controls how header block parser operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailHeaderBlockParserOptions : IEmailHeaderBlockParserOptionsContract
{
    EmailParsingLimits limits;
    EmailStructuredValueParseMode mode;
    EmailCompatibilityOptions compatibilityOptions;
    /// <summary>
    /// Creates a new header block parser options object.
    /// </summary>
    public EmailHeaderBlockParserOptions()
    {
        limits = new EmailParsingLimits();
        mode = EmailStructuredValueParseMode.Tolerant;
        compatibilityOptions = new EmailCompatibilityOptions();
        compatibilityOptions.SetAllowMissingHeaderBodySeparator(false);
        compatibilityOptions.SetReturnPartialResults(false);
    }

    /// <summary>
    /// Gets the limits.
    /// </summary>
    /// <returns>The current limits.</returns>
    public EmailParsingLimits GetLimits()
    {
        return limits.Clone();
    }

    /// <summary>
    /// Sets the limits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetLimits(EmailParsingLimits value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        limits = value.Clone();
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
    /// Gets the compatibility options.
    /// </summary>
    /// <returns>The current compatibility options.</returns>
    public EmailCompatibilityOptions GetCompatibilityOptions()
    {
        return compatibilityOptions.Clone();
    }

    /// <summary>
    /// Sets the compatibility options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetCompatibilityOptions(EmailCompatibilityOptions value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        compatibilityOptions = value.Clone();
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailHeaderBlockParserOptions Clone()
    {
        EmailHeaderBlockParserOptions copy = new EmailHeaderBlockParserOptions();
        copy.SetLimits(limits);
        copy.SetMode(mode);
        copy.SetCompatibilityOptions(compatibilityOptions);
        return copy;
    }
}
