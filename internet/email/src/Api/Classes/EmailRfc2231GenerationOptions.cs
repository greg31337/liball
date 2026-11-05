using System;

namespace email;
/// <summary>
/// Controls how rfc 2231 generation operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailRfc2231GenerationOptions : IEmailRfc2231GenerationOptionsContract
{
    /// <summary>
    /// The default maximum line length used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumLineLength = 78;
    /// <summary>
    /// The maximum header line length value exposed by this API.
    /// </summary>
    internal const int MaximumHeaderLineLength = 998;
    string charset;
    string language;
    int firstLineColumn;
    int maximumLineLength;
    int maximumSegments;
    /// <summary>
    /// Creates a new rfc 2231 generation options object.
    /// </summary>
    /// <param name = "charset">The charset used by the operation.</param>
    public EmailRfc2231GenerationOptions(string charset)
    {
        ValidateCharset(charset, "charset");
        this.charset = charset;
        language = "";
        firstLineColumn = 0;
        maximumLineLength = DefaultMaximumLineLength;
        maximumSegments = EmailRfc2231Constants.DefaultMaximumSegments;
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
    /// Gets the language.
    /// </summary>
    /// <returns>The current language.</returns>
    public string GetLanguage()
    {
        return language;
    }

    /// <summary>
    /// Sets the language used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    public void SetLanguage(string value)
    {
        ValidateLanguage(value, "value");
        language = value;
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
        if (value < 1 || value > MaximumHeaderLineLength)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        maximumLineLength = value;
    }

    /// <summary>
    /// Gets the maximum segments.
    /// </summary>
    /// <returns>The current maximum segments.</returns>
    public int GetMaximumSegments()
    {
        return maximumSegments;
    }

    /// <summary>
    /// Sets the maximum segments used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumSegments(int value)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        maximumSegments = value;
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailRfc2231GenerationOptions Clone()
    {
        EmailRfc2231GenerationOptions copy = new EmailRfc2231GenerationOptions(charset);
        copy.SetLanguage(language);
        copy.SetFirstLineColumn(firstLineColumn);
        copy.SetMaximumLineLength(maximumLineLength);
        copy.SetMaximumSegments(maximumSegments);
        return copy;
    }

    static void ValidateCharset(string value, string parameterName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character < 33 || character > 126 || IsCharsetSpecial(character))
            {
                throw new ArgumentException("RFC 2231 charsets must use registered MIME token syntax.", parameterName);
            }

            index++;
        }
    }

    static void ValidateLanguage(string value, string parameterName)
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
                throw new ArgumentException("RFC 2231 languages must contain ASCII letters, digits, or hyphens.", parameterName);
            }

            index++;
        }
    }

    static bool IsCharsetSpecial(char value)
    {
        return value == '(' || value == ')' || value == '<' || value == '>' || value == '@' || value == ',' || value == ';' || value == ':' || value == '\\' || value == '"' || value == '/' || value == '[' || value == ']' || value == '?' || value == '=' || value == '*' || value == 39 || value == '%';
    }
}
