namespace ftp;

/// <summary>
/// Carries an exception-less virtual file-system operation result.
/// </summary>
public class FtpFileSystemResult
{
    /// <summary>
    /// Indicates that the operation completed successfully.
    /// </summary>
    public bool Succeeded;

    /// <summary>
    /// The optional resolved, created, or changed entry.
    /// </summary>
    public FtpEntry? Entry;

    /// <summary>
    /// Entries returned by a directory enumeration.
    /// </summary>
    public FtpEntry[] Entries;

    /// <summary>
    /// The optional created or normalized virtual path.
    /// </summary>
    public string? Path;

    /// <summary>
    /// The optional capacity or quota information.
    /// </summary>
    public FtpSpaceInfo? Space;

    /// <summary>
    /// Indicates whether <see cref="AllocatedBytes"/> is available.
    /// </summary>
    public bool HasAllocatedBytes;

    /// <summary>
    /// The allocated bytes when <see cref="HasAllocatedBytes"/> is true.
    /// </summary>
    public long AllocatedBytes;

    /// <summary>
    /// The optional public failure description.
    /// </summary>
    public string? FailureMessage;

    /// <summary>
    /// Creates an empty, unsuccessful file-system result.
    /// </summary>
    public FtpFileSystemResult()
    {
        Succeeded = false;
        Entry = null;
        Entries = new FtpEntry[0];
        Path = null;
        Space = null;
        HasAllocatedBytes = false;
        AllocatedBytes = 0;
        FailureMessage = null;
    }
}

