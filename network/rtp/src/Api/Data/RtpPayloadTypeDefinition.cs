namespace rtp;

/// <summary>
/// Contains a signaling-supplied RTP payload-type definition.
/// </summary>
public class RtpPayloadTypeDefinition
{
    /// <summary>The RTP payload type.</summary>
    public byte PayloadType;

    /// <summary>The RTP timestamp clock rate in ticks per second.</summary>
    public int ClockRate;

    /// <summary>The application media identity or encoding name.</summary>
    public string MediaIdentity = "";

    /// <summary>The number of media channels.</summary>
    public int ChannelCount;

    /// <summary>Whether this definition represents an RFC 4588 retransmission payload.</summary>
    public bool IsRetransmission;

    /// <summary>Whether an associated original payload type is present.</summary>
    public bool HasAssociatedPayloadType;

    /// <summary>The original payload type associated with an RTX payload type.</summary>
    public byte AssociatedPayloadType;

    internal RtpPayloadTypeDefinition Copy()
    {
        RtpPayloadTypeDefinition copy = new RtpPayloadTypeDefinition();
        copy.PayloadType = PayloadType;
        copy.ClockRate = ClockRate;
        copy.MediaIdentity = MediaIdentity;
        copy.ChannelCount = ChannelCount;
        copy.IsRetransmission = IsRetransmission;
        copy.HasAssociatedPayloadType = HasAssociatedPayloadType;
        copy.AssociatedPayloadType = AssociatedPayloadType;
        return copy;
    }
}
