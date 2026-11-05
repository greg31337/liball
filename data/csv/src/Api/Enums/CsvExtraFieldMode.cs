namespace csv;

/// <summary>
/// Specifies how fields beyond the header width are handled.
/// </summary>
public enum CsvExtraFieldMode
{
    /// <summary>
    /// Keeps fields beyond the header width.
    /// </summary>
    Allow = 0,
    /// <summary>
    /// Removes fields beyond the header width.
    /// </summary>
    Ignore = 1,
    /// <summary>
    /// Keeps the row and reports a diagnostic for extra fields.
    /// </summary>
    Error = 2
}
