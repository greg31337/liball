namespace docx;

/// <summary>
/// Specifies the compression mode used by DOCX operations.
/// </summary>
public enum DocxCompressionMode
{
    /// <summary>
    /// Uses the ZIP implementation's balanced compression setting.
    /// </summary>
    Optimal = 0,
    /// <summary>
    /// Favors write speed over package size.
    /// </summary>
    Fastest = 1,
    /// <summary>
    /// Stores entries without compression.
    /// </summary>
    NoCompression = 2
}
