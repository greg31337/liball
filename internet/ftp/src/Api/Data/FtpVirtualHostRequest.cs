namespace ftp;

/// <summary>
/// Carries HOST, endpoint, and TLS context to a virtual-host resolver.
/// </summary>
public class FtpVirtualHostRequest
{
    /// <summary>
    /// The requested HOST name or address literal.
    /// </summary>
    public string HostName;

    /// <summary>
    /// The optional TLS SNI host name.
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
    /// Indicates that TLS was established implicitly before the greeting.
    /// </summary>
    public bool ImplicitTls;

    /// <summary>
    /// Creates an empty clear-channel host request.
    /// </summary>
    public FtpVirtualHostRequest()
    {
        HostName = "";
        ServerName = null;
        LocalEndpoint = null;
        RemoteEndpoint = null;
        ImplicitTls = false;
    }
}

