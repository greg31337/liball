namespace ftp;

/// <summary>
/// Specifies how the FTP control connection is secured.
/// </summary>
public enum FtpSecurityMode
{
    /// <summary>
    /// Uses an unencrypted FTP control connection.
    /// </summary>
    Plain = 0,
    /// <summary>
    /// Requires an explicit TLS upgrade before credentials are sent.
    /// </summary>
    ExplicitTlsRequired = 1,
    /// <summary>
    /// Attempts an explicit TLS upgrade and permits a configured plaintext fallback.
    /// </summary>
    ExplicitTlsOpportunistic = 2,
    /// <summary>
    /// Establishes TLS before FTP protocol messages are exchanged.
    /// </summary>
    ImplicitTls = 3
}
