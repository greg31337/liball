namespace tftp;

/// <summary>
/// Holds the result of committing or aborting a pending storage write.
/// </summary>
public class TftpStorageCommitResult
{
    /// <summary>
    /// Indicates whether the commit or abort operation succeeded.
    /// </summary>
    public bool Success = false;

    /// <summary>
    /// The protocol error code to report when <see cref="Success"/> is false.
    /// </summary>
    public TftpErrorCode ErrorCode = TftpErrorCode.Undefined;

    /// <summary>
    /// The error message to report when <see cref="Success"/> is false.
    /// </summary>
    public string ErrorMessage = "";
}
