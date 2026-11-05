using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailEncodedWordGenerationOptions"/>.
/// </summary>
public interface IEmailEncodedWordGenerationOptionsContract
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
    /// Determines whether the language is present or available.
    /// </summary>
    /// <returns><c>true</c> when the language is available; otherwise, <c>false</c>.</returns>
    bool HasLanguage();
    /// <summary>
    /// Gets the language.
    /// </summary>
    /// <returns>The current language.</returns>
    string? GetLanguage();
    /// <summary>
    /// Sets the language used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetLanguage(string? value);
    /// <summary>
    /// Clears the stored language value or values.
    /// </summary>
    void ClearLanguage();
    /// <summary>
    /// Gets the context.
    /// </summary>
    /// <returns>The current context.</returns>
    EmailEncodedWordContext GetContext();
    /// <summary>
    /// Sets the context used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetContext(EmailEncodedWordContext value);
    /// <summary>
    /// Gets the encoding choice.
    /// </summary>
    /// <returns>The current encoding choice.</returns>
    EmailEncodedWordEncodingChoice GetEncodingChoice();
    /// <summary>
    /// Sets the encoding choice used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetEncodingChoice(EmailEncodedWordEncodingChoice value);
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
    /// Gets the maximum encoded words.
    /// </summary>
    /// <returns>The current maximum encoded words.</returns>
    int GetMaximumEncodedWords();
    /// <summary>
    /// Sets the maximum encoded words used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumEncodedWords(int value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailEncodedWordGenerationOptions Clone();
}
