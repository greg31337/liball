namespace tftp;

/// <summary>
/// Identifies an error reported in a TFTP ERROR packet.
/// </summary>
public enum TftpErrorCode
{
    /// <summary>
    /// Indicates an undefined error whose details are carried by the message.
    /// </summary>
    Undefined = 0,

    /// <summary>
    /// Indicates that the requested file was not found.
    /// </summary>
    FileNotFound = 1,

    /// <summary>
    /// Indicates that access to the requested file was denied.
    /// </summary>
    AccessViolation = 2,

    /// <summary>
    /// Indicates that storage capacity or allocation was exhausted.
    /// </summary>
    StorageExhausted = 3,

    /// <summary>
    /// Indicates an illegal or unsupported TFTP operation.
    /// </summary>
    IllegalOperation = 4,

    /// <summary>
    /// Indicates that the packet used an unknown transfer identifier.
    /// </summary>
    UnknownTransferId = 5,

    /// <summary>
    /// Indicates that the destination file already exists.
    /// </summary>
    FileAlreadyExists = 6,

    /// <summary>
    /// Indicates that the specified user does not exist.
    /// </summary>
    NoSuchUser = 7,

    /// <summary>
    /// Indicates that option negotiation failed.
    /// </summary>
    OptionNegotiationFailed = 8
}
