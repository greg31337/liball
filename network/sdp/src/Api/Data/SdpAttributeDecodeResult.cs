using System.Collections.Generic;

namespace sdp;

/// <summary>
/// Contains the result of projecting a generic SDP attribute into typed data.
/// </summary>
public class SdpAttributeDecodeResult
{
    /// <summary>Indicates whether decoding completed without an error diagnostic.</summary>
    public bool Success = false;

    /// <summary>Indicates whether the attribute name has a supported typed projection.</summary>
    public bool Recognized = false;

    /// <summary>Contains the originating generic attribute without discarding opaque data.</summary>
    public SdpAttribute? SourceAttribute = null;

    /// <summary>Contains the typed projection when the attribute is recognized and valid.</summary>
    public object? TypedAttribute = null;

    /// <summary>Contains a common attribute projection when one was decoded.</summary>
    public SdpCommonAttribute? CommonAttribute = null;

    /// <summary>Contains an RTP payload mapping projection when one was decoded.</summary>
    public SdpRtpMapAttribute? RtpMapAttribute = null;

    /// <summary>Contains an RTP format-parameters projection when one was decoded.</summary>
    public SdpFormatParametersAttribute? FormatParametersAttribute = null;

    /// <summary>Contains an RTCP destination projection when one was decoded.</summary>
    public SdpRtcpAttribute? RtcpAttribute = null;

    /// <summary>Contains an RTCP feedback projection when one was decoded.</summary>
    public SdpRtcpFeedbackAttribute? RtcpFeedbackAttribute = null;

    /// <summary>Contains an RTCP extended-report projection when one was decoded.</summary>
    public SdpRtcpExtendedReportAttribute? RtcpExtendedReportAttribute = null;

    /// <summary>Contains a synchronization-source attribute projection when one was decoded.</summary>
    public SdpSourceAttribute? SynchronizationSourceAttribute = null;

    /// <summary>Contains a synchronization-source group projection when one was decoded.</summary>
    public SdpSourceGroupAttribute? SourceGroupAttribute = null;

    /// <summary>Contains a media-stream identifier projection when one was decoded.</summary>
    public SdpMediaStreamId? MediaStreamId = null;

    /// <summary>Contains an MSID semantic projection when one was decoded.</summary>
    public SdpMediaStreamSemanticAttribute? MediaStreamSemanticAttribute = null;

    /// <summary>Contains a multicast source-filter projection when one was decoded.</summary>
    public SdpSourceFilterAttribute? SourceFilterAttribute = null;

    /// <summary>Contains a timestamp reference-clock projection when one was decoded.</summary>
    public SdpClockReferenceAttribute? ClockReferenceAttribute = null;

    /// <summary>Contains a media-clock projection when one was decoded.</summary>
    public SdpMediaClockAttribute? MediaClockAttribute = null;

    /// <summary>Contains an image-constraint projection when one was decoded.</summary>
    public SdpImageAttribute? ImageAttribute = null;

    /// <summary>Contains a media-group projection when one was decoded.</summary>
    public SdpGroupAttribute? GroupAttribute = null;

    /// <summary>Contains a media identification projection when one was decoded.</summary>
    public SdpMediaIdAttribute? MediaIdAttribute = null;

    /// <summary>Contains an ICE username-fragment projection when one was decoded.</summary>
    public SdpIceUsernameFragmentAttribute? IceUsernameFragmentAttribute = null;

    /// <summary>Contains an ICE password projection when one was decoded.</summary>
    public SdpIcePasswordAttribute? IcePasswordAttribute = null;

    /// <summary>Contains an ICE options projection when one was decoded.</summary>
    public SdpIceOptionsAttribute? IceOptionsAttribute = null;

    /// <summary>Contains an ICE candidate projection when one was decoded.</summary>
    public SdpIceCandidate? IceCandidate = null;

    /// <summary>Contains an ICE remote-candidates projection when one was decoded.</summary>
    public SdpRemoteCandidatesAttribute? RemoteCandidatesAttribute = null;

    /// <summary>Contains a certificate-fingerprint projection when one was decoded.</summary>
    public SdpFingerprintAttribute? FingerprintAttribute = null;

    /// <summary>Contains a connection setup-role projection when one was decoded.</summary>
    public SdpSetupAttribute? SetupAttribute = null;

    /// <summary>Contains a connection-reuse projection when one was decoded.</summary>
    public SdpConnectionAttribute? ConnectionAttribute = null;

    /// <summary>Contains a TLS connection identifier projection when one was decoded.</summary>
    public SdpTlsIdAttribute? TlsIdAttribute = null;

    /// <summary>Contains a legacy security-description crypto projection when one was decoded.</summary>
    public SdpCryptoAttribute? CryptoAttribute = null;

    /// <summary>Contains an RTP header-extension mapping projection when one was decoded.</summary>
    public SdpExtMapAttribute? ExtMapAttribute = null;

    /// <summary>Contains an RTP stream restriction projection when one was decoded.</summary>
    public SdpRidAttribute? RidAttribute = null;

    /// <summary>Contains a simulcast stream-list projection when one was decoded.</summary>
    public SdpSimulcastAttribute? SimulcastAttribute = null;

    /// <summary>Contains an SCTP port projection when one was decoded.</summary>
    public SdpSctpPortAttribute? SctpPortAttribute = null;

    /// <summary>Contains an SCTP maximum-message-size projection when one was decoded.</summary>
    public SdpMaximumMessageSizeAttribute? MaximumMessageSizeAttribute = null;

    /// <summary>Contains a legacy SCTP mapping projection when one was decoded.</summary>
    public SdpSctpMapAttribute? SctpMapAttribute = null;

    /// <summary>Contains a data-channel mapping projection when one was decoded.</summary>
    public SdpDataChannelAttribute? DataChannelAttribute = null;

    /// <summary>Contains a data-channel subprotocol attribute projection when one was decoded.</summary>
    public SdpDataChannelSubprotocolAttribute? DataChannelSubprotocolAttribute = null;

    /// <summary>Contains retained diagnostics in deterministic order.</summary>
    public List<SdpDiagnostic> Diagnostics = new List<SdpDiagnostic>();
}
