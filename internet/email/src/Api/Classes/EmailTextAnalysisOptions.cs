using System;

namespace email;
/// <summary>
/// Controls how text analysis operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailTextAnalysisOptions : IEmailTextAnalysisOptionsContract
{
    /// <summary>
    /// The default maximum characters used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumCharacters = 10000000;
    /// <summary>
    /// The default maximum lines used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumLines = 100000;
    /// <summary>
    /// The default maximum snippet characters used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumSnippetCharacters = 200;
    int maximumCharacters;
    int maximumLines;
    int maximumSnippetCharacters;
    bool includeQuotedTextInSnippet;
    bool includeSignatureInSnippet;
    /// <summary>
    /// Creates a new text analysis options object.
    /// </summary>
    public EmailTextAnalysisOptions()
    {
        maximumCharacters = DefaultMaximumCharacters;
        maximumLines = DefaultMaximumLines;
        maximumSnippetCharacters = DefaultMaximumSnippetCharacters;
        includeQuotedTextInSnippet = false;
        includeSignatureInSnippet = false;
    }

    /// <summary>
    /// Gets the maximum characters.
    /// </summary>
    /// <returns>The current maximum characters.</returns>
    public int GetMaximumCharacters()
    {
        return maximumCharacters;
    }

    /// <summary>
    /// Sets the maximum characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumCharacters(int value)
    {
        if (value < 1)
            throw new ArgumentOutOfRangeException("value");
        maximumCharacters = value;
    }

    /// <summary>
    /// Gets the maximum lines.
    /// </summary>
    /// <returns>The current maximum lines.</returns>
    public int GetMaximumLines()
    {
        return maximumLines;
    }

    /// <summary>
    /// Sets the maximum lines used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumLines(int value)
    {
        if (value < 1)
            throw new ArgumentOutOfRangeException("value");
        maximumLines = value;
    }

    /// <summary>
    /// Gets the maximum snippet characters.
    /// </summary>
    /// <returns>The current maximum snippet characters.</returns>
    public int GetMaximumSnippetCharacters()
    {
        return maximumSnippetCharacters;
    }

    /// <summary>
    /// Sets the maximum snippet characters used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumSnippetCharacters(int value)
    {
        if (value < 1)
            throw new ArgumentOutOfRangeException("value");
        maximumSnippetCharacters = value;
    }

    /// <summary>
    /// Gets the include quoted text in snippet.
    /// </summary>
    /// <returns><c>true</c> when the include quoted text in snippet setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetIncludeQuotedTextInSnippet()
    {
        return includeQuotedTextInSnippet;
    }

    /// <summary>
    /// Sets the include quoted text in snippet used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the include quoted text in snippet behavior.</param>
    public void SetIncludeQuotedTextInSnippet(bool value)
    {
        includeQuotedTextInSnippet = value;
    }

    /// <summary>
    /// Gets the include signature in snippet.
    /// </summary>
    /// <returns><c>true</c> when the include signature in snippet setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetIncludeSignatureInSnippet()
    {
        return includeSignatureInSnippet;
    }

    /// <summary>
    /// Sets the include signature in snippet used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the include signature in snippet behavior.</param>
    public void SetIncludeSignatureInSnippet(bool value)
    {
        includeSignatureInSnippet = value;
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailTextAnalysisOptions Clone()
    {
        EmailTextAnalysisOptions copy = new EmailTextAnalysisOptions();
        copy.SetMaximumCharacters(maximumCharacters);
        copy.SetMaximumLines(maximumLines);
        copy.SetMaximumSnippetCharacters(maximumSnippetCharacters);
        copy.SetIncludeQuotedTextInSnippet(includeQuotedTextInSnippet);
        copy.SetIncludeSignatureInSnippet(includeSignatureInSnippet);
        return copy;
    }
}
