namespace ftp;

/// <summary>
/// Represents one planned or executed directory-transfer item.
/// </summary>
public class FtpDirectoryItemResult
{
    /// <summary>
    /// The local item path.
    /// </summary>
    public string LocalPath;

    /// <summary>
    /// The remote item path.
    /// </summary>
    public string RemotePath;

    /// <summary>
    /// The transfer direction.
    /// </summary>
    public FtpTransferDirection Direction;

    /// <summary>
    /// The item entry type.
    /// </summary>
    public FtpEntryType EntryType;

    /// <summary>
    /// Indicates that the item is part of the plan.
    /// </summary>
    public bool Planned;

    /// <summary>
    /// Indicates that an operation was attempted for the item.
    /// </summary>
    public bool Executed;

    /// <summary>
    /// Indicates that the attempted item operation succeeded.
    /// </summary>
    public bool Succeeded;

    /// <summary>
    /// Indicates that the item was intentionally skipped.
    /// </summary>
    public bool Skipped;

    /// <summary>
    /// Indicates that the item was deleted from the destination.
    /// </summary>
    public bool Deleted;

    /// <summary>
    /// The optional single-file transfer result.
    /// </summary>
    public FtpTransferResult? TransferResult;

    /// <summary>
    /// The optional filesystem command result, including any server authorization denial.
    /// </summary>
    public FtpCommandResult? CommandResult;

    /// <summary>
    /// The optional failure or skip description.
    /// </summary>
    public string? Message;

    /// <summary>
    /// Creates an unplanned, unexecuted directory item result.
    /// </summary>
    public FtpDirectoryItemResult()
    {
        LocalPath = "";
        RemotePath = "";
        Direction = FtpTransferDirection.Download;
        EntryType = FtpEntryType.Unknown;
        Planned = false;
        Executed = false;
        Succeeded = false;
        Skipped = false;
        Deleted = false;
        TransferResult = null;
        CommandResult = null;
        Message = null;
    }
}
