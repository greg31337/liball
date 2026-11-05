namespace ftp;

/// <summary>
/// Specifies whether a transfer may be resumed.
/// </summary>
public enum FtpResumePolicy
{
    /// <summary>
    /// Does not resume partial transfers.
    /// </summary>
    Disabled = 0,
    /// <summary>
    /// Resumes only when safety invariants can be established.
    /// </summary>
    IfSafe = 1,
    /// <summary>
    /// Fails unless the transfer can be resumed safely.
    /// </summary>
    Required = 2
}
