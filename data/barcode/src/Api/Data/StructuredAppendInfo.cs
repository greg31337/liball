namespace barcode;

/// <summary>
/// Holds sequence and file metadata for one structured-append segment.
/// </summary>
public class StructuredAppendInfo
{
    /// <summary>
    /// The symbology or profile carrying the segment.
    /// </summary>
    public BarcodeFormat Format;

    /// <summary>
    /// The zero-based segment index.
    /// </summary>
    public int Index;

    /// <summary>
    /// The total number of segments when known.
    /// </summary>
    public int Count;

    /// <summary>
    /// The file or sequence identifier.
    /// </summary>
    public string? FileIdentifier;

    /// <summary>
    /// Indicates whether this is the final segment.
    /// </summary>
    public bool IsLastSegment;
}
