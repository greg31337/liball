namespace sdp;

/// <summary>
/// Identifies the connection role of a TCP ICE candidate.
/// </summary>
public enum SdpIceTcpType
{
    /// <summary>The TCP candidate type is unknown or extension-defined.</summary>
    Unknown = 0,

    /// <summary>The endpoint actively opens the TCP connection.</summary>
    Active = 1,

    /// <summary>The endpoint passively listens for the TCP connection.</summary>
    Passive = 2,

    /// <summary>The endpoint simultaneously opens and listens for the TCP connection.</summary>
    SimultaneousOpen = 3
}
