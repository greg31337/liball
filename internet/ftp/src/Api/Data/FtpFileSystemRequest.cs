namespace ftp;

/// <summary>
/// Carries one decoded and authorized virtual file-system operation.
/// </summary>
public class FtpFileSystemRequest
{
    /// <summary>
    /// The principal performing the operation.
    /// </summary>
    public FtpPrincipal Principal;

    /// <summary>
    /// The normalized virtual path.
    /// </summary>
    public string Path;

    /// <summary>
    /// The optional normalized destination path.
    /// </summary>
    public string? DestinationPath;

    /// <summary>
    /// The requested file-open behavior.
    /// </summary>
    public FtpFileOpenMode OpenMode;

    /// <summary>
    /// The requested FTP file structure.
    /// </summary>
    public FtpFileStructure Structure;

    /// <summary>
    /// Indicates whether <see cref="Offset"/> is specified.
    /// </summary>
    public bool HasOffset;

    /// <summary>
    /// The validated byte or marker offset when <see cref="HasOffset"/> is true.
    /// </summary>
    public long Offset;

    /// <summary>
    /// Indicates whether <see cref="Length"/> is specified.
    /// </summary>
    public bool HasLength;

    /// <summary>
    /// The requested allocation or content length when <see cref="HasLength"/> is true.
    /// </summary>
    public long Length;

    /// <summary>
    /// The optional prefix used when creating a unique name.
    /// </summary>
    public string? UniquePrefix;

    /// <summary>
    /// The optional modification timestamp requested for a metadata mutation.
    /// </summary>
    public FtpTimestamp? ModificationTime;

    /// <summary>
    /// Indicates that directory creation or deletion applies recursively.
    /// </summary>
    public bool Recursive;

    /// <summary>
    /// Indicates whether <see cref="UnixMode"/> is specified.
    /// </summary>
    public bool HasUnixMode;

    /// <summary>
    /// The Unix-style permission bits when <see cref="HasUnixMode"/> is true.
    /// </summary>
    public int UnixMode;

    /// <summary>
    /// Creates a read request for an empty path and unauthenticated principal.
    /// </summary>
    public FtpFileSystemRequest()
    {
        Principal = new FtpPrincipal();
        Path = "";
        DestinationPath = null;
        OpenMode = FtpFileOpenMode.Read;
        Structure = FtpFileStructure.File;
        HasOffset = false;
        Offset = 0;
        HasLength = false;
        Length = 0;
        UniquePrefix = null;
        ModificationTime = null;
        Recursive = false;
        HasUnixMode = false;
        UnixMode = 0;
    }
}
