namespace asn1;

/// <summary>
/// Describes a schema-less BER TLV reader event.
/// </summary>
public class BerTlvEvent
{
    /// <summary>
    /// Contains the TLV event category.
    /// </summary>
    public AsnTlvEventKind Kind;

    /// <summary>
    /// Contains the event tag when the event represents a TLV.
    /// </summary>
    public AsnTag? Tag;

    /// <summary>
    /// Contains the zero-based encoded byte offset.
    /// </summary>
    public int Offset;

    /// <summary>
    /// Indicates whether Length contains a definite content length.
    /// </summary>
    public bool HasLength;

    /// <summary>
    /// Contains the definite content length when HasLength is true.
    /// </summary>
    public long Length;

    /// <summary>
    /// Contains primitive content bytes for primitive events.
    /// </summary>
    public byte[] Data = [];
}
