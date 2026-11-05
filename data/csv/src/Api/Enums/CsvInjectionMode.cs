namespace csv;

/// <summary>
/// Specifies how spreadsheet formula-like output is handled.
/// </summary>
public enum CsvInjectionMode
{
    /// <summary>
    /// Writes formula-like values unchanged.
    /// </summary>
    None = 0,
    /// <summary>
    /// Removes leading formula marker characters.
    /// </summary>
    Strip = 1,
    /// <summary>
    /// Prefixes formula-like text with an apostrophe.
    /// </summary>
    Escape = 2,
    /// <summary>
    /// Rejects formula-like output with an exception.
    /// </summary>
    Error = 3
}
