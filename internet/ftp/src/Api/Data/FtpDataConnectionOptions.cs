namespace ftp;

/// <summary>
/// Configures active and passive FTP data-connection behavior.
/// </summary>
public class FtpDataConnectionOptions
{
    /// <summary>
    /// The preferred data-connection strategy.
    /// </summary>
    public FtpDataConnectionMode Mode;

    /// <summary>
    /// TCP socket behavior applied to active and passive data connections.
    /// </summary>
    public FtpSocketOptions Socket;

    /// <summary>
    /// Indicates that EPSV is preferred for passive connections.
    /// </summary>
    public bool UseExtendedPassive;

    /// <summary>
    /// Indicates that EPSV ALL is sent before passive transfers.
    /// </summary>
    public bool SendExtendedPassiveAll;

    /// <summary>
    /// Indicates that PASV fallback is allowed for IPv4.
    /// </summary>
    public bool AllowPassiveFallback;

    /// <summary>
    /// Indicates that EPRT is preferred for active connections.
    /// </summary>
    public bool UseExtendedActive;

    /// <summary>
    /// Indicates that PORT fallback is allowed for IPv4.
    /// </summary>
    public bool AllowActiveFallback;

    /// <summary>
    /// Indicates that an address advertised by PASV is ignored.
    /// </summary>
    public bool IgnorePassiveAdvertisedAddress;

    /// <summary>
    /// Indicates that private/public PASV address mismatches are rejected when advertised addresses are honored.
    /// </summary>
    public bool RejectPassiveAddressMismatch;

    /// <summary>
    /// Indicates that a data peer must match the control peer.
    /// </summary>
    public bool RequireDataPeerMatch;

    /// <summary>
    /// Indicates that foreign data endpoints and FXP setup are permitted.
    /// </summary>
    public bool AllowForeignDataConnections;

    /// <summary>
    /// Indicates that active connections to privileged ports are rejected.
    /// </summary>
    public bool RejectPrivilegedActivePorts;

    /// <summary>
    /// The optional local endpoint used for active-mode listeners.
    /// </summary>
    public FtpEndpoint? ActiveBindEndpoint;

    /// <summary>
    /// The optional externally advertised active-mode address.
    /// </summary>
    public string? ActiveAdvertisedAddress;

    /// <summary>
    /// The first permitted active listener port, or zero for operating-system selection.
    /// </summary>
    public int ActivePortStart;

    /// <summary>
    /// The last permitted active listener port, or zero for operating-system selection.
    /// </summary>
    public int ActivePortEnd;

    /// <summary>
    /// The first port in the server passive-listener range.
    /// </summary>
    public int PassivePortStart;

    /// <summary>
    /// The last port in the server passive-listener range.
    /// </summary>
    public int PassivePortEnd;

    /// <summary>
    /// Creates passive-first data options with anti-bounce and anti-port-theft defaults.
    /// </summary>
    public FtpDataConnectionOptions()
    {
        Mode = FtpDataConnectionMode.Passive;
        Socket = new FtpSocketOptions();
        UseExtendedPassive = true;
        SendExtendedPassiveAll = false;
        AllowPassiveFallback = true;
        UseExtendedActive = true;
        AllowActiveFallback = true;
        IgnorePassiveAdvertisedAddress = FtpDefaults.IgnorePassiveAdvertisedAddress;
        RejectPassiveAddressMismatch = true;
        RequireDataPeerMatch = FtpDefaults.RequireDataPeerMatch;
        AllowForeignDataConnections = FtpDefaults.AllowForeignDataConnections;
        RejectPrivilegedActivePorts = true;
        ActiveBindEndpoint = null;
        ActiveAdvertisedAddress = null;
        ActivePortStart = 0;
        ActivePortEnd = 0;
        PassivePortStart = FtpDefaults.PassivePortStart;
        PassivePortEnd = FtpDefaults.PassivePortEnd;
    }
}
