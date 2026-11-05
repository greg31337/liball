namespace currencycodes;

/// <summary>
/// Stores the versions and checksums that identify the generated data snapshot.
/// </summary>
public class DataVersionInfo
{
    /// <summary>
    /// Stores the ISO 4217 publication date.
    /// </summary>
    public string IsoPublicationDate = string.Empty;

    /// <summary>
    /// Stores the CLDR release version.
    /// </summary>
    public string CldrVersion = string.Empty;

    /// <summary>
    /// Stores the generator schema version.
    /// </summary>
    public string GeneratorSchemaVersion = string.Empty;

    /// <summary>
    /// Stores the content-derived generated snapshot identifier.
    /// </summary>
    public string SnapshotIdentifier = string.Empty;

    /// <summary>
    /// Stores detached canonical source-checksum entries.
    /// </summary>
    public string[] SourceChecksums = new string[0];
}
