using System.Collections.Generic;

namespace sdp;

/// <summary>Represents a typed ICE candidate attribute.</summary>
public class SdpIceCandidate
{
    /// <summary>Contains the originating generic attribute when available.</summary>
    public SdpAttribute? SourceAttribute = null;
    /// <summary>Contains the candidate foundation.</summary>
    public string Foundation = string.Empty;
    /// <summary>Contains the ICE component identifier.</summary>
    public int Component = 0;
    /// <summary>Identifies a recognized candidate transport protocol.</summary>
    public SdpIceTransportProtocol TransportProtocol = SdpIceTransportProtocol.Unknown;
    /// <summary>Contains the original candidate transport token.</summary>
    public string TransportToken = string.Empty;
    /// <summary>Contains the unsigned candidate priority.</summary>
    public uint Priority = 0;
    /// <summary>Contains the candidate connection address.</summary>
    public string Address = string.Empty;
    /// <summary>Identifies the recognized candidate-address form.</summary>
    public SdpAddressKind AddressKind = SdpAddressKind.Unknown;
    /// <summary>Contains the candidate port.</summary>
    public int Port = 0;
    /// <summary>Identifies a recognized candidate type.</summary>
    public SdpIceCandidateType CandidateType = SdpIceCandidateType.Unknown;
    /// <summary>Contains the original candidate-type token.</summary>
    public string CandidateTypeToken = string.Empty;
    /// <summary>Indicates whether a related address was present.</summary>
    public bool HasRelatedAddress = false;
    /// <summary>Contains the related address.</summary>
    public string RelatedAddress = string.Empty;
    /// <summary>Indicates whether a related port was present.</summary>
    public bool HasRelatedPort = false;
    /// <summary>Contains the related port.</summary>
    public int RelatedPort = 0;
    /// <summary>Identifies a recognized ICE TCP candidate type.</summary>
    public SdpIceTcpType TcpType = SdpIceTcpType.Unknown;
    /// <summary>Contains the original ICE TCP type token.</summary>
    public string TcpTypeToken = string.Empty;
    /// <summary>Contains extension name/value pairs in source order.</summary>
    public List<SdpIceCandidateExtension> Extensions = new List<SdpIceCandidateExtension>();
}
