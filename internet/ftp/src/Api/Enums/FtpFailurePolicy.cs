namespace ftp;

/// <summary>
/// Specifies handling of an item failure in a multi-item operation.
/// </summary>
public enum FtpFailurePolicy
{
    /// <summary>
    /// Stops after the first failed item.
    /// </summary>
    Stop = 0,
    /// <summary>
    /// Continues processing remaining items and records the failure.
    /// </summary>
    Continue = 1
}
