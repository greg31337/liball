namespace version;

/// <summary>
/// Represents a python local version segment.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class PythonLocalVersionSegment
{
    /// <summary>
    /// Stores the <c>text</c> value.
    /// </summary>
    public string text;
    /// <summary>
    /// Stores the <c>numeric</c> value.
    /// </summary>
    public bool numeric;

    /// <summary>
    /// Initializes a new Python Local Version Segment instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="textValue">The text value to process.</param>
    /// <param name="numericValue">Whether to enable numeric value.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    public PythonLocalVersionSegment(string textValue, bool numericValue)
    {
        System.ArgumentNullException.ThrowIfNull(textValue);

        if (textValue.Length == 0)
        {
            throw new System.ArgumentException("Python local version segment text cannot be empty.");
        }

        if (!ContainsOnlyAlphaNumeric(textValue))
        {
            throw new System.ArgumentException("Python local version segment text must be alphanumeric.");
        }

        if (numericValue && !ContainsOnlyDigits(textValue))
        {
            throw new System.ArgumentException("Numeric Python local version segments must contain only digits.");
        }

        if (!numericValue && !ContainsAnyLetters(textValue))
        {
            throw new System.ArgumentException("Text Python local version segments must contain at least one letter.");
        }

        text = textValue;
        numeric = numericValue;
    }

    /// <summary>
    /// Gets a text.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetText()
    {
        return text;
    }

    /// <summary>
    /// Reports whether numeric.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsNumeric()
    {
        return numeric;
    }

    /// <summary>
    /// Reports whether text.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsText()
    {
        return !numeric;
    }

    internal static bool ContainsOnlyAlphaNumeric(string text)
    {
        int index = 0;

        while (index < text.Length)
        {
            if (!VersionChar.IsAlphaNumeric(text[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool ContainsOnlyDigits(string text)
    {
        int index = 0;

        while (index < text.Length)
        {
            if (!VersionChar.IsDigit(text[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool ContainsAnyLetters(string text)
    {
        int index = 0;

        while (index < text.Length)
        {
            if (VersionChar.IsLetter(text[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
