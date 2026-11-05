namespace ftp;

/// <summary>
/// Specifies how directory trees are synchronized.
/// </summary>
public enum FtpDirectorySyncMode
{
    /// <summary>
    /// Copies selected source entries without deleting destination entries.
    /// </summary>
    Copy = 0,
    /// <summary>
    /// Copies only entries that are new or changed.
    /// </summary>
    Update = 1,
    /// <summary>
    /// Makes the destination match the source, including configured deletions.
    /// </summary>
    Mirror = 2
}
