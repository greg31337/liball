namespace rtp;

/// <summary>
/// Contains the sender information carried by an RTCP sender report.
/// </summary>
public class RtcpSenderInformation
{
    /// <summary>The sender's 64-bit NTP timestamp.</summary>
    public ulong NtpTimestamp;

    /// <summary>The sender's corresponding RTP timestamp.</summary>
    public uint RtpTimestamp;

    /// <summary>The sender's cumulative RTP packet count.</summary>
    public uint PacketCount;

    /// <summary>The sender's cumulative media payload octet count.</summary>
    public uint OctetCount;

    internal RtcpSenderInformation Copy()
    {
        RtcpSenderInformation copy = new RtcpSenderInformation();
        copy.NtpTimestamp = NtpTimestamp;
        copy.RtpTimestamp = RtpTimestamp;
        copy.PacketCount = PacketCount;
        copy.OctetCount = OctetCount;
        return copy;
    }
}
