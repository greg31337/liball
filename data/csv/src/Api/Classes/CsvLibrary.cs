namespace csv;

/// <summary>
/// Exposes identifying information about the CSV library.
/// </summary>
public class CsvLibrary : ICsvLibrary
{
    /// <summary>
    /// Initializes a library information provider.
    /// </summary>
    public CsvLibrary()
    {
    }

    /// <summary>
    /// Creates a library information object.
    /// </summary>
    /// <returns>The initialized result.</returns>
    internal static CsvLibrary Create()
    {
        CsvLibrary library = new CsvLibrary();
        return library;
    }

    /// <summary>
    /// Returns the namespace containing the public API.
    /// </summary>
    /// <returns><see cref="CsvLibraryInfo.NamespaceName"/>.</returns>
    public string GetNamespaceName()
    {
        return CsvLibraryInfo.NamespaceName;
    }

    /// <summary>
    /// Returns the human-readable library name.
    /// </summary>
    /// <returns><see cref="CsvLibraryInfo.DisplayName"/>.</returns>
    public string GetDisplayName()
    {
        return CsvLibraryInfo.DisplayName;
    }
}
