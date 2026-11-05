namespace rtp;

/// <summary>
/// Contains one RTCP source-description item.
/// </summary>
public class RtcpSourceDescriptionItem
{
    /// <summary>The source-description item type.</summary>
    public RtcpSdesItemType Type;

    /// <summary>The original wire type, including an unrecognized type value.</summary>
    public byte WireType;

    /// <summary>The item value bytes, excluding a private-item prefix.</summary>
    public byte[] Value = [];

    /// <summary>The prefix bytes of an RFC 3550 private item.</summary>
    public byte[] PrivatePrefix = [];

    internal RtcpSourceDescriptionItem Copy()
    {
        RtcpSourceDescriptionItem copy = new RtcpSourceDescriptionItem();
        copy.Type = Type;
        copy.WireType = WireType;
        copy.Value = (byte[])Value.Clone();
        copy.PrivatePrefix = (byte[])PrivatePrefix.Clone();
        return copy;
    }
}
