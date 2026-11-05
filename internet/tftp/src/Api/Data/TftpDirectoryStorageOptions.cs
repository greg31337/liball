namespace tftp;

/// <summary>
/// Holds root, access, and path-safety settings for directory-backed storage.
/// </summary>
public class TftpDirectoryStorageOptions
{
    /// <summary>
    /// The canonical storage root or a path that can be resolved to it.
    /// </summary>
    public string RootPath = "";

    /// <summary>
    /// Indicates whether existing content may be read.
    /// </summary>
    public bool AllowRead = true;

    /// <summary>
    /// Indicates whether content may be written.
    /// </summary>
    public bool AllowWrite = false;

    /// <summary>
    /// Indicates whether new files may be created by writes.
    /// </summary>
    public bool AllowCreate = false;

    /// <summary>
    /// Indicates whether existing files may be replaced by writes.
    /// </summary>
    public bool AllowOverwrite = false;

    /// <summary>
    /// Indicates whether requests may address descendants of the storage root.
    /// </summary>
    public bool AllowSubdirectories = true;

    /// <summary>
    /// Indicates whether symbolic links and reparse points must be rejected.
    /// </summary>
    public bool RejectSymbolicLinks = true;
}
