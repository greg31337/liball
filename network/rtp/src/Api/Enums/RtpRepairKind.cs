namespace rtp;

/// <summary>
/// Identifies the mechanism used to repair RTP packet loss.
/// </summary>
public enum RtpRepairKind
{
    /// <summary>The repair uses packet retransmission.</summary>
    Retransmission = 0,

    /// <summary>The repair uses redundant payload data.</summary>
    Redundancy = 1,

    /// <summary>The repair uses forward error correction.</summary>
    ForwardErrorCorrection = 2,

    /// <summary>The repair mechanism is application defined.</summary>
    ApplicationDefined = 3
}
