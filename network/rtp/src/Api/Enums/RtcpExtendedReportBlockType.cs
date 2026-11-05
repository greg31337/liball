namespace rtp;

/// <summary>
/// Identifies an RFC 3611 extended-report block type.
/// </summary>
public enum RtcpExtendedReportBlockType
{
    /// <summary>The block type is unsupported or not recognized.</summary>
    Unknown = 0,

    /// <summary>The block reports loss using run-length encoding.</summary>
    LossRunLength = 1,

    /// <summary>The block reports duplicates using run-length encoding.</summary>
    DuplicateRunLength = 2,

    /// <summary>The block reports packet receipt times.</summary>
    PacketReceiptTimes = 3,

    /// <summary>The block reports a receiver reference time.</summary>
    ReceiverReferenceTime = 4,

    /// <summary>The block reports delays since receiver reports.</summary>
    DelaySinceLastReceiverReport = 5,

    /// <summary>The block reports summary statistics.</summary>
    StatisticsSummary = 6,

    /// <summary>The block reports voice-over-IP metrics.</summary>
    VoiceOverIpMetrics = 7
}
