namespace ftp;

/// <summary>
/// Carries host and connection context to a server certificate provider.
/// </summary>
public class FtpCertificateRequest
{
    /// <summary>
    /// The selected FTP virtual-host name.
    /// </summary>
    public string HostName;

    /// <summary>
    /// The optional TLS SNI server name.
    /// </summary>
    public string? ServerName;

    /// <summary>
    /// The optional local listener endpoint.
    /// </summary>
    public FtpEndpoint? LocalEndpoint;

    /// <summary>
    /// The optional remote control-peer endpoint.
    /// </summary>
    public FtpEndpoint? RemoteEndpoint;

    /// <summary>
    /// Indicates that TLS is being established before the FTP greeting.
    /// </summary>
    public bool ImplicitTls;

    /// <summary>
    /// Indicates that a client certificate will be requested.
    /// </summary>
    public bool RequestClientCertificate;

    /// <summary>
    /// Creates an empty explicit-TLS certificate request.
    /// </summary>
    public FtpCertificateRequest()
    {
        HostName = "";
        ServerName = null;
        LocalEndpoint = null;
        RemoteEndpoint = null;
        ImplicitTls = false;
        RequestClientCertificate = false;
    }
}

