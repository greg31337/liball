using System.Threading;

namespace tftp;

/// <summary>
/// Controls a TFTP server.
/// </summary>
public interface ITftpServer
{
    /// <summary>
    /// Runs the server until cancellation or a stop request ends execution.
    /// </summary>
    /// <param name="options">The listener and transfer settings to use.</param>
    /// <param name="cancellationToken">A token that requests server shutdown.</param>
    void Run(TftpServerOptions options, CancellationToken cancellationToken);

    /// <summary>
    /// Requests that the server stop using the specified shutdown behavior.
    /// </summary>
    /// <param name="mode">The shutdown behavior to use.</param>
    void Stop(TftpServerStopMode mode);

    /// <summary>
    /// Gets a snapshot of the current server status.
    /// </summary>
    /// <returns>The current server status snapshot.</returns>
    TftpServerStatus GetStatus();
}
