namespace ftp;

/// <summary>
/// Identifies the direction of a transfer.
/// </summary>
public enum FtpTransferDirection
{
    /// <summary>
    /// Transfers from an FTP server to the local caller.
    /// </summary>
    Download = 0,
    /// <summary>
    /// Transfers from the local caller to an FTP server.
    /// </summary>
    Upload = 1,
    /// <summary>
    /// Transfers directly between two FTP servers.
    /// </summary>
    ServerToServer = 2
}
