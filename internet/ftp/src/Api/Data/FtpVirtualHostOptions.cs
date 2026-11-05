namespace ftp;

/// <summary>
/// Configures one HOST-selected FTP virtual host.
/// </summary>
public class FtpVirtualHostOptions
{
    /// <summary>
    /// The case-insensitive DNS host name.
    /// </summary>
    public string HostName;

    /// <summary>
    /// The greeting text for this host.
    /// </summary>
    public string Greeting;

    /// <summary>
    /// The local or provider-defined root path.
    /// </summary>
    public string RootPath;

    /// <summary>
    /// TLS settings and certificate material for this host.
    /// </summary>
    public FtpTlsOptions Tls;

    /// <summary>
    /// The optional file-system provider that replaces the server provider for this host.
    /// </summary>
    public IFtpFileSystem? FileSystem;

    /// <summary>
    /// The optional authentication provider that replaces the server provider for this host.
    /// </summary>
    public IFtpAuthenticator? Authenticator;

    /// <summary>
    /// The optional authorization provider that replaces the server provider for this host.
    /// </summary>
    public IFtpAuthorizer? Authorizer;

    /// <summary>
    /// The optional certificate provider that replaces the server provider for this host.
    /// </summary>
    public IFtpCertificateProvider? CertificateProvider;

    /// <summary>
    /// The feature set advertised by this host.
    /// </summary>
    public FtpFeatureSet Features;

    /// <summary>
    /// The optional host-specific extension-handler chain, or null to use the server chain.
    /// </summary>
    public IFtpExtensionHandler[]? ExtensionHandlers;

    /// <summary>
    /// Indicates that anonymous authentication is permitted.
    /// </summary>
    public bool AllowAnonymous;

    /// <summary>
    /// Indicates that anonymous principals may perform write operations.
    /// </summary>
    public bool AllowAnonymousWrites;

    /// <summary>
    /// The maximum simultaneous sessions for this host.
    /// </summary>
    public int MaximumSessions;

    /// <summary>
    /// The maximum simultaneous sessions per authenticated user.
    /// </summary>
    public int MaximumSessionsPerUser;

    /// <summary>
    /// Creates a disabled, unnamed virtual-host configuration.
    /// </summary>
    public FtpVirtualHostOptions()
    {
        HostName = "";
        Greeting = "FTP service ready";
        RootPath = "";
        Tls = new FtpTlsOptions();
        FileSystem = null;
        Authenticator = null;
        Authorizer = null;
        CertificateProvider = null;
        Features = new FtpFeatureSet();
        ExtensionHandlers = null;
        AllowAnonymous = false;
        AllowAnonymousWrites = FtpDefaults.AllowAnonymousWrites;
        MaximumSessions = 256;
        MaximumSessionsPerUser = 8;
    }
}
