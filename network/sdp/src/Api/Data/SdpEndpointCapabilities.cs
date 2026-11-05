using System.Collections.Generic;

namespace sdp;

/// <summary>Describes endpoint-wide SDP negotiation capabilities.</summary>
public class SdpEndpointCapabilities
{
    /// <summary>Contains media capabilities in preference order.</summary>
    public List<SdpMediaCapability> Media = new List<SdpMediaCapability>();
    /// <summary>Contains supported RTP header extensions in preference order.</summary>
    public List<SdpHeaderExtension> HeaderExtensions = new List<SdpHeaderExtension>();
    /// <summary>Contains certificate fingerprints available for negotiation.</summary>
    public List<SdpFingerprintAttribute> Fingerprints = new List<SdpFingerprintAttribute>();
    /// <summary>Contains the default connection data used when media capabilities do not override it.</summary>
    public SdpConnectionData? Connection = null;
    /// <summary>Contains endpoint-wide ICE signaling capabilities when available.</summary>
    public SdpIceDescription? Ice = null;
    /// <summary>Contains endpoint-wide TLS or DTLS signaling capabilities when available.</summary>
    public SdpDtlsDescription? Dtls = null;
    /// <summary>Indicates support for ICE signaling.</summary>
    public bool SupportsIce = false;
    /// <summary>Indicates support for DTLS signaling.</summary>
    public bool SupportsDtls = false;
    /// <summary>Indicates support for BUNDLE grouping.</summary>
    public bool SupportsBundle = false;
    /// <summary>Indicates support for SCTP data channels.</summary>
    public bool SupportsDataChannels = false;
}
