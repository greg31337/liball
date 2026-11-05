namespace unicode;

/// <summary>
/// Provides metadata about the Unicode data compiled into the library.
/// </summary>
public interface IUnicodeData
{
    /// <summary>
    /// Returns the Unicode release number compiled into the library.
    /// </summary>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    string GetVersion();

    /// <summary>
    /// Returns the version, date, and source URLs for the compiled Unicode data.
    /// </summary>
    /// <returns>An object containing the result described in the summary.</returns>
    UnicodeDataProvenance GetDataProvenance();

    /// <summary>
    /// Returns the upstream Unicode data sources used to generate the runtime tables.
    /// </summary>
    /// <returns>A new array containing the resulting values.</returns>
    UnicodeDataSource[] GetDataSources();
}
