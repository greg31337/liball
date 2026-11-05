namespace csv;

/// <summary>
/// Specifies how rows shorter than the header are completed.
/// </summary>
public enum CsvMissingFieldMode
{
    /// <summary>
    /// Uses an empty string for missing content.
    /// </summary>
    Empty = 0,
    /// <summary>
    /// Uses null values for missing fields.
    /// </summary>
    Null = 1,
    /// <summary>
    /// Keeps the short row and reports a diagnostic.
    /// </summary>
    Error = 2
}
