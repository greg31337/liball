namespace csv;

/// <summary>
/// Specifies how empty header names are handled.
/// </summary>
public enum CsvMissingHeaderMode
{
    /// <summary>
    /// Creates a name for each empty header field.
    /// </summary>
    Generate = 0,
    /// <summary>
    /// Keeps empty header names.
    /// </summary>
    Empty = 1,
    /// <summary>
    /// Keeps the empty name and reports a diagnostic.
    /// </summary>
    Error = 2
}
