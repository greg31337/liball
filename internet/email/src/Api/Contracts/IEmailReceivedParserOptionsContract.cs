using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailReceivedParserOptions"/>.
/// </summary>
public interface IEmailReceivedParserOptionsContract
{
    /// <summary>
    /// Gets the mode.
    /// </summary>
    /// <returns>The current mode.</returns>
    EmailReceivedParseMode GetMode();
    /// <summary>
    /// Sets the mode used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMode(EmailReceivedParseMode value);
    /// <summary>
    /// Gets the maximum clauses.
    /// </summary>
    /// <returns>The current maximum clauses.</returns>
    int GetMaximumClauses();
    /// <summary>
    /// Sets the maximum clauses used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumClauses(int value);
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
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailReceivedParserOptions Clone();
}
