namespace csv;

/// <summary>
/// Specifies which physical line endings terminate input records.
/// </summary>
public enum CsvNewLineMode
{
    /// <summary>
    /// Recognizes CRLF, LF, and CR record endings.
    /// </summary>
    Auto = 0,
    /// <summary>
    /// Recognizes CRLF record endings.
    /// </summary>
    Crlf = 1,
    /// <summary>
    /// Recognizes LF record endings.
    /// </summary>
    Lf = 2,
    /// <summary>
    /// Recognizes CR record endings.
    /// </summary>
    Cr = 3
}
