using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailHeaderBlockParserOptions"/>.
/// </summary>
public interface IEmailHeaderBlockParserOptionsContract
{
    /// <summary>
    /// Gets the limits.
    /// </summary>
    /// <returns>The current limits.</returns>
    EmailParsingLimits GetLimits();
    /// <summary>
    /// Sets the limits used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetLimits(EmailParsingLimits value);
    /// <summary>
    /// Gets the mode.
    /// </summary>
    /// <returns>The current mode.</returns>
    EmailStructuredValueParseMode GetMode();
    /// <summary>
    /// Sets the mode used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMode(EmailStructuredValueParseMode value);
    /// <summary>
    /// Gets the compatibility options.
    /// </summary>
    /// <returns>The current compatibility options.</returns>
    EmailCompatibilityOptions GetCompatibilityOptions();
    /// <summary>
    /// Sets the compatibility options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetCompatibilityOptions(EmailCompatibilityOptions value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailHeaderBlockParserOptions Clone();
}
