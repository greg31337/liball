namespace barcode;

/// <summary>
/// Holds PDF417 dimensions, layout, compaction, error-correction, ECI, and Macro options.
/// </summary>
public class Pdf417Options
{
    /// <summary>
    /// The requested number of data columns, or zero when automatic.
    /// </summary>
    public int Columns;

    /// <summary>
    /// The requested number of rows, or zero when automatic.
    /// </summary>
    public int Rows;

    /// <summary>
    /// The requested standard or compact row layout.
    /// </summary>
    public Pdf417Variant Variant;

    /// <summary>
    /// The requested high-level compaction mode.
    /// </summary>
    public Pdf417CompactionMode CompactionMode;

    /// <summary>
    /// The requested error-correction level, or a negative value when automatic.
    /// </summary>
    public int ErrorCorrectionLevel;

    /// <summary>
    /// The requested ECI assignment number, or zero when no explicit assignment is requested.
    /// </summary>
    public int EciAssignmentNumber;

    /// <summary>
    /// The Macro PDF417 segment metadata when Macro mode is requested.
    /// </summary>
    public StructuredAppendInfo? Macro;

    /// <summary>
    /// The optional Macro PDF417 file name.
    /// </summary>
    public string? FileName;

    /// <summary>
    /// The optional Macro PDF417 timestamp.
    /// </summary>
    public long Timestamp;

    /// <summary>
    /// The optional Macro PDF417 sender.
    /// </summary>
    public string? Sender;

    /// <summary>
    /// The optional Macro PDF417 addressee.
    /// </summary>
    public string? Addressee;

    /// <summary>
    /// The optional Macro PDF417 file size in bytes.
    /// </summary>
    public long FileSize;

    /// <summary>
    /// The optional Macro PDF417 file checksum.
    /// </summary>
    public int FileChecksum;
}
