namespace sdp;

/// <summary>
/// Represents an SDP origin field.
/// </summary>
public class SdpOrigin
{
    /// <summary>Contains the originating user's name.</summary>
    public string Username = string.Empty;

    /// <summary>Contains the lossless decimal session identifier.</summary>
    public string SessionId = string.Empty;

    /// <summary>Contains the lossless decimal session version.</summary>
    public string SessionVersion = string.Empty;

    /// <summary>Contains the registered or extension network-type token.</summary>
    public string NetworkType = string.Empty;

    /// <summary>Contains the registered or extension address-type token.</summary>
    public string AddressType = string.Empty;

    /// <summary>Contains the unicast address or externally interpreted address name.</summary>
    public string UnicastAddress = string.Empty;

    /// <summary>Identifies the recognized form of the unicast address.</summary>
    public SdpAddressKind AddressKind = SdpAddressKind.Unknown;

    /// <summary>Contains the original text following <c>o=</c>.</summary>
    public string RawText = string.Empty;

    /// <summary>Identifies the origin field's starting position in the input.</summary>
    public SdpSourceLocation Location = new SdpSourceLocation();
}
