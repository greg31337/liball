using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailStructuredValueParserOptions"/>.
/// </summary>
public interface IEmailStructuredValueParserOptionsContract
{
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
    /// Gets the maximum parameters.
    /// </summary>
    /// <returns>The current maximum parameters.</returns>
    int GetMaximumParameters();
    /// <summary>
    /// Sets the maximum parameters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumParameters(int value);
    /// <summary>
    /// Gets the maximum input bytes.
    /// </summary>
    /// <returns>The current maximum input bytes.</returns>
    int GetMaximumInputBytes();
    /// <summary>
    /// Sets the maximum input bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumInputBytes(int value);
    /// <summary>
    /// Gets the maximum decoded parameter bytes.
    /// </summary>
    /// <returns>The current maximum decoded parameter bytes.</returns>
    long GetMaximumDecodedParameterBytes();
    /// <summary>
    /// Sets the maximum decoded parameter bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumDecodedParameterBytes(long value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailStructuredValueParserOptions Clone();
}
