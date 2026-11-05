namespace ftp;

/// <summary>
/// Specifies handling of symbolic links during recursive operations.
/// </summary>
public enum FtpSymlinkPolicy
{
    /// <summary>
    /// Ignores symbolic links.
    /// </summary>
    Skip = 0,
    /// <summary>
    /// Traverses the symbolic-link target subject to containment and cycle checks.
    /// </summary>
    Follow = 1,
    /// <summary>
    /// Preserves symbolic links when supported.
    /// </summary>
    Preserve = 2,
    /// <summary>
    /// Fails when a symbolic link is encountered.
    /// </summary>
    Fail = 3
}
