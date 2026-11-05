namespace ftp;

/// <summary>
/// Configures one FTP or implicit-FTPS control listener.
/// </summary>
public class FtpListenerOptions
{
    /// <summary>
    /// The local endpoint to bind.
    /// </summary>
    public FtpEndpoint Endpoint;

    /// <summary>
    /// The listener security mode.
    /// </summary>
    public FtpSecurityMode SecurityMode;

    /// <summary>
    /// TLS settings used by this listener.
    /// </summary>
    public FtpTlsOptions Tls;

    /// <summary>
    /// TCP socket behavior applied to accepted control connections.
    /// </summary>
    public FtpSocketOptions ControlSocket;

    /// <summary>
    /// The bounded pending-connection backlog.
    /// </summary>
    public int Backlog;

    /// <summary>
    /// Indicates that an IPv6 listener accepts IPv4-mapped connections when supported.
    /// </summary>
    public bool DualStack;

    /// <summary>
    /// Indicates that credentials may be accepted before TLS protection is active.
    /// </summary>
    public bool AllowPlaintextCredentials;

    /// <summary>
    /// The optional address advertised for passive connections accepted by this listener.
    /// </summary>
    public string? PassiveAdvertisedAddress;

    /// <summary>
    /// Creates an explicit-TLS-required listener on the registered FTP control port.
    /// </summary>
    public FtpListenerOptions()
    {
        Endpoint = new FtpEndpoint();
        SecurityMode = FtpSecurityMode.ExplicitTlsRequired;
        Tls = new FtpTlsOptions();
        ControlSocket = new FtpSocketOptions();
        Backlog = 128;
        DualStack = true;
        AllowPlaintextCredentials = FtpDefaults.AllowPlaintextCredentials;
        PassiveAdvertisedAddress = null;
    }
}
