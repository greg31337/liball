namespace asn1;

/// <summary>
/// Contains all inputs for one schema-less BER TLV read operation.
/// </summary>
public class BerReadRequest
{
    /// <summary>
    /// Contains BER, DER, or CER encoded bytes.
    /// </summary>
    public byte[] Data = [];

    /// <summary>
    /// Contains the zero-based starting offset.
    /// </summary>
    public int Offset;

    /// <summary>
    /// Indicates whether Count limits the input region.
    /// </summary>
    public bool HasCount;

    /// <summary>
    /// Contains the input count when HasCount is true.
    /// </summary>
    public int Count;

    /// <summary>
    /// Selects BER, DER, or CER rule validation.
    /// </summary>
    public AsnEncodingRules EncodingRules;

    /// <summary>
    /// Indicates whether exact encoded TLV bytes are retained.
    /// </summary>
    public bool PreserveEncodedData;
}
