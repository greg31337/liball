using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailHeaderLexicalOptions"/>.
/// </summary>
public interface IEmailHeaderLexicalOptionsContract
{
    /// <summary>
    /// Gets the mode.
    /// </summary>
    /// <returns>The current mode.</returns>
    EmailHeaderLexicalMode GetMode();
    /// <summary>
    /// Sets the mode used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMode(EmailHeaderLexicalMode value);
    /// <summary>
    /// Gets the maximum tokens.
    /// </summary>
    /// <returns>The current maximum tokens.</returns>
    int GetMaximumTokens();
    /// <summary>
    /// Sets the maximum tokens used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumTokens(int value);
    /// <summary>
    /// Gets the maximum token bytes.
    /// </summary>
    /// <returns>The current maximum token bytes.</returns>
    int GetMaximumTokenBytes();
    /// <summary>
    /// Sets the maximum token bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumTokenBytes(int value);
    /// <summary>
    /// Gets the maximum comment depth.
    /// </summary>
    /// <returns>The current maximum comment depth.</returns>
    int GetMaximumCommentDepth();
    /// <summary>
    /// Sets the maximum comment depth used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumCommentDepth(int value);
    /// <summary>
    /// Gets the allow internationalized.
    /// </summary>
    /// <returns><c>true</c> when the allow internationalized setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowInternationalized();
    /// <summary>
    /// Sets the allow internationalized used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow internationalized behavior.</param>
    void SetAllowInternationalized(bool value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailHeaderLexicalOptions Clone();
}
