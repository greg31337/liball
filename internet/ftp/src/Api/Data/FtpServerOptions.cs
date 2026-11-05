namespace ftp;

/// <summary>
/// Configures an embeddable FTP server and its bounded resources.
/// </summary>
public class FtpServerOptions
{
    /// <summary>
    /// The control listeners started by the server.
    /// </summary>
    public FtpListenerOptions[] Listeners;

    /// <summary>
    /// The HOST-selectable virtual hosts.
    /// </summary>
    public FtpVirtualHostOptions[] VirtualHosts;

    /// <summary>
    /// Shared timeout and cancellation settings.
    /// </summary>
    public FtpTimeoutOptions Timeouts;

    /// <summary>
    /// Shared active and passive data-connection policy.
    /// </summary>
    public FtpDataConnectionOptions DataConnection;

    /// <summary>
    /// Default TLS settings used when a listener or host does not replace them.
    /// </summary>
    public FtpTlsOptions Tls;

    /// <summary>
    /// The ordered extension-handler chain used for custom server commands.
    /// </summary>
    public IFtpExtensionHandler[] ExtensionHandlers;

    /// <summary>
    /// The optional built-in fixed-user authenticator configuration.
    /// </summary>
    public FtpFixedUserOptions? FixedUsers;

    /// <summary>
    /// The optional default virtual-host name.
    /// </summary>
    public string? DefaultHostName;

    /// <summary>
    /// The maximum number of pending accepted connections.
    /// </summary>
    public int MaximumPendingConnections;

    /// <summary>
    /// The maximum number of simultaneous sessions.
    /// </summary>
    public int MaximumSessions;

    /// <summary>
    /// The maximum number of simultaneous sessions per remote address.
    /// </summary>
    public int MaximumSessionsPerAddress;

    /// <summary>
    /// The maximum number of simultaneous sessions per authenticated user.
    /// </summary>
    public int MaximumSessionsPerUser;

    /// <summary>
    /// The maximum number of simultaneous data transfers.
    /// </summary>
    public int MaximumSimultaneousTransfers;

    /// <summary>
    /// The maximum number of commands accepted per minute per session.
    /// </summary>
    public int MaximumCommandsPerMinute;

    /// <summary>
    /// The maximum malformed control lines accepted before disconnect.
    /// </summary>
    public int MaximumMalformedLines;

    /// <summary>
    /// The maximum authentication attempts accepted per session.
    /// </summary>
    public int MaximumAuthenticationAttempts;

    /// <summary>
    /// The maximum entries emitted by one directory listing.
    /// </summary>
    public int MaximumListingEntryCount;

    /// <summary>
    /// Indicates that foreign data endpoints and FXP are allowed.
    /// </summary>
    public bool AllowForeignDataConnections;

    /// <summary>
    /// Indicates that historic X-prefixed aliases are enabled.
    /// </summary>
    public bool EnableHistoricAliases;

    /// <summary>
    /// Indicates that optional compatibility extensions are enabled.
    /// </summary>
    public bool EnableCompatibilityExtensions;

    /// <summary>
    /// Indicates that PRET transfer preparation is required and advertised.
    /// </summary>
    public bool EnablePret;

    /// <summary>
    /// Indicates that CLNT client identification is accepted and advertised.
    /// </summary>
    public bool EnableClnt;

    /// <summary>
    /// Indicates that MFMT modification-time mutation is accepted and advertised.
    /// </summary>
    public bool EnableMfmt;

    /// <summary>
    /// Indicates that MODE Z Deflate transfers are enabled and advertised.
    /// </summary>
    public bool EnableModeZ;

    /// <summary>
    /// Indicates that the HASH command and OPTS HASH selection are enabled.
    /// </summary>
    public bool EnableHash;

    /// <summary>
    /// Indicates that the MD5 compatibility command is enabled.
    /// </summary>
    public bool EnableMd5;

    /// <summary>
    /// Indicates that the XMD5 compatibility command is enabled.
    /// </summary>
    public bool EnableXmd5;

    /// <summary>
    /// Indicates that the XSHA1 compatibility command is enabled.
    /// </summary>
    public bool EnableXsha1;

    /// <summary>
    /// Indicates that the XSHA256 compatibility command is enabled.
    /// </summary>
    public bool EnableXsha256;

    /// <summary>
    /// Indicates that the XSHA512 compatibility command is enabled.
    /// </summary>
    public bool EnableXsha512;

    /// <summary>
    /// Indicates that the XCRC compatibility command is enabled.
    /// </summary>
    public bool EnableXcrc;

    /// <summary>
    /// Indicates that event and transcript data redact secrets.
    /// </summary>
    public bool RedactSecrets;

    /// <summary>
    /// Creates an empty server configuration with bounded secure resource limits.
    /// </summary>
    public FtpServerOptions()
    {
        Listeners = new FtpListenerOptions[0];
        VirtualHosts = new FtpVirtualHostOptions[0];
        Timeouts = new FtpTimeoutOptions();
        DataConnection = new FtpDataConnectionOptions();
        Tls = new FtpTlsOptions();
        ExtensionHandlers = new IFtpExtensionHandler[0];
        FixedUsers = null;
        DefaultHostName = null;
        MaximumPendingConnections = 128;
        MaximumSessions = 256;
        MaximumSessionsPerAddress = 16;
        MaximumSessionsPerUser = 8;
        MaximumSimultaneousTransfers = 64;
        MaximumCommandsPerMinute = 600;
        MaximumMalformedLines = 5;
        MaximumAuthenticationAttempts = FtpDefaults.MaximumAuthenticationAttempts;
        MaximumListingEntryCount = FtpDefaults.MaximumListingEntryCount;
        AllowForeignDataConnections = FtpDefaults.AllowForeignDataConnections;
        EnableHistoricAliases = false;
        EnableCompatibilityExtensions = false;
        EnablePret = false;
        EnableClnt = false;
        EnableMfmt = false;
        EnableModeZ = false;
        EnableHash = false;
        EnableMd5 = false;
        EnableXmd5 = false;
        EnableXsha1 = false;
        EnableXsha256 = false;
        EnableXsha512 = false;
        EnableXcrc = false;
        RedactSecrets = true;
    }
}
