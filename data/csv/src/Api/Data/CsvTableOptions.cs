namespace csv;

/// <summary>
/// Controls how a reader is materialized into an in-memory table.
/// </summary>
public class CsvTableOptions
{
    /// <summary>
    /// Maximum rows written or loaded; zero means no limit.
    /// </summary>
    public int MaxRows;
    /// <summary>
    /// Whether the reader header is copied into the table.
    /// </summary>
    public bool IncludeHeader;
    /// <summary>
    /// Whether loading copies each reader row.
    /// </summary>
    public bool CloneRows;
    /// <summary>
    /// Whether cloned rows retain diagnostics.
    /// </summary>
    public bool PreserveDiagnostics;

    /// <summary>
    /// Initializes a new CsvTableOptions with library defaults.
    /// </summary>
    public CsvTableOptions()
    {
        MaxRows = 0;
        IncludeHeader = true;
        CloneRows = true;
        PreserveDiagnostics = true;
    }

    /// <summary>
    /// Creates an instance populated with library defaults.
    /// </summary>
    /// <returns>The initialized result.</returns>
    internal static CsvTableOptions CreateDefault()
    {
        CsvTableOptions options = new CsvTableOptions();
        return options;
    }
}
