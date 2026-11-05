namespace asn1;

/// <summary>
/// Contains one chunk supplied to an incremental decoder.
/// </summary>
public class AsnIncrementalInput
{
    /// <summary>
    /// Contains the input chunk bytes.
    /// </summary>
    public byte[] Data = [];

    /// <summary>
    /// Contains the zero-based starting offset within Data.
    /// </summary>
    public int Offset;

    /// <summary>
    /// Indicates whether Count limits the chunk region.
    /// </summary>
    public bool HasCount;

    /// <summary>
    /// Contains the chunk byte count when HasCount is true.
    /// </summary>
    public int Count;

    /// <summary>
    /// Indicates whether this chunk ends the encoded input.
    /// </summary>
    public bool IsFinalBlock;
}
