namespace fake;

/// <summary>
/// Represents correlated fictional country, language, currency, calling-code, and time-zone metadata.
/// </summary>
/// <remarks>Code-shaped values are synthetic fixtures and are not claimed to be official assignments.</remarks>
public class CountryDetails
{
    /// <summary>The fictional country display name.</summary>
    public string Country = string.Empty;

    /// <summary>The fictional country abbreviation.</summary>
    public string Abbreviation = string.Empty;

    /// <summary>The alpha-2 country-code-shaped value.</summary>
    public string Alpha2Code = string.Empty;

    /// <summary>The alpha-3 country-code-shaped value.</summary>
    public string Alpha3Code = string.Empty;

    /// <summary>The three-digit country-code-shaped value.</summary>
    public string NumericCode = string.Empty;

    /// <summary>The fictional language display name.</summary>
    public string Language = string.Empty;

    /// <summary>The language-code-shaped value.</summary>
    public string LanguageCode = string.Empty;

    /// <summary>The fictional currency display name.</summary>
    public string Currency = string.Empty;

    /// <summary>The currency-code-shaped value.</summary>
    public string CurrencyCode = string.Empty;

    /// <summary>The calling-code-shaped value.</summary>
    public string CallingCode = string.Empty;

    /// <summary>The associated bundled IANA time-zone identifier.</summary>
    public string TimeZoneId = string.Empty;

    internal CountryDetails()
    {
    }
}
