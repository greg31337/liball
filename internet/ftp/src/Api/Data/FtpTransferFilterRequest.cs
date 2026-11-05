namespace ftp;

/// <summary>
/// Carries one recursive transfer entry to a caller-supplied filter.
/// </summary>
public class FtpTransferFilterRequest
{
    /// <summary>
    /// The transfer direction.
    /// </summary>
    public FtpTransferDirection Direction;

    /// <summary>
    /// The entry under consideration.
    /// </summary>
    public FtpEntry Entry;

    /// <summary>
    /// The local item path.
    /// </summary>
    public string LocalPath;

    /// <summary>
    /// The remote item path.
    /// </summary>
    public string RemotePath;

    /// <summary>
    /// The zero-based traversal depth.
    /// </summary>
    public int Depth;

    /// <summary>
    /// Indicates that the planned action would delete a destination entry.
    /// </summary>
    public bool Deletion;

    /// <summary>
    /// Creates an empty download filter request.
    /// </summary>
    public FtpTransferFilterRequest()
    {
        Direction = FtpTransferDirection.Download;
        Entry = new FtpEntry();
        LocalPath = "";
        RemotePath = "";
        Depth = 0;
        Deletion = false;
    }
}

