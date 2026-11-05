namespace rtp;

/// <summary>
/// Defines RTCP header fields, packet types, sizes, limits, and scheduling defaults.
/// </summary>
public static class RtcpProtocolConstants
{
    /// <summary>The RTCP protocol version.</summary>
    public const byte Version = 2;

    /// <summary>The size of the common RTCP header in bytes.</summary>
    public const int HeaderSize = 4;

    /// <summary>The number of bytes represented by one RTCP length word.</summary>
    public const int LengthWordSize = 4;

    /// <summary>The maximum value of the RTCP length field.</summary>
    public const int MaximumLengthField = 65535;

    /// <summary>The largest RTCP packet size representable by the length field.</summary>
    public const int MaximumPacketSize = 262144;

    /// <summary>The largest number of minimum-size packets in one compound packet.</summary>
    public const int MaximumCompoundPacketCount = 65536;

    /// <summary>The largest value of an RTCP count, subtype, or feedback-format field.</summary>
    public const int MaximumCount = 31;

    /// <summary>The maximum number of report blocks in one sender or receiver report.</summary>
    public const int MaximumReceptionReportCount = 31;

    /// <summary>The maximum number of source identifiers in one SDES or BYE packet.</summary>
    public const int MaximumSourceCount = 31;

    /// <summary>The maximum byte length of an SDES item value or BYE reason.</summary>
    public const int MaximumTextLength = 255;

    /// <summary>The maximum RTCP padding count representable by the final byte.</summary>
    public const int MaximumPaddingCount = 255;

    /// <summary>The size of an SSRC or CSRC identifier in bytes.</summary>
    public const int SourceIdentifierSize = 4;

    /// <summary>The size of sender information in bytes.</summary>
    public const int SenderInformationSize = 20;

    /// <summary>The size of a reception report block in bytes.</summary>
    public const int ReceptionReportSize = 24;

    /// <summary>The minimum size of a sender report in bytes.</summary>
    public const int SenderReportMinimumSize = 28;

    /// <summary>The minimum size of a receiver report in bytes.</summary>
    public const int ReceiverReportMinimumSize = 8;

    /// <summary>The size of an SDES item header in bytes.</summary>
    public const int SourceDescriptionItemHeaderSize = 2;

    /// <summary>The SDES item type that terminates a source-description chunk.</summary>
    public const byte SourceDescriptionEnd = 0;

    /// <summary>The size of an APP packet name in bytes.</summary>
    public const int ApplicationNameSize = 4;

    /// <summary>The minimum size of an APP packet in bytes.</summary>
    public const int ApplicationMinimumSize = 12;

    /// <summary>The minimum size of an RTPFB or PSFB packet in bytes.</summary>
    public const int FeedbackMinimumSize = 12;

    /// <summary>The minimum size of an XR packet in bytes.</summary>
    public const int ExtendedReportMinimumSize = 8;

    /// <summary>The size of an XR block header in bytes.</summary>
    public const int ExtendedReportBlockHeaderSize = 4;

    /// <summary>The minimum signed 24-bit cumulative-loss value.</summary>
    public const int CumulativeLossMinimum = -8388608;

    /// <summary>The maximum signed 24-bit cumulative-loss value.</summary>
    public const int CumulativeLossMaximum = 8388607;

    /// <summary>The maximum value of an eight-bit fraction-lost field.</summary>
    public const int FractionLostMaximum = 255;

    /// <summary>The historic full-intra-request packet type reserved by IANA.</summary>
    public const byte HistoricFullIntraRequestPacketType = 192;

    /// <summary>The historic negative-acknowledgement packet type reserved by IANA.</summary>
    public const byte HistoricNegativeAcknowledgementPacketType = 193;

    /// <summary>The SMPTE time-code mapping packet type.</summary>
    public const byte SmpteTimeCodePacketType = 194;

    /// <summary>The extended inter-arrival jitter report packet type.</summary>
    public const byte InterArrivalJitterPacketType = 195;

    /// <summary>The sender report packet type.</summary>
    public const byte SenderReportPacketType = 200;

    /// <summary>The receiver report packet type.</summary>
    public const byte ReceiverReportPacketType = 201;

    /// <summary>The source description packet type.</summary>
    public const byte SourceDescriptionPacketType = 202;

    /// <summary>The goodbye packet type.</summary>
    public const byte GoodbyePacketType = 203;

    /// <summary>The application-defined packet type.</summary>
    public const byte ApplicationDefinedPacketType = 204;

