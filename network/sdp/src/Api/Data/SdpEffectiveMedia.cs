using System.Collections.Generic;

namespace sdp;

/// <summary>Collects effective inherited and media-level state for one media description.</summary>
public class SdpEffectiveMedia
{
    /// <summary>Contains the zero-based media index.</summary>
    public int MediaIndex = -1;
    /// <summary>Contains the zero-based media index that owns the effective transport endpoint.</summary>
    public int TransportMediaIndex = -1;
    /// <summary>Contains the media identifier.</summary>
    public string MediaId = string.Empty;
    /// <summary>Contains the effective transport port.</summary>
    public int Port = 0;
    /// <summary>Indicates whether the effective transport endpoint declares an explicit port count.</summary>
    public bool HasPortCount = false;
    /// <summary>Contains the effective transport port count.</summary>
    public int PortCount = 1;
    /// <summary>Identifies the effective media direction.</summary>
    public SdpDirection Direction = SdpDirection.Unknown;
    /// <summary>Contains the effective connection data when present.</summary>
    public SdpConnectionData? Connection = null;
    /// <summary>Contains effective bandwidth fields.</summary>
    public List<SdpBandwidth> Bandwidths = new List<SdpBandwidth>();
    /// <summary>Contains effective media-language capabilities in preference order.</summary>
    public List<string> Languages = new List<string>();
    /// <summary>Contains effective SDP-description languages in source order.</summary>
    public List<string> SessionDescriptionLanguages = new List<string>();
    /// <summary>Contains projected codecs in format order.</summary>
    public List<SdpCodec> Codecs = new List<SdpCodec>();
    /// <summary>Contains the effective RTCP destination when present.</summary>
    public SdpRtcpAttribute? Rtcp = null;
    /// <summary>Contains effective RTCP extended-report attributes.</summary>
    public List<SdpRtcpExtendedReportAttribute> RtcpExtendedReports = new List<SdpRtcpExtendedReportAttribute>();
    /// <summary>Contains source attributes in source order.</summary>
    public List<SdpSourceAttribute> SourceAttributes = new List<SdpSourceAttribute>();
    /// <summary>Contains source groups in source order.</summary>
    public List<SdpSourceGroupAttribute> SourceGroups = new List<SdpSourceGroupAttribute>();
    /// <summary>Contains media-stream identifiers in source order.</summary>
    public List<SdpMediaStreamId> MediaStreamIds = new List<SdpMediaStreamId>();
    /// <summary>Contains multicast source filters in source order.</summary>
    public List<SdpSourceFilterAttribute> SourceFilters = new List<SdpSourceFilterAttribute>();
    /// <summary>Contains effective ICE signaling when present.</summary>
    public SdpIceDescription? Ice = null;
    /// <summary>Contains effective TLS or DTLS signaling when present.</summary>
    public SdpDtlsDescription? Dtls = null;
    /// <summary>Contains effective header-extension attributes.</summary>
    public List<SdpExtMapAttribute> HeaderExtensions = new List<SdpExtMapAttribute>();
    /// <summary>Contains RID attributes in source order.</summary>
    public List<SdpRidAttribute> Rids = new List<SdpRidAttribute>();
    /// <summary>Contains effective simulcast signaling when present.</summary>
    public SdpSimulcastAttribute? Simulcast = null;
    /// <summary>Contains image-constraint attributes in source order.</summary>
    public List<SdpImageAttribute> ImageAttributes = new List<SdpImageAttribute>();
    /// <summary>Contains timestamp reference clocks in source order.</summary>
    public List<SdpClockReferenceAttribute> ClockReferences = new List<SdpClockReferenceAttribute>();
    /// <summary>Contains media clocks in source order.</summary>
    public List<SdpMediaClockAttribute> MediaClocks = new List<SdpMediaClockAttribute>();
    /// <summary>Contains effective SCTP signaling when present.</summary>
    public SdpSctpDescription? Sctp = null;
    /// <summary>Contains data-channel mappings in source order.</summary>
    public List<SdpDataChannelAttribute> DataChannels = new List<SdpDataChannelAttribute>();
    /// <summary>Contains effective BFCP signaling when present.</summary>
    public SdpBfcpDescription? Bfcp = null;
    /// <summary>Contains resolved BUNDLE transport state when present.</summary>
    public SdpBundleTransport? BundleTransport = null;
}
