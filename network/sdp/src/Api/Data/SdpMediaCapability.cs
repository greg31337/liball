using System.Collections.Generic;

namespace sdp;

/// <summary>Describes local capabilities for one media kind.</summary>
public class SdpMediaCapability
{
    /// <summary>Identifies the recognized media kind.</summary>
    public SdpMediaKind MediaKind = SdpMediaKind.Unknown;
    /// <summary>Contains the registered or extension media-type token.</summary>
    public string MediaType = string.Empty;
    /// <summary>Contains the preferred media identifier, or an empty value when one should be allocated.</summary>
    public string MediaId = string.Empty;
    /// <summary>Contains the preferred transport port.</summary>
    public int Port = 0;
    /// <summary>Indicates whether an explicit port count is required.</summary>
    public bool HasPortCount = false;
    /// <summary>Contains the preferred port count.</summary>
    public int PortCount = 1;
    /// <summary>Contains supported transport protocols in preference order.</summary>
    public List<string> Protocols = new List<string>();
    /// <summary>Contains explicitly supported non-RTP media format tokens in preference order.</summary>
    public List<string> Formats = new List<string>();
    /// <summary>Contains supported codecs in preference order.</summary>
    public List<SdpCodecCapability> Codecs = new List<SdpCodecCapability>();
    /// <summary>Contains the supported media direction.</summary>
    public SdpDirection Direction = SdpDirection.SendReceive;
    /// <summary>Contains media-specific connection data when it overrides the endpoint default.</summary>
    public SdpConnectionData? Connection = null;
    /// <summary>Contains media-specific ICE capabilities when they override the endpoint default.</summary>
    public SdpIceDescription? Ice = null;
    /// <summary>Contains media-specific TLS or DTLS capabilities when they override the endpoint default.</summary>
    public SdpDtlsDescription? Dtls = null;
    /// <summary>Contains supported RTP header extensions in preference order.</summary>
    public List<SdpHeaderExtension> HeaderExtensions = new List<SdpHeaderExtension>();
    /// <summary>Contains media-stream identifiers to advertise in source order.</summary>
    public List<SdpMediaStreamId> MediaStreamIds = new List<SdpMediaStreamId>();
    /// <summary>Contains synchronization-source attributes to advertise in source order.</summary>
    public List<SdpSourceAttribute> SourceAttributes = new List<SdpSourceAttribute>();
    /// <summary>Contains synchronization-source groups to advertise in source order.</summary>
    public List<SdpSourceGroupAttribute> SourceGroups = new List<SdpSourceGroupAttribute>();
    /// <summary>Contains supported RTP stream encodings in preference order.</summary>
    public List<SdpRidAttribute> Rids = new List<SdpRidAttribute>();
    /// <summary>Contains supported simulcast signaling when available.</summary>
    public SdpSimulcastAttribute? Simulcast = null;
    /// <summary>Contains supported image constraints in preference order.</summary>
    public List<SdpImageAttribute> ImageAttributes = new List<SdpImageAttribute>();
    /// <summary>Contains supported SCTP signaling for application media when available.</summary>
    public SdpSctpDescription? Sctp = null;
    /// <summary>Contains supported data-channel mappings in preference order.</summary>
    public List<SdpDataChannelAttribute> DataChannels = new List<SdpDataChannelAttribute>();
    /// <summary>Indicates support for RTP and RTCP multiplexing.</summary>
    public bool SupportsRtcpMux = false;
    /// <summary>Indicates support for requiring RTP and RTCP multiplexing.</summary>
    public bool SupportsRtcpMuxOnly = false;
    /// <summary>Indicates support for reduced-size RTCP.</summary>
    public bool SupportsReducedSizeRtcp = false;
    /// <summary>Indicates that this media capability may be offered only as part of a BUNDLE group.</summary>
    public bool BundleOnly = false;
    /// <summary>Contains the lower-is-more-preferred media ordering value.</summary>
    public int Preference = 0;
}
