namespace rtp;

/// <summary>
/// Contains an RTCP application-defined packet body.
/// </summary>
public class RtcpApplicationDefined
{
    /// <summary>The five-bit application-defined subtype.</summary>
    public byte Subtype;

    /// <summary>The SSRC or CSRC associated with the application data.</summary>
    public uint SourceIdentifier;

    /// <summary>The four ASCII name bytes.</summary>
    public byte[] Name = [];

    /// <summary>The opaque application-defined data bytes.</summary>
    public byte[] Data = [];

    internal RtcpApplicationDefined Copy()
    {
        RtcpApplicationDefined copy = new RtcpApplicationDefined();
        copy.Subtype = Subtype;
        copy.SourceIdentifier = SourceIdentifier;
        copy.Name = (byte[])Name.Clone();
        copy.Data = (byte[])Data.Clone();
        return copy;
    }
}
