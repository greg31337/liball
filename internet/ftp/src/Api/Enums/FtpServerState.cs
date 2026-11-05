namespace ftp;

/// <summary>
/// Identifies the lifecycle state of an FTP server.
/// </summary>
public enum FtpServerState
{
    /// <summary>
    /// The server is not accepting connections.
    /// </summary>
    Stopped = 0,
    /// <summary>
    /// The server is starting listeners.
    /// </summary>
    Starting = 1,
    /// <summary>
    /// The server is accepting connections.
    /// </summary>
    Running = 2,
    /// <summary>
    /// The server has stopped accepting connections and is draining sessions.
    /// </summary>
    Draining = 3,
    /// <summary>
    /// The server is closing listeners and sessions.
    /// </summary>
    Stopping = 4,
    /// <summary>
    /// The server stopped because of an unrecoverable host fault.
    /// </summary>
    Faulted = 5
}
