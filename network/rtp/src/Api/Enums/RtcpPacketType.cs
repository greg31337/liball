namespace rtp;

/// <summary>
/// Identifies an RTCP packet by its registered wire packet type.
/// </summary>
public enum RtcpPacketType
{
    /// <summary>The packet type is unsupported or not recognized.</summary>
    Unknown = 0,

    /// <summary>The historic full-intra-request packet type reserved by IANA.</summary>
    HistoricFullIntraRequest = 192,

    /// <summary>The historic negative-acknowledgement packet type reserved by IANA.</summary>
    HistoricNegativeAcknowledgement = 193,

    /// <summary>The packet maps SMPTE time codes.</summary>
    SmpteTimeCode = 194,

    /// <summary>The packet reports extended inter-arrival jitter.</summary>
    ExtendedInterArrivalJitter = 195,

    /// <summary>The packet is a sender report.</summary>
    SenderReport = 200,

    /// <summary>The packet is a receiver report.</summary>
    ReceiverReport = 201,

    /// <summary>The packet contains source descriptions.</summary>
    SourceDescription = 202,

    /// <summary>The packet is a goodbye indication.</summary>
    Goodbye = 203,

    /// <summary>The packet contains application-defined data.</summary>
    ApplicationDefined = 204,

    /// <summary>The packet contains transport-layer feedback.</summary>
    TransportFeedback = 205,

    /// <summary>The packet contains payload-specific feedback.</summary>
    PayloadFeedback = 206,

    /// <summary>The packet contains extended reports.</summary>
    ExtendedReport = 207,

    /// <summary>The packet contains audio-video bridging information.</summary>
    AudioVideoBridging = 208,

    /// <summary>The packet contains receiver summary information.</summary>
    ReceiverSummaryInformation = 209,

    /// <summary>The packet contains a port-mapping token message.</summary>
    PortMappingToken = 210,

    /// <summary>The packet contains inter-destination media synchronization settings.</summary>
    InterDestinationMediaSynchronization = 211,

    /// <summary>The packet identifies reporting-group sources.</summary>
    ReportingGroupSources = 212,

    /// <summary>The packet contains a splicing notification.</summary>
    SplicingNotification = 213
}
