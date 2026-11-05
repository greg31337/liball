namespace ftp;

/// <summary>
/// Configures one file or stream transfer.
/// </summary>
public class FtpTransferOptions
{
    /// <summary>
    /// The FTP representation type.
    /// </summary>
    public FtpTransferType Type;

    /// <summary>
    /// The text format used with ASCII or EBCDIC representations.
    /// </summary>
    public FtpTextFormat TextFormat;

    /// <summary>
    /// The FTP file structure.
    /// </summary>
    public FtpFileStructure Structure;

    /// <summary>
    /// The FTP transfer mode.
    /// </summary>
    public FtpTransferMode Mode;

    /// <summary>
    /// Indicates whether <see cref="LocalByteSize"/> is specified.
    /// </summary>
    public bool HasLocalByteSize;

    /// <summary>
    /// The TYPE L byte size when <see cref="HasLocalByteSize"/> is true.
    /// </summary>
    public int LocalByteSize;

    /// <summary>
    /// The destination collision policy.
    /// </summary>
    public FtpCollisionPolicy CollisionPolicy;

    /// <summary>
    /// The restart policy.
    /// </summary>
    public FtpResumePolicy ResumePolicy;

    /// <summary>
    /// The post-transfer verification mode.
    /// </summary>
    public FtpVerificationMode VerificationMode;

    /// <summary>
    /// Shared timeout and cancellation settings.
    /// </summary>
    public FtpTimeoutOptions Timeouts;

    /// <summary>
    /// Indicates that missing destination parent directories may be created.
    /// </summary>
    public bool CreateParentDirectories;

    /// <summary>
    /// Indicates that uploads use a temporary name followed by a rename commit.
    /// </summary>
    public bool AtomicUpload;

    /// <summary>
    /// Indicates that partial destination content is removed after failure.
    /// </summary>
    public bool CleanupPartialOnFailure;

    /// <summary>
    /// Indicates that source modification timestamps are preserved when supported.
    /// </summary>
    public bool PreserveModifiedTime;

    /// <summary>
    /// Indicates that control-channel keepalive commands may run during transfer.
    /// </summary>
    public bool EnableKeepAlive;

    /// <summary>
    /// The interval between transfer-time NOOP keepalives in milliseconds.
    /// </summary>
    public int KeepAliveIntervalMilliseconds;

    /// <summary>
    /// The bounded transfer buffer size in bytes.
    /// </summary>
    public int BufferBytes;

    /// <summary>
    /// The optional expected checksum algorithm.
    /// </summary>
    public string? ExpectedChecksumAlgorithm;

    /// <summary>
    /// The optional expected checksum text.
    /// </summary>
    public string? ExpectedChecksum;

    /// <summary>
    /// Creates a binary stream transfer with non-destructive defaults.
    /// </summary>
    public FtpTransferOptions()
    {
        Type = FtpTransferType.Image;
        TextFormat = FtpTextFormat.NonPrint;
        Structure = FtpFileStructure.File;
        Mode = FtpTransferMode.Stream;
        HasLocalByteSize = false;
        LocalByteSize = 0;
        CollisionPolicy = FtpCollisionPolicy.Fail;
        ResumePolicy = FtpResumePolicy.Disabled;
        VerificationMode = FtpVerificationMode.Size;
        Timeouts = new FtpTimeoutOptions();
        CreateParentDirectories = false;
        AtomicUpload = false;
        CleanupPartialOnFailure = true;
        PreserveModifiedTime = false;
        EnableKeepAlive = false;
        KeepAliveIntervalMilliseconds =
            FtpDefaults.TransferKeepAliveIntervalMilliseconds;
        BufferBytes = FtpDefaults.TransferBufferBytes;
        ExpectedChecksumAlgorithm = null;
        ExpectedChecksum = null;
    }
}
