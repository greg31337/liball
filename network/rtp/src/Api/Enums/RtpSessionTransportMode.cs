namespace rtp;

/// <summary>
/// Identifies how an RTP session receives RTP and RTCP datagrams.
/// </summary>
public enum RtpSessionTransportMode
{
    /// <summary>RTP and RTCP use separate transport paths.</summary>
    Separate = 0,

    /// <summary>RTP and RTCP share one path using RFC 5761 multiplexing.</summary>
    Multiplexed = 1
}
