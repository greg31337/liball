using System.Collections.Generic;

namespace sdp;

/// <summary>Represents a multicast source-filter attribute.</summary>
public class SdpSourceFilterAttribute
{
    /// <summary>Contains the originating generic attribute when available.</summary>
    public SdpAttribute? SourceAttribute = null;
    /// <summary>Contains the filter-mode token such as <c>incl</c> or <c>excl</c>.</summary>
    public string FilterMode = string.Empty;
    /// <summary>Contains the registered or extension network-type token.</summary>
    public string NetworkType = string.Empty;
    /// <summary>Contains the registered or extension address-type token.</summary>
    public string AddressType = string.Empty;
    /// <summary>Contains the multicast destination address.</summary>
    public string DestinationAddress = string.Empty;
    /// <summary>Identifies the recognized destination-address form.</summary>
    public SdpAddressKind AddressKind = SdpAddressKind.Unknown;
    /// <summary>Contains source addresses in source order.</summary>
    public List<string> SourceAddresses = new List<string>();
}
