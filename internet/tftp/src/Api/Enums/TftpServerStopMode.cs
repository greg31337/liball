namespace tftp;

/// <summary>
/// Identifies how a running TFTP server should stop.
/// </summary>
public enum TftpServerStopMode
{
    /// <summary>
    /// Stops accepting requests and allows active transfers to finish.
    /// </summary>
    Graceful = 0,

    /// <summary>
    /// Stops accepting requests and cancels active transfers.
    /// </summary>
    Immediate = 1
}
