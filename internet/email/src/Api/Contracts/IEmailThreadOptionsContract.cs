using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailThreadOptions"/>.
/// </summary>
public interface IEmailThreadOptionsContract
{
    /// <summary>
    /// Gets the maximum header count.
    /// </summary>
    /// <returns>The current maximum header count.</returns>
    int GetMaximumHeaderCount();
    /// <summary>
    /// Sets the maximum header count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumHeaderCount(int value);
    /// <summary>
    /// Gets the maximum identifier count.
    /// </summary>
    /// <returns>The current maximum identifier count.</returns>
    int GetMaximumIdentifierCount();
    /// <summary>
    /// Sets the maximum identifier count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumIdentifierCount(int value);
    /// <summary>
    /// Gets the maximum identifier bytes.
    /// </summary>
    /// <returns>The current maximum identifier bytes.</returns>
    int GetMaximumIdentifierBytes();
    /// <summary>
    /// Sets the maximum identifier bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumIdentifierBytes(int value);
    /// <summary>
    /// Gets the maximum subject bytes.
    /// </summary>
    /// <returns>The current maximum subject bytes.</returns>
    int GetMaximumSubjectBytes();
    /// <summary>
    /// Sets the maximum subject bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumSubjectBytes(int value);
    /// <summary>
    /// Gets the maximum subject characters.
    /// </summary>
    /// <returns>The current maximum subject characters.</returns>
    int GetMaximumSubjectCharacters();
    /// <summary>
    /// Sets the maximum subject characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumSubjectCharacters(int value);
    /// <summary>
    /// Gets the maximum graph message count.
    /// </summary>
    /// <returns>The current maximum graph message count.</returns>
    int GetMaximumGraphMessageCount();
    /// <summary>
    /// Sets the maximum graph message count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumGraphMessageCount(int value);
    /// <summary>
    /// Gets the maximum graph identifier count.
    /// </summary>
    /// <returns>The current maximum graph identifier count.</returns>
    int GetMaximumGraphIdentifierCount();
    /// <summary>
    /// Sets the maximum graph identifier count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumGraphIdentifierCount(int value);
    /// <summary>
    /// Gets the allow recovered identifiers.
    /// </summary>
    /// <returns><c>true</c> when the allow recovered identifiers setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetAllowRecoveredIdentifiers();
    /// <summary>
    /// Sets the allow recovered identifiers used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the allow recovered identifiers behavior.</param>
    void SetAllowRecoveredIdentifiers(bool value);
    /// <summary>
    /// Gets the enable subject fallback.
    /// </summary>
    /// <returns><c>true</c> when the enable subject fallback setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetEnableSubjectFallback();
    /// <summary>
    /// Sets the enable subject fallback used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the enable subject fallback behavior.</param>
    void SetEnableSubjectFallback(bool value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailThreadOptions Clone();
}
