namespace currencycodes;

/// <summary>
/// Stores public identity and classification data for a territory or special entity.
/// </summary>
public class TerritoryInfo
{
    /// <summary>
    /// Stores the canonical English territory or entity name.
    /// </summary>
    public string CanonicalName = string.Empty;

    /// <summary>
    /// Indicates whether an alpha-2 territory code is assigned.
    /// </summary>
    public bool HasAlpha2Code = false;

    /// <summary>
    /// Stores the alpha-2 territory code when <see cref="HasAlpha2Code"/> is <c>true</c>.
    /// </summary>
    public string Alpha2Code = string.Empty;

    /// <summary>
    /// Indicates whether an alpha-3 territory code is assigned.
    /// </summary>
    public bool HasAlpha3Code = false;

    /// <summary>
    /// Stores the alpha-3 territory code when <see cref="HasAlpha3Code"/> is <c>true</c>.
    /// </summary>
    public string Alpha3Code = string.Empty;

    /// <summary>
    /// Indicates whether a numeric territory code is assigned.
    /// </summary>
    public bool HasNumericCode = false;

    /// <summary>
    /// Stores the canonical three-digit numeric territory code when <see cref="HasNumericCode"/> is <c>true</c>.
    /// </summary>
    public string NumericCode = string.Empty;

    /// <summary>
    /// Stores the integer view of <see cref="NumericCode"/> when <see cref="HasNumericCode"/> is <c>true</c>.
    /// </summary>
    public int NumericCodeValue = 0;

    /// <summary>
    /// Identifies the classification of the territory or special entity.
    /// </summary>
    public TerritoryKind Kind = TerritoryKind.Other;
}
