namespace ftp;

/// <summary>
/// Specifies behavior when a transfer destination already exists.
/// </summary>
public enum FtpCollisionPolicy
{
    /// <summary>
    /// Replaces the existing destination.
    /// </summary>
    Overwrite = 0,
    /// <summary>
    /// Leaves the existing destination unchanged.
    /// </summary>
    Skip = 1,
    /// <summary>
    /// Resumes from existing content when safe.
    /// </summary>
    Resume = 2,
    /// <summary>
    /// Appends to existing content.
    /// </summary>
    Append = 3,
    /// <summary>
    /// Fails without changing the existing destination.
    /// </summary>
    Fail = 4,
    /// <summary>
    /// Requests or creates a unique destination name.
    /// </summary>
    Unique = 5
}
