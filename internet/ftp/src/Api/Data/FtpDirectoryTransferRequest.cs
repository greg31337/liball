namespace ftp;

/// <summary>
/// Describes one recursive directory transfer or synchronization.
/// </summary>
public class FtpDirectoryTransferRequest
{
    /// <summary>
    /// The transfer direction.
    /// </summary>
    public FtpTransferDirection Direction;

    /// <summary>
    /// The local root path.
    /// </summary>
    public string LocalPath;

    /// <summary>
    /// The remote FTP root path.
    /// </summary>
    public string RemotePath;

    /// <summary>
    /// The recursive transfer and synchronization options.
    /// </summary>
    public FtpDirectoryTransferOptions Options;

    /// <summary>
    /// Creates an unbound directory download request.
    /// </summary>
    public FtpDirectoryTransferRequest()
    {
        Direction = FtpTransferDirection.Download;
        LocalPath = "";
        RemotePath = "";
        Options = new FtpDirectoryTransferOptions();
    }
}

