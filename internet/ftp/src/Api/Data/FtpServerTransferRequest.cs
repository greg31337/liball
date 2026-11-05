namespace ftp;

/// <summary>
/// Describes one direct server-to-server FTP transfer.
/// </summary>
public class FtpServerTransferRequest
{
    /// <summary>
    /// The source FTP client session.
    /// </summary>
    public IFtpClient? SourceClient;

    /// <summary>
    /// The target FTP client session.
    /// </summary>
    public IFtpClient? TargetClient;

    /// <summary>
    /// The source remote pathname.
    /// </summary>
    public string SourcePath;

    /// <summary>
    /// The target remote pathname.
    /// </summary>
    public string TargetPath;

    /// <summary>
    /// Indicates that target content is appended.
    /// </summary>
    public bool Append;

    /// <summary>
    /// Indicates that the target server chooses a unique name.
    /// </summary>
    public bool Unique;

    /// <summary>
    /// The transfer representation, mode, timeout, and verification options.
    /// </summary>
    public FtpTransferOptions Options;

    /// <summary>
    /// Creates an unbound server-to-server transfer request.
    /// </summary>
    public FtpServerTransferRequest()
    {
        SourceClient = null;
        TargetClient = null;
        SourcePath = "";
        TargetPath = "";
        Append = false;
        Unique = false;
        Options = new FtpTransferOptions();
    }
}

