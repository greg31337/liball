namespace version;

/// <summary>
/// Represents a version identifier.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class VersionIdentifier
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
    /// Initializes a new Version Identifier instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="textValue">The text value to process.</param>
    /// <param name="numericValue">Whether to enable numeric value.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    public VersionIdentifier(string textValue, bool numericValue)
    {
        System.ArgumentNullException.ThrowIfNull(textValue);

        if (textValue.Length == 0)
        {
            throw new System.ArgumentException("Identifier text cannot be empty.");
        }

        if (numericValue && !ContainsOnlyDigits(textValue))
        {
            throw new System.ArgumentException("Numeric identifier text must contain only digits.");
        }

        text = textValue;
        numeric = numericValue;
    }

    /// <summary>
    /// Creates a numeric.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting version identifier.</returns>
    internal static VersionIdentifier CreateNumeric(string text)
    {
        return new VersionIdentifier(text, true);
    }

    /// <summary>
    /// Creates a text.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting version identifier.</returns>
    internal static VersionIdentifier CreateText(string text)
    {
        return new VersionIdentifier(text, false);
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
    /// Gets a length.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetLength()
    {
        return text.Length;
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

    /// <summary>
    /// Reports whether this value has leading zero.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasLeadingZero()
    {
        return numeric && text.Length > 1 && text[0] == '0';
    }

    internal static bool ContainsOnlyDigits(string value)
    {
        int index = 0;

        while (index < value.Length)
        {
            if (!VersionChar.IsDigit(value[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
