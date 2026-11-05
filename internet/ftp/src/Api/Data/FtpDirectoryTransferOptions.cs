namespace ftp;

/// <summary>
/// Configures recursive directory transfer and synchronization behavior.
/// </summary>
public class FtpDirectoryTransferOptions
{
    /// <summary>
    /// Options applied to each file transfer.
    /// </summary>
    public FtpTransferOptions Transfer;

    /// <summary>
    /// The directory synchronization mode.
    /// </summary>
    public FtpDirectorySyncMode SyncMode;

    /// <summary>
    /// The symbolic-link policy.
    /// </summary>
    public FtpSymlinkPolicy SymlinkPolicy;

    /// <summary>
    /// The behavior after an item failure.
    /// </summary>
    public FtpFailurePolicy FailurePolicy;

    /// <summary>
    /// The optional caller-supplied entry filter.
    /// </summary>
    public IFtpTransferFilter? Filter;

    /// <summary>
    /// Indicates that planning occurs without mutating either side.
    /// </summary>
    public bool DryRun;

    /// <summary>
    /// Indicates that destination entries absent from the source may be deleted.
    /// </summary>
    public bool DeleteExtraneous;

    /// <summary>
    /// Indicates that recursive destructive operations have been explicitly enabled.
    /// </summary>
    public bool AllowRecursiveDeletion;

    /// <summary>
    /// Indicates that empty source directories are created at the destination.
    /// </summary>
    public bool PreserveEmptyDirectories;

    /// <summary>
    /// The maximum recursive traversal depth.
    /// </summary>
    public int MaximumDepth;

    /// <summary>
    /// The maximum number of planned or processed entries.
    /// </summary>
    public int MaximumEntryCount;

    /// <summary>
    /// Creates a copy-only configuration that skips links and stops on failure.
    /// </summary>
    public FtpDirectoryTransferOptions()
    {
        Transfer = new FtpTransferOptions();
        SyncMode = FtpDirectorySyncMode.Copy;
        SymlinkPolicy = FtpSymlinkPolicy.Skip;
        FailurePolicy = FtpFailurePolicy.Stop;
        Filter = null;
        DryRun = false;
        DeleteExtraneous = false;
        AllowRecursiveDeletion = false;
        PreserveEmptyDirectories = true;
        MaximumDepth = 128;
        MaximumEntryCount = FtpDefaults.MaximumListingEntryCount;
    }
}
