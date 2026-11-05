namespace sdp;

/// <summary>Configures offer-and-answer negotiation behavior.</summary>
public class SdpNegotiationOptions
{
    /// <summary>Identifies the local endpoint's negotiation role.</summary>
    public SdpNegotiationRole Role = SdpNegotiationRole.Unknown;
    /// <summary>Specifies how codec configurations are matched.</summary>
    public SdpCodecMatchMode CodecMatchMode = SdpCodecMatchMode.Strict;
    /// <summary>Contains local endpoint capabilities when available.</summary>
    public SdpEndpointCapabilities? LocalCapabilities = null;
    /// <summary>Indicates whether incompatible media may be rejected.</summary>
    public bool AllowMediaRejection = true;
    /// <summary>Indicates whether offered payload assignments should be preserved.</summary>
    public bool PreservePayloadTypes = true;
    /// <summary>Indicates whether local codec preference takes precedence over remote ordering.</summary>
    public bool PreferLocalCodecOrder = true;
    /// <summary>Indicates whether conflicting header-extension identifiers may be remapped.</summary>
    public bool AllowHeaderExtensionIdRemapping = true;
    /// <summary>Indicates whether an answer may accept only a subset of an offered BUNDLE group.</summary>
    public bool AllowBundleSubset = true;
    /// <summary>Indicates whether subsequent negotiation may replace ICE credentials.</summary>
    public bool AllowIceRestart = true;
    /// <summary>Indicates whether subsequent descriptions must preserve media-description order.</summary>
    public bool PreserveMediaOrder = true;
    /// <summary>Indicates whether subsequent descriptions must preserve existing media identifiers.</summary>
    public bool PreserveMediaIds = true;
    /// <summary>Indicates whether rejected media descriptions must retain their existing slots.</summary>
    public bool PreserveRejectedMedia = true;
    /// <summary>Indicates whether a changed local description increments the origin session version.</summary>
    public bool IncrementOriginSessionVersion = true;
    /// <summary>Indicates that ICE signaling is required.</summary>
    public bool RequireIce = false;
    /// <summary>Indicates that TLS or DTLS signaling is required.</summary>
    public bool RequireDtls = false;
    /// <summary>Indicates that RTP and RTCP multiplexing is required.</summary>
    public bool RequireRtcpMux = false;
    /// <summary>Indicates that BUNDLE negotiation is required.</summary>
    public bool RequireBundle = false;
    /// <summary>Contains the maximum number of retained negotiation diagnostics.</summary>
    public int MaximumDiagnosticCount = SdpLimits.DefaultMaximumDiagnosticCount;
}
