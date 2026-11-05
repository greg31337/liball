namespace asn1;

/// <summary>
/// Contains all inputs for one schema-directed decode or canonicality operation.
/// </summary>
public class AsnDecodeRequest
{
    /// <summary>
    /// Contains the qualified schema type name.
    /// </summary>
    public string TypeName = "";

    /// <summary>
    /// Contains the encoded bytes.
    /// </summary>
    public byte[] Data = [];

    /// <summary>
    /// Contains the zero-based starting byte offset.
    /// </summary>
    public int Offset;

    /// <summary>
    /// Indicates whether Count limits the input region.
    /// </summary>
    public bool HasCount;

    /// <summary>
    /// Contains the input byte count when HasCount is true.
    /// </summary>
    public int Count;

    /// <summary>
    /// Contains codec options when supplied.
    /// </summary>
    public AsnCodecOptions? Options;
}