    /// <summary>The transport-layer feedback packet type.</summary>
    public const byte TransportFeedbackPacketType = 205;

    /// <summary>The payload-specific feedback packet type.</summary>
    public const byte PayloadFeedbackPacketType = 206;

    /// <summary>The extended report packet type.</summary>
    public const byte ExtendedReportPacketType = 207;

    /// <summary>The audio-video bridging packet type.</summary>
    public const byte AudioVideoBridgingPacketType = 208;

    /// <summary>The receiver summary information packet type.</summary>
    public const byte ReceiverSummaryInformationPacketType = 209;

    /// <summary>The port-mapping token packet type.</summary>
    public const byte PortMappingTokenPacketType = 210;

    /// <summary>The inter-destination media synchronization packet type.</summary>
    public const byte InterDestinationMediaSynchronizationPacketType = 211;

    /// <summary>The reporting-group reporting-sources packet type.</summary>
    public const byte ReportingGroupSourcesPacketType = 212;

    /// <summary>The splicing notification message packet type.</summary>
    public const byte SplicingNotificationPacketType = 213;

    /// <summary>The first packet type in the primary IANA assignment range.</summary>
    public const byte PrimaryPacketTypeMinimum = 200;

    /// <summary>The last packet type in the primary IANA assignment range.</summary>
    public const byte PrimaryPacketTypeMaximum = 223;

    /// <summary>The first RTCP packet type distinguishable by RFC 5761 multiplexing.</summary>
    public const byte MultiplexingPacketTypeMinimum = 192;

    /// <summary>The last RTCP packet type distinguishable by RFC 5761 multiplexing.</summary>
    public const byte MultiplexingPacketTypeMaximum = 223;

    /// <summary>The mask of the version field in the first RTCP header byte.</summary>
    public const byte VersionMask = 192;

    /// <summary>The mask of the padding flag in the first RTCP header byte.</summary>
    public const byte PaddingMask = 32;

    /// <summary>The mask of the count, subtype, or feedback-format field.</summary>
    public const byte CountMask = 31;

    /// <summary>The bit shift of the RTCP version field.</summary>
    public const int VersionShift = 6;

    /// <summary>The RFC 3550 recommended fraction of session bandwidth allocated to RTCP.</summary>
    public const double RecommendedBandwidthFraction = 0.05;

    /// <summary>The RFC 3550 fraction of RTCP bandwidth reserved for active senders.</summary>
    public const double SenderBandwidthFraction = 0.25;

    /// <summary>The RFC 3550 fraction of RTCP bandwidth reserved for receivers.</summary>
    public const double ReceiverBandwidthFraction = 0.75;

    /// <summary>The maximum RTCP bandwidth fraction used for optional SDES items.</summary>
    public const double OptionalSourceDescriptionBandwidthFraction = 0.20;

    /// <summary>The RFC 3550 minimum regular RTCP interval in seconds.</summary>
    public const double MinimumIntervalSeconds = 5.0;

    /// <summary>The RFC 3550 minimum initial RTCP interval in seconds.</summary>
    public const double InitialMinimumIntervalSeconds = 2.5;

    /// <summary>The minimum RFC 4585 feedback-retention interval in ticks.</summary>
    public const long MinimumFeedbackRetentionTicks = 20000000L;

    /// <summary>The RFC 3550 interval compensation factor.</summary>
    public const double IntervalCompensation = 1.21828;

    /// <summary>The numerator weight for a new RTCP packet-size sample.</summary>
    public const int AverageSizeSampleWeight = 1;

    /// <summary>The numerator weight for the previous average RTCP packet size.</summary>
    public const int AverageSizePreviousWeight = 15;

    /// <summary>The common denominator for RTCP packet-size averaging.</summary>
    public const int AverageSizeWeightDenominator = 16;

    /// <summary>The largest session membership for which BYE may be sent immediately.</summary>
    public const int GoodbyeImmediateMemberThreshold = 50;

    /// <summary>The number of report intervals used to expire inactive senders.</summary>
    public const int SenderTimeoutIntervalMultiplier = 2;

    /// <summary>The number of deterministic intervals used to expire inactive members.</summary>
    public const int MemberTimeoutIntervalMultiplier = 5;

    /// <summary>The number of compact-NTP delay units in one second.</summary>
    public const int CompactNtpUnitsPerSecond = 65536;

    /// <summary>The offset in seconds from the NTP epoch to the Unix epoch.</summary>
    public const ulong NtpToUnixEpochSeconds = 2208988800UL;
}
