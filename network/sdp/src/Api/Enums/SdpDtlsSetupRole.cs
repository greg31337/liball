namespace sdp;

/// <summary>
/// Identifies a TLS or DTLS connection setup role.
/// </summary>
public enum SdpDtlsSetupRole
{
    /// <summary>The setup role is unknown or extension-defined.</summary>
    Unknown = 0,

    /// <summary>The endpoint initiates the connection.</summary>
    Active = 1,

    /// <summary>The endpoint accepts an incoming connection.</summary>
    Passive = 2,

    /// <summary>The endpoint allows the answerer to select active or passive.</summary>
    ActPass = 3,

    /// <summary>The endpoint does not establish a connection while the session is on hold.</summary>
    HoldConnection = 4
}
