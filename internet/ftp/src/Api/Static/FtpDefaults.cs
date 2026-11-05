namespace ftp;

/// <summary>
/// Defines bounded and security-conscious default values for FTP operations.
/// </summary>
public static class FtpDefaults
{
    /// <summary>
    /// The maximum encoded size of one control-channel line.
    /// </summary>
    public const int MaximumControlLineBytes = 8192;

    /// <summary>
    /// The maximum aggregate encoded size of one reply.
    /// </summary>
    public const int MaximumReplyBytes = 65536;

    /// <summary>
    /// The maximum number of lines accepted in one multiline reply.
    /// </summary>
    public const int MaximumReplyLines = 256;

    /// <summary>
    /// The maximum number of feature records retained from FEAT.
    /// </summary>
    public const int MaximumFeatureCount = 256;

    /// <summary>
    /// The maximum number of facts retained for one MLSx entry.
    /// </summary>
    public const int MaximumFactCount = 128;

    /// <summary>
    /// The maximum number of entries retained from one directory listing.
    /// </summary>
    public const int MaximumListingEntryCount = 100000;

    /// <summary>
    /// The default bounded transfer buffer size in bytes.
    /// </summary>
    public const int TransferBufferBytes = 65536;

    /// <summary>
    /// The default interval between transfer-time control-channel keepalives.
    /// </summary>
    public const int TransferKeepAliveIntervalMilliseconds = 30000;

    /// <summary>
    /// The default control-connection timeout in milliseconds.
    /// </summary>
    public const int ConnectTimeoutMilliseconds = 30000;

    /// <summary>
    /// The default control-channel read timeout in milliseconds.
    /// </summary>
    public const int ControlReadTimeoutMilliseconds = 30000;

    /// <summary>
    /// The default control-channel write timeout in milliseconds.
    /// </summary>
    public const int ControlWriteTimeoutMilliseconds = 30000;

    /// <summary>
    /// The default data-connection setup timeout in milliseconds.
    /// </summary>
    public const int DataConnectionTimeoutMilliseconds = 30000;

    /// <summary>
    /// The default TLS handshake timeout in milliseconds.
    /// </summary>
    public const int TlsHandshakeTimeoutMilliseconds = 30000;

    /// <summary>
    /// The default session idle timeout in milliseconds.
    /// </summary>
    public const int IdleTimeoutMilliseconds = 300000;

    /// <summary>
    /// The default graceful server stop deadline in milliseconds.
    /// </summary>
    public const int GracefulStopTimeoutMilliseconds = 30000;

    /// <summary>
    /// The default automatic retry count, preventing implicit replay.
    /// </summary>
    public const int RetryCount = 0;

    /// <summary>
    /// The default maximum number of authentication attempts per session.
    /// </summary>
    public const int MaximumAuthenticationAttempts = 5;

    /// <summary>
    /// The first port in the default dynamic passive-listener range.
    /// </summary>
    public const int PassivePortStart = 49152;

    /// <summary>
    /// The last port in the default dynamic passive-listener range.
    /// </summary>
    public const int PassivePortEnd = 65535;

    /// <summary>
    /// Indicates that PASV advertised addresses are ignored by default.
    /// </summary>
    public const bool IgnorePassiveAdvertisedAddress = true;

    /// <summary>
    /// Indicates that data peers must match the control peer by default.
    /// </summary>
    public const bool RequireDataPeerMatch = true;

    /// <summary>
    /// Indicates that foreign data endpoints and FXP are denied by default.
    /// </summary>
    public const bool AllowForeignDataConnections = false;

    /// <summary>
    /// Indicates that sending credentials over plaintext is denied by default.
    /// </summary>
    public const bool AllowPlaintextCredentials = false;

    /// <summary>
    /// Indicates that clearing a protected control channel is denied by default.
    /// </summary>
    public const bool AllowClearCommandChannel = false;

    /// <summary>
    /// Indicates that anonymous write access is denied by default.
    /// </summary>
    public const bool AllowAnonymousWrites = false;
}
