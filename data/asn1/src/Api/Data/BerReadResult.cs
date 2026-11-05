namespace asn1;

/// <summary>
/// Contains TLV nodes, events, progress, and diagnostics from one read operation.
/// </summary>
public class BerReadResult
{
    /// <summary>
    /// Contains the read status.
    /// </summary>
    public AsnDecodeStatus Status;

    /// <summary>
    /// Contains the exact number of input bytes consumed.
    /// </summary>
    public int ConsumedCount;

    /// <summary>
    /// Contains the root TLV node when tree reading completed.
    /// </summary>
    public BerTlv? Tlv;

    /// <summary>
    /// Contains emitted TLV events in encoded order.
    /// </summary>
    public BerTlvEvent[] Events = [];

    /// <summary>
    /// Contains TLV diagnostics.
    /// </summary>
    public AsnDiagnostic[] Diagnostics = [];
}
