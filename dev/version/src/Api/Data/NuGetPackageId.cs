namespace version;

/// <summary>
/// Represents a nuget package id.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class NuGetPackageId
{
    /// <summary>
    /// Defines the maximum number of characters accepted in a NuGet package identifier.
    /// </summary>
    public const int MaxLength = 100;

    /// <summary>
    /// Stores the <c>value</c> value.
    /// </summary>
    public string value;
    /// <summary>
    /// Stores the <c>normalizedValue</c> value.
    /// </summary>
    public string normalizedValue;

    /// <summary>
    /// Initializes a new NuGet Package Id instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="valueText">The value text to process.</param>
    public NuGetPackageId(string valueText)
    {
        Validate(valueText);

        value = valueText;
        normalizedValue = Normalize(valueText);
    }

    /// <summary>
    /// Creates a new instance from the supplied identity and metadata values.
    /// </summary>
    /// <param name="valueText">The value text to process.</param>
    /// <returns>The resulting nuget package id.</returns>
    internal static NuGetPackageId Create(string valueText)
    {
        return new NuGetPackageId(valueText);
    }

    /// <summary>
    /// Normalizes a NuGet package identifier for case-insensitive comparison.
    /// </summary>
    /// <param name="valueText">The value text to process.</param>
    /// <returns>The resulting text.</returns>
    internal static string NormalizeId(string valueText)
    {
        Validate(valueText);
        return Normalize(valueText);
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
    /// Reports whether both identifiers are equal under their ecosystem comparison rules.
    /// </summary>
    /// <param name="other">The other used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    internal bool EqualsId(NuGetPackageId other)
    {
        System.ArgumentNullException.ThrowIfNull(other);

        return normalizedValue == other.GetNormalizedValue();
    }

    /// <summary>
    /// Reports whether this value matches the supplied text under its ecosystem comparison rules.
    /// </summary>
    /// <param name="valueText">The value text to process.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool MatchesText(string valueText)
    {
        return normalizedValue == NormalizeId(valueText);
    }

    internal static void Validate(string valueText)
    {
        System.ArgumentNullException.ThrowIfNull(valueText);

        if (valueText.Length == 0)
        {
            throw new System.ArgumentException("NuGet package id cannot be empty.");
        }

        if (valueText.Length > MaxLength)
        {
            throw new System.ArgumentException("NuGet package id exceeds the maximum length.");
        }

        int index = 0;
        while (index < valueText.Length)
        {
            char valueChar = valueText[index];
            if (!VersionChar.IsAlphaNumeric(valueChar) && valueChar != '.' && valueChar != '-' && valueChar != '_')
            {
                throw new System.ArgumentException("NuGet package id contains an invalid character.");
            }

            index = index + 1;
        }
    }

    internal static string Normalize(string valueText)
    {
        return valueText.ToLowerInvariant();
    }
}
