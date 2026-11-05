using System.IO;

namespace ftp;

/// <summary>
/// Describes one local-to-server or server-to-local transfer.
/// </summary>
public class FtpTransferRequest
{
    /// <summary>
    /// The transfer direction.
    /// </summary>
    public FtpTransferDirection Direction;

    /// <summary>
    /// The remote FTP pathname.
    /// </summary>
    public string RemotePath;

    /// <summary>
    /// The optional local file pathname.
    /// </summary>
    public string? LocalPath;

    /// <summary>
    /// The optional caller-owned content stream.
    /// </summary>
    public Stream? Content;

    /// <summary>
    /// Indicates that a supplied content stream remains open after the operation.
    /// </summary>
    public bool LeaveContentOpen;

    /// <summary>
    /// Indicates whether <see cref="RestartOffset"/> is specified.
    /// </summary>
    public bool HasRestartOffset;

    /// <summary>
    /// The validated restart offset when <see cref="HasRestartOffset"/> is true.
    /// </summary>
    public long RestartOffset;

    /// <summary>
    /// Indicates whether <see cref="ExpectedSize"/> is specified.
    /// </summary>
    public bool HasExpectedSize;

    /// <summary>
    /// The expected logical size when <see cref="HasExpectedSize"/> is true.
    /// </summary>
    public long ExpectedSize;

    /// <summary>
    /// The transfer behavior and verification settings.
    /// </summary>
    public FtpTransferOptions Options;

    /// <summary>
    /// The optional observer for transfer lifecycle and progress notifications.
    /// </summary>
    public IFtpTransferObserver? Observer;

    /// <summary>
    /// Creates an unbound download request that leaves caller streams open.
    /// </summary>
    public FtpTransferRequest()
    {
        Direction = FtpTransferDirection.Download;
        RemotePath = "";
        LocalPath = null;
        Content = null;
        LeaveContentOpen = true;
        HasRestartOffset = false;
        RestartOffset = 0;
        HasExpectedSize = false;
        ExpectedSize = 0;
        Options = new FtpTransferOptions();
        Observer = null;
    }
}
