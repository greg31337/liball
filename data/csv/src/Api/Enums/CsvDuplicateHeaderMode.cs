namespace csv;

/// <summary>
/// Specifies how repeated header names are handled.
/// </summary>
public enum CsvDuplicateHeaderMode
{
    /// <summary>
    /// Keeps repeated names and permits lookup using header search rules.
    /// </summary>
    Allow = 0,
    /// <summary>
    /// Resolves a repeated name to its first occurrence.
    /// </summary>
    FirstWins = 1,
    /// <summary>
    /// Resolves a repeated name to its last occurrence.
    /// </summary>
    LastWins = 2,
    /// <summary>
    /// Reports a diagnostic when a header repeats a name.
    /// </summary>
    Error = 3
}
