namespace diameter;

/// <summary>
/// Defines command codes used by the built-in Diameter applications.
/// </summary>
public static class DiameterCommandCodes
{
    /// <summary>The Capabilities-Exchange request and answer command code.</summary>
    public const uint CapabilitiesExchange = 257;

    /// <summary>The Re-Auth request and answer command code.</summary>
    public const uint ReAuth = 258;

    /// <summary>The Accounting request and answer command code.</summary>
    public const uint Accounting = 271;

    /// <summary>The Credit-Control request and answer command code.</summary>
    public const uint CreditControl = 272;

    /// <summary>The Abort-Session request and answer command code.</summary>
    public const uint AbortSession = 274;

    /// <summary>The Session-Termination request and answer command code.</summary>
    public const uint SessionTermination = 275;

    /// <summary>The Device-Watchdog request and answer command code.</summary>
    public const uint DeviceWatchdog = 280;

    /// <summary>The Disconnect-Peer request and answer command code.</summary>
    public const uint DisconnectPeer = 282;

    /// <summary>The Capabilities-Update request and answer command code.</summary>
    public const uint CapabilitiesUpdate = 328;
}
