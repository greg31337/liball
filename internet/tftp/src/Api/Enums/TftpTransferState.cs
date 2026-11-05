namespace tftp;

/// <summary>
/// Identifies the current lifecycle state of a TFTP transfer.
/// </summary>
public enum TftpTransferState
{
    /// <summary>
    /// The transfer has been created but has not started.
    /// </summary>
    Created = 0,

    /// <summary>
    /// The remote host is being resolved.
    /// </summary>
    Resolving = 1,

    /// <summary>
    /// The initial read or write request is in progress.
    /// </summary>
    Requesting = 2,

    /// <summary>
    /// Transfer options are being negotiated.
    /// </summary>
    Negotiating = 3,

    /// <summary>
    /// File content is being sent.
    /// </summary>
    Sending = 4,

    /// <summary>
    /// File content is being received.
    /// </summary>
    Receiving = 5,

    /// <summary>
    /// The receiver is waiting briefly to acknowledge a repeated final block.
    /// </summary>
    Dallying = 6,

    /// <summary>
    /// The transfer completed successfully.
    /// </summary>
    Completed = 7,

    /// <summary>
    /// The transfer ended because of a failure.
    /// </summary>
    Failed = 8,

    /// <summary>
    /// The transfer ended because cancellation was requested.
    /// </summary>
    Cancelled = 9
}
