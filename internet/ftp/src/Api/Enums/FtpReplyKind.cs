namespace ftp;

/// <summary>
/// Classifies the functional group of an FTP reply.
/// </summary>
public enum FtpReplyKind
{
    /// <summary>
    /// Represents an unavailable or invalid functional group.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Relates to syntax or an unrecognized command.
    /// </summary>
    Syntax = 1,
    /// <summary>
    /// Relates to status or informational replies.
    /// </summary>
    Information = 2,
    /// <summary>
    /// Relates to control or data connections.
    /// </summary>
    Connections = 3,
    /// <summary>
    /// Relates to authentication or accounting.
    /// </summary>
    AuthenticationAndAccounting = 4,
    /// <summary>
    /// Uses the protocol's unspecified functional group.
    /// </summary>
    Unspecified = 5,
    /// <summary>
    /// Relates to file-system operations.
    /// </summary>
    FileSystem = 6
}
