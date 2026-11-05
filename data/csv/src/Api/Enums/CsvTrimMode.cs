namespace csv;

/// <summary>
/// Specifies which field values have surrounding whitespace removed.
/// </summary>
public enum CsvTrimMode
{
    /// <summary>
    /// Preserves surrounding whitespace.
    /// </summary>
    None = 0,
    /// <summary>
    /// Trims both ends of unquoted fields only.
    /// </summary>
    Unquoted = 1,
    /// <summary>
    /// Trims both ends of quoted and unquoted fields.
    /// </summary>
    QuotedAndUnquoted = 2,
    /// <summary>
    /// Trims leading whitespace from every field.
    /// </summary>
    Left = 3,
    /// <summary>
    /// Trims trailing whitespace from every field.
    /// </summary>
    Right = 4,
    /// <summary>
    /// Trims both ends of every field.
    /// </summary>
    Both = 5
}
