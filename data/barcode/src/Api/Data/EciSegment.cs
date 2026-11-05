namespace barcode;

/// <summary>
/// Describes an Extended Channel Interpretation assignment over a byte range.
/// </summary>
public class EciSegment
{
    /// <summary>
    /// The ECI assignment number.
    /// </summary>
    public int AssignmentNumber;

    /// <summary>
    /// The zero-based offset of the first covered byte.
    /// </summary>
    public int ByteOffset;

    /// <summary>
    /// The number of covered bytes.
    /// </summary>
    public int ByteCount;
}
