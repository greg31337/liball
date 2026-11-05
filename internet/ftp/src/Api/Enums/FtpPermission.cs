namespace ftp;

/// <summary>
/// Identifies an MLSx permission granted for an entry.
/// </summary>
public enum FtpPermission
{
    /// <summary>
    /// Represents no permission.
    /// </summary>
    None = 0,
    /// <summary>
    /// Permits appending data to a file.
    /// </summary>
    Append = 1,
    /// <summary>
    /// Permits creating a new file.
    /// </summary>
    CreateFile = 2,
    /// <summary>
    /// Permits deleting a file.
    /// </summary>
    DeleteFile = 3,
    /// <summary>
    /// Permits entering a directory.
    /// </summary>
    EnterDirectory = 4,
    /// <summary>
    /// Permits renaming an entry.
    /// </summary>
    Rename = 5,
    /// <summary>
    /// Permits listing a directory.
    /// </summary>
    List = 6,
    /// <summary>
    /// Permits creating a directory.
    /// </summary>
    CreateDirectory = 7,
    /// <summary>
    /// Permits deleting a directory.
    /// </summary>
    DeleteDirectory = 8,
    /// <summary>
    /// Permits retrieving a file.
    /// </summary>
    Read = 9,
    /// <summary>
    /// Permits writing a file.
    /// </summary>
    Write = 10
}
