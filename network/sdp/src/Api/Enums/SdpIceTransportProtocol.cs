namespace sdp;

/// <summary>
/// Identifies a transport protocol used by an ICE candidate.
/// </summary>
public enum SdpIceTransportProtocol
{
    /// <summary>The transport protocol is unknown or extension-defined.</summary>
    Unknown = 0,

    /// <summary>The candidate uses the User Datagram Protocol.</summary>
    Udp = 1,

    /// <summary>The candidate uses the Transmission Control Protocol.</summary>
    Tcp = 2
}
