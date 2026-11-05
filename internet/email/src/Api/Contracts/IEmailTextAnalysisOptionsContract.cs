using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailTextAnalysisOptions"/>.
/// </summary>
public interface IEmailTextAnalysisOptionsContract
{
    /// <summary>
    /// Gets the maximum characters.
    /// </summary>
    /// <returns>The current maximum characters.</returns>
    int GetMaximumCharacters();
    /// <summary>
    /// Sets the maximum characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumCharacters(int value);
    /// <summary>
    /// Gets the maximum lines.
    /// </summary>
    /// <returns>The current maximum lines.</returns>
    int GetMaximumLines();
    /// <summary>
    /// Sets the maximum lines used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumLines(int value);
    /// <summary>
    /// Gets the maximum snippet characters.
    /// </summary>
    /// <returns>The current maximum snippet characters.</returns>
    int GetMaximumSnippetCharacters();
    /// <summary>
    /// Sets the maximum snippet characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumSnippetCharacters(int value);
    /// <summary>
    /// Gets the include quoted text in snippet.
    /// </summary>
    /// <returns><c>true</c> when the include quoted text in snippet setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetIncludeQuotedTextInSnippet();
    /// <summary>
    /// Sets the include quoted text in snippet used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the include quoted text in snippet behavior.</param>
    void SetIncludeQuotedTextInSnippet(bool value);
    /// <summary>
    /// Gets the include signature in snippet.
    /// </summary>
    /// <returns><c>true</c> when the include signature in snippet setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetIncludeSignatureInSnippet();
    /// <summary>
    /// Sets the include signature in snippet used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the include signature in snippet behavior.</param>
    void SetIncludeSignatureInSnippet(bool value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailTextAnalysisOptions Clone();
}
