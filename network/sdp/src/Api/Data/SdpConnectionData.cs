namespace sdp;

/// <summary>
/// Represents session-level or media-level SDP connection data.
/// </summary>
public class SdpConnectionData
{
    /// <summary>Contains the registered or extension network-type token.</summary>
    public string NetworkType = string.Empty;

    /// <summary>Contains the registered or extension address-type token.</summary>
    public string AddressType = string.Empty;

    /// <summary>Contains the connection address without multicast suffixes.</summary>
    public string Address = string.Empty;

    /// <summary>Identifies the recognized form of the connection address.</summary>
    public SdpAddressKind AddressKind = SdpAddressKind.Unknown;

    /// <summary>Indicates whether an IPv4 multicast time-to-live value was present.</summary>
    public bool HasTimeToLive = false;

    /// <summary>Contains the IPv4 multicast time-to-live value.</summary>
    public int TimeToLive = 0;

    /// <summary>Indicates whether an explicit multicast address count was present.</summary>
    public bool HasAddressCount = false;

    /// <summary>Contains the multicast address count, or one when no count was present.</summary>
    public int AddressCount = 1;

    /// <summary>Contains the original address token including multicast suffixes.</summary>
    public string RawAddress = string.Empty;

    /// <summary>Contains the original text following <c>c=</c>.</summary>
    public string RawText = string.Empty;

    /// <summary>Identifies the connection field's starting position in the input.</summary>
    public SdpSourceLocation Location = new SdpSourceLocation();
}
