namespace ftp;

/// <summary>
/// Identifies the kind of an FTP directory entry.
/// </summary>
public enum FtpEntryType
{
    /// <summary>
    /// Represents an entry whose type is not known.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Represents a regular file.
    /// </summary>
    File = 1,
    /// <summary>
    /// Represents a directory.
    /// </summary>
    Directory = 2,
    /// <summary>
    /// Represents the current-directory pseudo-entry.
    /// </summary>
    CurrentDirectory = 3,
    /// <summary>
    /// Represents the parent-directory pseudo-entry.
    /// </summary>
    ParentDirectory = 4,
    /// <summary>
    /// Represents a symbolic link.
    /// </summary>
    SymbolicLink = 5
}
