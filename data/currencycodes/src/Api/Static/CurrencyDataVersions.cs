namespace currencycodes;

/// <summary>
/// Identifies the pinned source data and generated snapshot exposed by this library.
/// </summary>
public static class CurrencyDataVersions
{
    /// <summary>
    /// Stores the ISO 4217 publication date.
    /// </summary>
    public const string IsoPublicationDate = "2026-01-01";

    /// <summary>
    /// Stores the pinned CLDR release version.
    /// </summary>
    public const string CldrVersion = "48.2";

    /// <summary>
    /// Stores the generator schema version.
    /// </summary>
    public const string GeneratorSchemaVersion = "1";

    /// <summary>
    /// Stores the content-derived identifier for the pinned generated snapshot.
    /// </summary>
    public const string SnapshotIdentifier = "sha256:45c22d81e49767588f1509e6028b964f988a2c83f813184c00994bae30204a92";
}
