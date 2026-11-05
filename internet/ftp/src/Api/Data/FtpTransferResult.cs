namespace ftp;

/// <summary>
/// Carries transfer completion, byte accounting, verification, and command results.
/// </summary>
public class FtpTransferResult
{
    /// <summary>
    /// The request that produced this result.
    /// </summary>
    public FtpTransferRequest Request;

    /// <summary>
    /// The final progress snapshot.
    /// </summary>
    public FtpTransferProgress Progress;

    /// <summary>
    /// The ordered control-channel command results.
    /// </summary>
    public FtpCommandResult[] Commands;

    /// <summary>
    /// Indicates that the transfer completed successfully.
    /// </summary>
    public bool Succeeded;

    /// <summary>
    /// Indicates that post-transfer verification was attempted.
    /// </summary>
    public bool VerificationAttempted;

    /// <summary>
    /// Indicates that all requested verification checks succeeded.
    /// </summary>
    public bool Verified;

    /// <summary>
    /// The verification mode that was applied.
    /// </summary>
    public FtpVerificationMode VerificationMode;

    /// <summary>
    /// Indicates whether <see cref="ActualSize"/> is available.
    /// </summary>
    public bool HasActualSize;

    /// <summary>
    /// The observed logical size when <see cref="HasActualSize"/> is true.
    /// </summary>
    public long ActualSize;

    /// <summary>
    /// The optional checksum algorithm used for verification.
    /// </summary>
    public string? ChecksumAlgorithm;

    /// <summary>
    /// The optional expected checksum.
    /// </summary>
    public string? ExpectedChecksum;

    /// <summary>
    /// The optional observed checksum.
    /// </summary>
    public string? ActualChecksum;

    /// <summary>
    /// The optional accepted or emitted restart marker.
    /// </summary>
    public string? RestartMarker;

    /// <summary>
    /// The optional unique remote name returned by STOU.
    /// </summary>
    public string? UniqueRemoteName;

    /// <summary>
    /// The optional local failure description.
    /// </summary>
    public string? FailureMessage;

    /// <summary>
    /// Creates an empty, non-successful, unverified transfer result.
    /// </summary>
    public FtpTransferResult()
    {
        Request = new FtpTransferRequest();
        Progress = new FtpTransferProgress();
        Commands = new FtpCommandResult[0];
        Succeeded = false;
        VerificationAttempted = false;
        Verified = false;
        VerificationMode = FtpVerificationMode.None;
        HasActualSize = false;
        ActualSize = 0;
        ChecksumAlgorithm = null;
        ExpectedChecksum = null;
        ActualChecksum = null;
        RestartMarker = null;
        UniqueRemoteName = null;
        FailureMessage = null;
    }
}

