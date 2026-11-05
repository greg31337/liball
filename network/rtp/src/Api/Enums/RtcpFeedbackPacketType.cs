namespace rtp;

/// <summary>
/// Identifies the RTCP packet type carrying a feedback message.
/// </summary>
public enum RtcpFeedbackPacketType
{
    /// <summary>The feedback packet type is unsupported or not recognized.</summary>
    Unknown = 0,

    /// <summary>The message is transport-layer feedback.</summary>
    TransportLayer = 205,

    /// <summary>The message is payload-specific feedback.</summary>
    PayloadSpecific = 206
}
