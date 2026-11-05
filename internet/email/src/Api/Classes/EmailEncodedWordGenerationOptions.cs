using System;

namespace email;
/// <summary>
/// Controls how encoded word generation operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailEncodedWordGenerationOptions : IEmailEncodedWordGenerationOptionsContract
{
    /// <summary>
    /// The default maximum line length used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumLineLength = 76;
    string charset;
    string? language;
    bool languagePresent;
    EmailEncodedWordContext context;
    EmailEncodedWordEncodingChoice encodingChoice;
    int firstLineColumn;
    int maximumLineLength;
    int maximumEncodedWords;
    /// <summary>
    /// Creates a new encoded word generation options object.
    /// </summary>
    /// <param name = "charset">The charset used by the operation.</param>
    public EmailEncodedWordGenerationOptions(string charset)
    {
        ValidateCharset(charset, "charset");
        this.charset = charset;
        language = null;
        languagePresent = false;
        context = EmailEncodedWordContext.UnstructuredText;
        encodingChoice = EmailEncodedWordEncodingChoice.Automatic;
        firstLineColumn = 0;
        maximumLineLength = DefaultMaximumLineLength;
        maximumEncodedWords = EmailEncodedWordConstants.DefaultMaximumEncodedWords;
    }

    /// <summary>
    /// Gets the charset.
    /// </summary>
    /// <returns>The current charset.</returns>
    public string GetCharset()
    {
        return charset;
    }

    /// <summary>
    /// Sets the charset used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetCharset(string value)
    {
        ValidateCharset(value, "value");
        charset = value;
    }

    /// <summary>
    /// Determines whether the language is present or available.
    /// </summary>
    /// <returns><c>true</c> when the language is available; otherwise, <c>false</c>.</returns>
    public bool HasLanguage()
    {
        return languagePresent;
    }

    /// <summary>
    /// Gets the language.
    /// </summary>
    /// <returns>The current language.</returns>
    public string? GetLanguage()
    {
        return language;
    }

    /// <summary>
    /// Sets the language used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetLanguage(string? value)
    {
        ValidateLanguage(value, "value");
        language = value;
        languagePresent = true;
    }

    /// <summary>
    /// Clears the stored language value or values.
    /// </summary>
    public void ClearLanguage()
    {
        language = null;
        languagePresent = false;
    }

    /// <summary>
    /// Gets the context.
    /// </summary>
    /// <returns>The current context.</returns>
    public EmailEncodedWordContext GetContext()
    {
        return context;
    }

    /// <summary>
    /// Sets the context used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetContext(EmailEncodedWordContext value)
    {
        if (value < EmailEncodedWordContext.UnstructuredText || value > EmailEncodedWordContext.Forbidden)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        context = value;
    }

    /// <summary>
    /// Gets the encoding choice.
    /// </summary>
    /// <returns>The current encoding choice.</returns>
    public EmailEncodedWordEncodingChoice GetEncodingChoice()
    {
        return encodingChoice;
    }

    /// <summary>
    /// Sets the encoding choice used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetEncodingChoice(EmailEncodedWordEncodingChoice value)
    {
        if (value < EmailEncodedWordEncodingChoice.Automatic || value > EmailEncodedWordEncodingChoice.QuotedPrintable)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        encodingChoice = value;
    }

    /// <summary>
    /// Gets the first line column.
    /// </summary>
    /// <returns>The current first line column.</returns>
    public int GetFirstLineColumn()
    {
        return firstLineColumn;
    }

    /// <summary>
    /// Sets the first line column used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetFirstLineColumn(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        firstLineColumn = value;
    }

    /// <summary>
    /// Gets the maximum line length.
    /// </summary>
    /// <returns>The current maximum line length.</returns>
    public int GetMaximumLineLength()
    {
        return maximumLineLength;
    }

    /// <summary>
    /// Sets the maximum line length used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumLineLength(int value)
    {
        if (value < 1 || value > DefaultMaximumLineLength)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        maximumLineLength = value;
    }

    /// <summary>
    /// Gets the maximum encoded words.
    /// </summary>
    /// <returns>The current maximum encoded words.</returns>
    public int GetMaximumEncodedWords()
    {
        return maximumEncodedWords;
    }

    /// <summary>
    /// Sets the maximum encoded words used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumEncodedWords(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        maximumEncodedWords = value;
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailEncodedWordGenerationOptions Clone()
    {
        EmailEncodedWordGenerationOptions copy = new EmailEncodedWordGenerationOptions(charset);
        if (languagePresent)
        {
            copy.SetLanguage(language);
        }

        copy.SetContext(context);
        copy.SetEncodingChoice(encodingChoice);
        copy.SetFirstLineColumn(firstLineColumn);
        copy.SetMaximumLineLength(maximumLineLength);
        copy.SetMaximumEncodedWords(maximumEncodedWords);
        return copy;
    }

    static void ValidateCharset(string value, string parameterName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (value.Length == 0)
        {
            throw new ArgumentException("Encoded-word charsets cannot be empty.", parameterName);
        }

        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character < 33 || character > 126 || IsEspecial(character) || character == '*')
            {
                throw new ArgumentException("Encoded-word charsets must use the RFC 2047 token syntax.", parameterName);
            }

            index++;
        }
    }

    static void ValidateLanguage(string? value, string parameterName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            bool alphaNumeric = character >= 'A' && character <= 'Z' || character >= 'a' && character <= 'z' || character >= '0' && character <= '9';
            if (!alphaNumeric && character != '-')
            {
                throw new ArgumentException("Encoded-word languages must contain ASCII letters, digits, or hyphens.", parameterName);
            }

            index++;
        }
    }

    static bool IsEspecial(char value)
    {
        return value == '(' || value == ')' || value == '<' || value == '>' || value == '@' || value == ',' || value == ';' || value == ':' || value == '\\' || value == '"' || value == '/' || value == '[' || value == ']' || value == '?' || value == '.' || value == '=';
    }
}
