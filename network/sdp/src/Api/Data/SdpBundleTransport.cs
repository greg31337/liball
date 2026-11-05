using System.Collections.Generic;

namespace sdp;

/// <summary>Collects transport-category state resolved for a BUNDLE group.</summary>
public class SdpBundleTransport
{
    /// <summary>Contains the tagged media identifier.</summary>
    public string TaggedMediaId = string.Empty;
    /// <summary>Contains the effective connection data when present.</summary>
    public SdpConnectionData? Connection = null;
    /// <summary>Contains effective ICE signaling when present.</summary>
    public SdpIceDescription? Ice = null;
    /// <summary>Contains effective TLS or DTLS signaling when present.</summary>
    public SdpDtlsDescription? Dtls = null;
    /// <summary>Contains effective header-extension mappings.</summary>
    public List<SdpExtMapAttribute> HeaderExtensions = new List<SdpExtMapAttribute>();
    /// <summary>Contains effective IDENTICAL and TRANSPORT attributes from the tagged media section.</summary>
    public List<SdpAttribute> SharedAttributes = new List<SdpAttribute>();
    /// <summary>Contains NORMAL attributes belonging to the selected media section.</summary>
    public List<SdpAttribute> MediaAttributes = new List<SdpAttribute>();
    /// <summary>Contains selected NORMAL bandwidth values and group-aggregated SUM bandwidth values.</summary>
    public List<SdpBandwidth> Bandwidths = new List<SdpBandwidth>();
    /// <summary>Indicates whether RTP and RTCP multiplexing is active.</summary>
    public bool RtcpMux = false;
}
