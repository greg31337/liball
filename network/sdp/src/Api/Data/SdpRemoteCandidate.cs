namespace sdp;

/// <summary>Represents one component and endpoint from <c>remote-candidates</c>.</summary>
public class SdpRemoteCandidate
{
    /// <summary>Contains the ICE component identifier.</summary>
    public int Component = 0;
    /// <summary>Contains the remote candidate address.</summary>
    public string Address = string.Empty;
    /// <summary>Identifies the recognized address form.</summary>
    public SdpAddressKind AddressKind = SdpAddressKind.Unknown;
    /// <summary>Contains the remote candidate port.</summary>
    public int Port = 0;
}
