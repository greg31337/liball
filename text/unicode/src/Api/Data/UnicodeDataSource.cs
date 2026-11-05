namespace unicode;

/// <summary>
/// Describes one upstream data source used to build the runtime tables.
/// </summary>
public class UnicodeDataSource
{
    /// <summary>
    /// The human-readable name.
    /// </summary>
    public string Name;
    /// <summary>
    /// The absolute URL of the source.
    /// </summary>
    public string Url;
    /// <summary>
    /// A plain-English description of how the source data is used.
    /// </summary>
    public string Purpose;

    /// <summary>
    /// Initializes a new instance with the supplied values.
    /// </summary>
    /// <param name="name">The name to store or look up; name lookup is case-insensitive.</param>
    /// <param name="url">The absolute source URL.</param>
    /// <param name="purpose">The description of how the source is used.</param>
    public UnicodeDataSource(string name, string url, string purpose)
    {
        Name = name;
        Url = url;
        Purpose = purpose;
    }
}
