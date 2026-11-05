namespace sdp;

/// <summary>
/// Identifies an Interactive Connectivity Establishment candidate type.
/// </summary>
public enum SdpIceCandidateType
{
    /// <summary>The candidate type is unknown or extension-defined.</summary>
    Unknown = 0,

    /// <summary>The candidate represents a local interface address.</summary>
    Host = 1,

    /// <summary>The candidate represents a server-reflexive address.</summary>
    ServerReflexive = 2,

    /// <summary>The candidate represents a peer-reflexive address.</summary>
    PeerReflexive = 3,

    /// <summary>The candidate represents a relayed address.</summary>
    Relayed = 4
}
