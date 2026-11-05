namespace csv;

/// <summary>
/// Specifies when output fields are quoted.
/// </summary>
public enum CsvQuoteMode
{
    /// <summary>
    /// Quotes only fields that require quoting.
    /// </summary>
    Minimal = 0,
    /// <summary>
    /// Quotes every field.
    /// </summary>
    All = 1,
    /// <summary>
    /// Applies no special handling.
    /// </summary>
    None = 2,
    /// <summary>
    /// Quotes non-null fields and leaves null output unquoted.
    /// </summary>
    NonNull = 3
}
