namespace asn1;

/// <summary>
/// Contains encoded bytes, progress, and diagnostics from one TLV write operation.
/// </summary>
public class BerWriteResult
{
    /// <summary>
    /// Indicates whether writing completed successfully.
    /// </summary>
    public bool Success;

    /// <summary>
    /// Contains the encoded TLV bytes.
    /// </summary>
    public byte[] Data = [];

    /// <summary>
    /// Contains the exact number of bytes written.
    /// </summary>
    public int WrittenCount;

    /// <summary>
    /// Contains TLV writing diagnostics.
    /// </summary>
    public AsnDiagnostic[] Diagnostics = [];
}
