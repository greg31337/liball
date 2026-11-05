namespace ftp;

/// <summary>
/// Defines lifecycle operations for an embeddable FTP server.
/// </summary>
public interface IFtpServer
{
    /// <summary>
    /// Starts the server using a validated snapshot of the supplied options.
    /// </summary>
    /// <param name="options">The listener, host, security, and resource options.</param>
    void StartServer(FtpServerOptions options);

    /// <summary>
    /// Stops the server and applies its configured graceful-stop deadline.
    /// </summary>
    void StopServer();

    /// <summary>
    /// Gets the current server lifecycle state.
    /// </summary>
    /// <returns>The current server state.</returns>
    FtpServerState GetServerState();
}

