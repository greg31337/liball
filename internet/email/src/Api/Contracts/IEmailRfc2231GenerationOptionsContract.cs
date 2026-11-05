using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailRfc2231GenerationOptions"/>.
/// </summary>
public interface IEmailRfc2231GenerationOptionsContract
{
    /// <summary>
    /// Gets the charset.
    /// </summary>
    /// <returns>The current charset.</returns>
    string GetCharset();
    /// <summary>
    /// Sets the charset used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetCharset(string value);
    /// <summary>
    /// Gets the language.
    /// </summary>
    /// <returns>The current language.</returns>
    string GetLanguage();
    /// <summary>
    /// Sets the language used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetLanguage(string value);
    /// <summary>
    /// Gets the first line column.
    /// </summary>
    /// <returns>The current first line column.</returns>
    int GetFirstLineColumn();
    /// <summary>
    /// Sets the first line column used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetFirstLineColumn(int value);
    /// <summary>
    /// Gets the maximum line length.
    /// </summary>
    /// <returns>The current maximum line length.</returns>
    int GetMaximumLineLength();
    /// <summary>
    /// Sets the maximum line length used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumLineLength(int value);
    /// <summary>
    /// Gets the maximum segments.
    /// </summary>
    /// <returns>The current maximum segments.</returns>
    int GetMaximumSegments();
    /// <summary>
    /// Sets the maximum segments used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumSegments(int value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailRfc2231GenerationOptions Clone();
}
