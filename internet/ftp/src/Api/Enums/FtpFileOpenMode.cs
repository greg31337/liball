namespace ftp;

/// <summary>
/// Specifies how a virtual file is opened.
/// </summary>
public enum FtpFileOpenMode
{
    /// <summary>
    /// Opens an existing file for reading.
    /// </summary>
    Read = 0,
    /// <summary>
    /// Creates a new file and fails if it exists.
    /// </summary>
    CreateNew = 1,
    /// <summary>
    /// Creates or replaces a file.
    /// </summary>
    Overwrite = 2,
    /// <summary>
    /// Opens or creates a file and writes at its end.
    /// </summary>
    Append = 3,
    /// <summary>
    /// Opens an existing file for writing at a validated restart offset.
    /// </summary>
    Resume = 4
}
