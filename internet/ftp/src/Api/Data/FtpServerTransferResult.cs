namespace ftp;

/// <summary>
/// Carries paired-session results for a direct server-to-server transfer.
/// </summary>
public class FtpServerTransferResult
{
    /// <summary>
    /// The request that produced this result.
    /// </summary>
    public FtpServerTransferRequest Request;

    /// <summary>
    /// The ordered command results from the source session.
    /// </summary>
    public FtpCommandResult[] SourceCommands;

    /// <summary>
    /// The ordered command results from the target session.
    /// </summary>
    public FtpCommandResult[] TargetCommands;

    /// <summary>
    /// Indicates that both server sessions reached positive completion.
    /// </summary>
    public bool Succeeded;

    /// <summary>
    /// Indicates whether <see cref="LogicalBytes"/> is available.
    /// </summary>
    public bool HasLogicalBytes;

    /// <summary>
    /// The transferred logical bytes when <see cref="HasLogicalBytes"/> is true.
    /// </summary>
    public long LogicalBytes;

    /// <summary>
    /// The optional unique target name returned by STOU.
    /// </summary>
    public string? UniqueTargetName;

    /// <summary>
    /// The optional paired-operation failure description.
    /// </summary>
    public string? FailureMessage;

    /// <summary>
    /// Creates an empty, non-successful server-to-server result.
    /// </summary>
    public FtpServerTransferResult()
    {
        Request = new FtpServerTransferRequest();
        SourceCommands = new FtpCommandResult[0];
        TargetCommands = new FtpCommandResult[0];
        Succeeded = false;
        HasLogicalBytes = false;
        LogicalBytes = 0;
        UniqueTargetName = null;
        FailureMessage = null;
    }
}

