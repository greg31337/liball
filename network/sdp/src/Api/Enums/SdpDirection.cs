namespace sdp;

/// <summary>
/// Identifies the direction of media relative to the SDP sender.
/// </summary>
public enum SdpDirection
{
    /// <summary>The direction is unknown or extension-defined.</summary>
    Unknown = 0,

    /// <summary>Media is sent and received.</summary>
    SendReceive = 1,

    /// <summary>Media is sent but not received.</summary>
    SendOnly = 2,

    /// <summary>Media is received but not sent.</summary>
    ReceiveOnly = 3,

    /// <summary>Media is neither sent nor received.</summary>
    Inactive = 4
}
