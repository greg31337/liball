namespace tftp;

/// <summary>
/// Identifies the direction of a TFTP transfer relative to the server.
/// </summary>
public enum TftpTransferDirection
{
    /// <summary>
    /// Reads content from the server.
    /// </summary>
    Read = 0,

    /// <summary>
    /// Writes content to the server.
    /// </summary>
    Write = 1
}
