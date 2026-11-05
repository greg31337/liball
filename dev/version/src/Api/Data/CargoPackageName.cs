namespace version;

/// <summary>
/// Represents a cargo package name.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class CargoPackageName
{
    /// <summary>
    /// Stores the <c>value</c> value.
    /// </summary>
    public string value;
    /// <summary>
    /// Stores the <c>normalizedValue</c> value.
    /// </summary>
    public string normalizedValue;

    /// <summary>
    /// Initializes a new Cargo Package Name instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="valueText">The value text to process.</param>
    public CargoPackageName(string valueText)
    {
        ValidateName(valueText, "Cargo package name cannot be empty or contain invalid characters.");

        value = valueText;
        normalizedValue = valueText.ToLowerInvariant();
    }

    /// <summary>
    /// Creates a new instance from the supplied identity and metadata values.
    /// </summary>
    /// <param name="valueText">The value text to process.</param>
    /// <returns>The resulting cargo package name.</returns>
    internal static CargoPackageName Create(string valueText)
    {
        return new CargoPackageName(valueText);
    }

    /// <summary>
    /// Gets a value.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetValue()
    {
        return value;
    }

    /// <summary>
    /// Gets a normalized value.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetNormalizedValue()
    {
        return normalizedValue;
    }

    /// <summary>
    /// Reports whether this value matches the supplied text under its ecosystem comparison rules.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal bool MatchesText(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        return normalizedValue == text.ToLowerInvariant();
    }

    /// <summary>
    /// Reports whether both normalized package names are equal.
    /// </summary>
    /// <param name="other">The other used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal bool EqualsName(CargoPackageName other)
    {
        System.ArgumentNullException.ThrowIfNull(other);

        return normalizedValue == other.GetNormalizedValue();
    }

    /// <summary>
    /// Validates a name.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="message">The message used by the operation.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    internal static void ValidateName(string text, string message)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        if (text.Length == 0 || !VersionChar.IsAlphaNumeric(text[0]))
        {
            throw new System.ArgumentException(message);
        }

        int index = 0;
        while (index < text.Length)
        {
            if (!VersionChar.IsAlphaNumeric(text[index]) && text[index] != '-' && text[index] != '_')
            {
                throw new System.ArgumentException(message);
            }

            index = index + 1;
        }
    }
}
