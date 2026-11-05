namespace csv;

/// <summary>
/// Specifies how malformed CSV input is handled.
/// </summary>
public enum CsvErrorMode
{
    /// <summary>
    /// Stops the current read when malformed input is found.
    /// </summary>
    Strict = 0,
    /// <summary>
    /// Accepts deterministic non-standard input and does not enforce parser limits.
    /// </summary>
    Relaxed = 1,
    /// <summary>
    /// Discards malformed rows and continues reading.
    /// </summary>
    SkipBadRow = 2,
    /// <summary>
    /// Stores malformed rows as rejects and continues reading.
    /// </summary>
    CollectRejects = 3
}
