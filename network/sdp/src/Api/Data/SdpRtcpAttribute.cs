namespace sdp;

/// <summary>Represents a typed RTCP destination attribute.</summary>
public class SdpRtcpAttribute
{
    /// <summary>Contains the originating generic attribute when available.</summary>
    public SdpAttribute? SourceAttribute = null;
    /// <summary>Contains the RTCP port.</summary>
    public int Port = 0;
    /// <summary>Indicates whether a network and address triple was present.</summary>
    public bool HasNetworkAddress = false;
    /// <summary>Contains the registered or extension network-type token.</summary>
    public string NetworkType = string.Empty;
    /// <summary>Contains the registered or extension address-type token.</summary>
    public string AddressType = string.Empty;
    /// <summary>Contains the RTCP connection address.</summary>
    public string Address = string.Empty;
    /// <summary>Identifies the recognized address form.</summary>
    public SdpAddressKind AddressKind = SdpAddressKind.Unknown;
}
