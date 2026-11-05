namespace ftp;

/// <summary>
/// Hosts an embeddable FTP server using caller-supplied policy and storage providers.
/// </summary>
public class FtpServer : IFtpServer
{
    internal FtpServerRuntime Runtime;

    /// <summary>
    /// Creates a server using the required file-system, identity, policy, host, certificate, and event providers.
    /// </summary>
    /// <param name="fileSystem">The virtual file-system provider.</param>
    /// <param name="authenticator">The identity provider.</param>
    /// <param name="authorizer">The authorization policy provider.</param>
    /// <param name="hostResolver">The virtual-host provider.</param>
    /// <param name="certificateProvider">The server certificate provider.</param>
    /// <param name="eventSink">The structured event sink.</param>
    public FtpServer(
        IFtpFileSystem? fileSystem,
        IFtpAuthenticator? authenticator,
        IFtpAuthorizer? authorizer,
        IFtpHostResolver? hostResolver,
        IFtpCertificateProvider? certificateProvider,
        IFtpEventSink? eventSink)
    {
        Runtime = new FtpServerRuntime(
            fileSystem,
            authenticator,
            authorizer,
            hostResolver,
            certificateProvider,
            eventSink);
    }

    /// <summary>
    /// Starts the server using a validated snapshot of the supplied options.
    /// </summary>
    /// <param name="options">The listener, host, security, and resource options.</param>
    public void StartServer(FtpServerOptions options)
    {
        Runtime.StartServer(options);
    }

    /// <summary>
    /// Stops the server and applies its configured graceful-stop deadline.
    /// </summary>
    public void StopServer()
    {
        Runtime.StopServer();
    }

    /// <summary>
    /// Gets the current server lifecycle state.
    /// </summary>
    /// <returns>The current server state.</returns>
    public FtpServerState GetServerState()
    {
        return Runtime.GetServerState();
    }
}
