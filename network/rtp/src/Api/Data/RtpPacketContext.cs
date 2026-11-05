namespace rtp;

/// <summary>
/// Contains caller-supplied timing and transport context for an RTP packet.
/// </summary>
public class RtpPacketContext
{
    /// <summary>The monotonic arrival or send time in caller-defined ticks.</summary>
    public long ArrivalTimeTicks;

    /// <summary>The identity token for the transport path carrying the packet.</summary>
    public string TransportIdentity = "";

    /// <summary>Whether the packet was recovered by a repair mechanism.</summary>
    public bool IsRepaired;

    /// <summary>Whether the packet arrived after its release deadline.</summary>
    public bool IsLate;

    /// <summary>The media clock rate associated with the packet payload type.</summary>
    public int ClockRate;

    /// <summary>The packet arrival time converted to media-clock units.</summary>
    public long MediaClockTimestamp;

    internal RtpPacketContext Copy()
    {
        RtpPacketContext copy = new RtpPacketContext();
        copy.ArrivalTimeTicks = ArrivalTimeTicks;
        copy.TransportIdentity = TransportIdentity;
        copy.IsRepaired = IsRepaired;
        copy.IsLate = IsLate;
        copy.ClockRate = ClockRate;
        copy.MediaClockTimestamp = MediaClockTimestamp;
        return copy;
    }
}
