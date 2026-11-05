namespace csv;

/// <summary>
/// Controls header discovery, generation, lookup, and field-count policies.
/// </summary>
public class CsvHeaderOptions
{
    /// <summary>
    /// Whether the first record is a header.
    /// </summary>
    public bool HasHeader;
    /// <summary>
    /// Whether names are generated when no header record exists.
    /// </summary>
    public bool GenerateHeaders;
    /// <summary>
    /// Prefix for generated names; a one-based column number is appended.
    /// </summary>
    public string GeneratedHeaderPrefix;
    /// <summary>
    /// Whether names are trimmed and internal whitespace is collapsed.
    /// </summary>
    public bool NormalizeHeaderNames;
    /// <summary>
    /// Whether name lookup distinguishes letter case.
    /// </summary>
    public bool CaseSensitive;
    /// <summary>
    /// The repeated-name policy.
    /// </summary>
    public CsvDuplicateHeaderMode DuplicateHeaderMode;
    /// <summary>
    /// The empty-header-name policy.
    /// </summary>
    public CsvMissingHeaderMode MissingHeaderMode;
    /// <summary>
    /// The short-row policy.
    /// </summary>
    public CsvMissingFieldMode MissingFieldMode;
    /// <summary>
    /// The wide-row policy.
    /// </summary>
    public CsvExtraFieldMode ExtraFieldMode;

    /// <summary>
    /// Initializes a new CsvHeaderOptions with library defaults.
    /// </summary>
    public CsvHeaderOptions()
    {
        HasHeader = false;
        GenerateHeaders = true;
        GeneratedHeaderPrefix = "Column";
        NormalizeHeaderNames = false;
        CaseSensitive = true;
        DuplicateHeaderMode = CsvDuplicateHeaderMode.Error;
        MissingHeaderMode = CsvMissingHeaderMode.Generate;
        MissingFieldMode = CsvMissingFieldMode.Empty;
        ExtraFieldMode = CsvExtraFieldMode.Allow;
    }

    /// <summary>
    /// Creates an instance populated with library defaults.
    /// </summary>
    /// <returns>The initialized result.</returns>
    internal static CsvHeaderOptions CreateDefault()
    {
        CsvHeaderOptions options = new CsvHeaderOptions();
        return options;
    }
}
