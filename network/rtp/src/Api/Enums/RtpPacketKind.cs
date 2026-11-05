namespace rtp;

/// <summary>
/// Identifies the protocol kind of a classified datagram.
/// </summary>
public enum RtpPacketKind
{
    /// <summary>The datagram cannot be safely classified.</summary>
    Unknown = 0,

    /// <summary>The datagram contains an RTP packet.</summary>
    Rtp = 1,

    /// <summary>The datagram contains an RTCP packet.</summary>
    Rtcp = 2
}
