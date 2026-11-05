namespace ftp;

/// <summary>
/// Identifies a structured FTP server or client event.
/// </summary>
public enum FtpEventKind
{
    /// <summary>
    /// Represents an event kind not understood by this library.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Reports server or session lifecycle activity.
    /// </summary>
    Lifecycle = 1,
    /// <summary>
    /// Reports control or data connection activity.
    /// </summary>
    Connection = 2,
    /// <summary>
    /// Reports a redacted authentication outcome.
    /// </summary>
    Authentication = 3,
    /// <summary>
    /// Reports receipt or transmission of a command.
    /// </summary>
    Command = 4,
    /// <summary>
    /// Reports receipt or transmission of a reply.
    /// </summary>
    Reply = 5,
    /// <summary>
    /// Reports an authorization decision.
    /// </summary>
    Authorization = 6,
    /// <summary>
    /// Reports a file-system operation.
    /// </summary>
    FileSystem = 7,
    /// <summary>
    /// Reports the start of a transfer.
    /// </summary>
    TransferStarted = 8,
    /// <summary>
    /// Reports transfer progress.
    /// </summary>
    TransferProgress = 9,
    /// <summary>
    /// Reports transfer completion.
    /// </summary>
    TransferCompleted = 10,
    /// <summary>
    /// Reports a TLS protection change or diagnostic.
    /// </summary>
    Tls = 11,
    /// <summary>
    /// Reports rejection by a security or resource policy.
    /// </summary>
    PolicyRejection = 12,
    /// <summary>
    /// Reports an unexpected isolated fault.
    /// </summary>
    Fault = 13
}
