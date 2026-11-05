namespace barcode;

/// <summary>
/// Holds barcode text, raw bytes, interpretation metadata, and application-profile state.
/// </summary>
public class BarcodePayload
{
    /// <summary>
    /// The interpreted payload text when available.
    /// </summary>
    public string? Text;

    /// <summary>
    /// The original or requested payload bytes when available.
    /// </summary>
    public byte[]? RawBytes;

    /// <summary>
    /// The selected character-encoding name when available.
    /// </summary>
    public string? CharacterEncoding;

    /// <summary>
    /// The ECI assignments and byte ranges in payload order.
    /// </summary>
    public EciSegment[]? EciSegments;

    /// <summary>
    /// Indicates whether the payload uses GS1 data semantics.
    /// </summary>
    public bool IsGs1;

    /// <summary>
    /// The structured-append metadata when the payload is one segment of a sequence.
    /// </summary>
    public StructuredAppendInfo? StructuredAppend;
}
