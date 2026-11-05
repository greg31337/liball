namespace barcode;

/// <summary>
/// Selects a PDF417 high-level compaction mode.
/// </summary>
public enum Pdf417CompactionMode
{
    /// <summary>
    /// Selects compaction modes automatically.
    /// </summary>
    Automatic = 0,

    /// <summary>
    /// Uses text compaction.
    /// </summary>
    Text = 1,

    /// <summary>
    /// Uses byte compaction.
    /// </summary>
    Byte = 2,

    /// <summary>
    /// Uses numeric compaction.
    /// </summary>
    Numeric = 3
}
