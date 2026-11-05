namespace ftp;

/// <summary>
/// Identifies the lifecycle state of an FTP client or server session.
/// </summary>
public enum FtpSessionState
{
    /// <summary>
    /// No control connection exists.
    /// </summary>
    Disconnected = 0,
    /// <summary>
    /// A control connection exists and authentication has not completed.
    /// </summary>
    Connected = 1,
    /// <summary>
    /// A virtual host has been selected before authentication.
    /// </summary>
    HostSelected = 2,
    /// <summary>
    /// An authentication exchange is in progress.
    /// </summary>
    Authenticating = 3,
    /// <summary>
    /// Authentication has completed successfully.
    /// </summary>
    Authenticated = 4,
    /// <summary>
    /// A data transfer is in progress.
    /// </summary>
    Transferring = 5,
    /// <summary>
    /// The session is shutting down.
    /// </summary>
    Closing = 6,
    /// <summary>
    /// The session has completed shutdown.
    /// </summary>
    Closed = 7
}
