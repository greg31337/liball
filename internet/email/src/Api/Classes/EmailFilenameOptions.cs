using System;

namespace email;
/// <summary>
/// Controls how filename operations behave.
/// </summary>
/// <remarks>
/// The constructor establishes a complete default policy. Setters reject unsupported values before the options are used by an operation.
/// </remarks>
public class EmailFilenameOptions : IEmailFilenameOptionsContract
{
    /// <summary>
    /// The default maximum length used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumLength = 255;
    EmailFilenameNormalization normalization;
    int maximumLength;
    string fallbackFilename;
    char replacementCharacter;
    bool caseInsensitiveCollisions;
    /// <summary>
    /// Creates a new filename options object.
    /// </summary>
    public EmailFilenameOptions()
    {
        normalization = EmailFilenameNormalization.Nfc;
        maximumLength = DefaultMaximumLength;
        fallbackFilename = "attachment";
        replacementCharacter = '_';
        caseInsensitiveCollisions = true;
    }

    /// <summary>
    /// Gets the normalization.
    /// </summary>
    /// <returns>The current normalization.</returns>
    public EmailFilenameNormalization GetNormalization()
    {
        return normalization;
    }

    /// <summary>
    /// Sets the normalization used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetNormalization(EmailFilenameNormalization value)
    {
        if (value < EmailFilenameNormalization.None || value > EmailFilenameNormalization.Nfkc)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        normalization = value;
    }

    /// <summary>
    /// Gets the maximum length.
    /// </summary>
    /// <returns>The current maximum length.</returns>
    public int GetMaximumLength()
    {
        return maximumLength;
    }

    /// <summary>
    /// Sets the maximum length used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void SetMaximumLength(int value)
    {
        if (value < 16)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        maximumLength = value;
    }

    /// <summary>
    /// Gets the fallback filename.
    /// </summary>
    /// <returns>The current fallback filename.</returns>
    public string GetFallbackFilename()
    {
        return fallbackFilename;
    }

    /// <summary>
    /// Sets the fallback filename used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void SetFallbackFilename(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value.Length == 0)
        {
            throw new ArgumentException("A fallback filename cannot be empty.", "value");
        }

        fallbackFilename = value;
    }

    /// <summary>
    /// Gets the replacement character.
    /// </summary>
    /// <returns>The current replacement character.</returns>
    public char GetReplacementCharacter()
    {
        return replacementCharacter;
    }

    /// <summary>
    /// Sets the replacement character used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    public void SetReplacementCharacter(char value)
    {
        if (char.IsControl(value) || char.IsSurrogate(value) || char.IsWhiteSpace(value) || value == '.' || new EmailFilename().IsForbiddenFilenameCharacter(value))
        {
            throw new ArgumentException("The replacement character must be a safe visible filename character.", "value");
        }

        replacementCharacter = value;
    }

    /// <summary>
    /// Gets the case insensitive collisions.
    /// </summary>
    /// <returns><c>true</c> when the case insensitive collisions setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetCaseInsensitiveCollisions()
    {
        return caseInsensitiveCollisions;
    }

    /// <summary>
    /// Sets the case insensitive collisions used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the case insensitive collisions behavior.</param>
    public void SetCaseInsensitiveCollisions(bool value)
    {
        caseInsensitiveCollisions = value;
    }

    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    public EmailFilenameOptions Clone()
    {
        EmailFilenameOptions copy = new EmailFilenameOptions();
        copy.SetNormalization(normalization);
        copy.SetMaximumLength(maximumLength);
        copy.SetFallbackFilename(fallbackFilename);
        copy.SetReplacementCharacter(replacementCharacter);
        copy.SetCaseInsensitiveCollisions(caseInsensitiveCollisions);
        return copy;
    }
}
