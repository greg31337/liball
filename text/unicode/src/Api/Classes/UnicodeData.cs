namespace unicode;

/// <summary>
/// Provides metadata about the Unicode data compiled into the library.
/// </summary>
public class UnicodeData : IUnicodeData
{
    internal static readonly UnicodeData Instance = new UnicodeData();

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public UnicodeData()
    {
    }

    /// <summary>
    /// Returns the Unicode release number compiled into the library.
    /// </summary>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    public string GetVersion()
    {
        return UnicodeVersion.Version;
    }

    /// <summary>
    /// Returns the version, date, and source URLs for the compiled Unicode data.
    /// </summary>
    /// <returns>An object containing the result described in the summary.</returns>
    public UnicodeDataProvenance GetDataProvenance()
    {
        return new UnicodeDataProvenance(
            UnicodeVersion.Version,
            UnicodeVersion.BaselineDate,
            UnicodeVersion.VersionUrl,
            UnicodeVersion.UcdUrl,
            UnicodeVersion.EmojiUrl,
            UnicodeVersion.ProvenanceDocument);
    }

    /// <summary>
    /// Returns the upstream Unicode data sources used to generate the runtime tables.
    /// </summary>
    /// <returns>A new array containing the resulting values.</returns>
    public UnicodeDataSource[] GetDataSources()
    {
        return new UnicodeDataSource[]
        {
            new UnicodeDataSource("Unicode Version", UnicodeVersion.VersionUrl, "Unicode version baseline"),
            new UnicodeDataSource("Unicode Character Database", UnicodeVersion.UcdUrl, "UCD source files"),
            new UnicodeDataSource("Unicode Emoji Data", UnicodeVersion.EmojiUrl, "Emoji property source files"),
        };
    }
}
