namespace ftp;

/// <summary>
/// Specifies the framing or compression mode of a data transfer.
/// </summary>
public enum FtpTransferMode
{
    /// <summary>
    /// Uses stream mode.
    /// </summary>
    Stream = 0,
    /// <summary>
    /// Uses block mode.
    /// </summary>
    Block = 1,
    /// <summary>
    /// Uses the RFC 959 compressed mode.
    /// </summary>
    Compressed = 2,
    /// <summary>
    /// Uses the optional MODE Z Deflate extension.
    /// </summary>
    Deflate = 3
}
