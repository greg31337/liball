namespace rtp;

/// <summary>
/// Defines RFC 3611 extended-report block type values.
/// </summary>
public static class RtcpExtendedReportBlockTypes
{
    /// <summary>The block type for loss run-length reports.</summary>
    public const byte LossRunLength = 1;

    /// <summary>The block type for duplicate run-length reports.</summary>
    public const byte DuplicateRunLength = 2;

    /// <summary>The block type for packet receipt-time reports.</summary>
    public const byte PacketReceiptTimes = 3;

    /// <summary>The block type for receiver reference-time reports.</summary>
    public const byte ReceiverReferenceTime = 4;

    /// <summary>The block type for delay-since-last-receiver-report reports.</summary>
    public const byte DelaySinceLastReceiverReport = 5;

    /// <summary>The block type for statistics-summary reports.</summary>
    public const byte StatisticsSummary = 6;

    /// <summary>The block type for voice-over-IP metrics reports.</summary>
    public const byte VoiceOverIpMetrics = 7;

    /// <summary>The block type reserved for future extensions.</summary>
    public const byte Extension = 255;
}
