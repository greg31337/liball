namespace ftp;

/// <summary>
/// Carries aggregate and per-item results for a recursive directory operation.
/// </summary>
public class FtpDirectoryTransferResult
{
    /// <summary>
    /// The request that produced this result.
    /// </summary>
    public FtpDirectoryTransferRequest Request;

    /// <summary>
    /// The plan that was executed or returned for dry-run mode.
    /// </summary>
    public FtpDirectoryPlan Plan;

    /// <summary>
    /// Per-item results in deterministic order.
    /// </summary>
    public FtpDirectoryItemResult[] Items;

    /// <summary>
    /// Indicates that the requested directory operation completed successfully.
    /// </summary>
    public bool Succeeded;

    /// <summary>
    /// The total number of considered items.
    /// </summary>
    public int TotalItems;

    /// <summary>
    /// The number of successful items.
    /// </summary>
    public int SucceededItems;

    /// <summary>
    /// The number of failed items.
    /// </summary>
    public int FailedItems;

    /// <summary>
    /// The number of skipped items.
    /// </summary>
    public int SkippedItems;

    /// <summary>
    /// The number of deleted destination items.
    /// </summary>
    public int DeletedItems;

    /// <summary>
    /// The aggregate logical bytes transferred.
    /// </summary>
    public long LogicalBytes;

    /// <summary>
    /// The optional aggregate failure description.
    /// </summary>
    public string? FailureMessage;

    /// <summary>
    /// Creates an empty, non-successful directory result.
    /// </summary>
    public FtpDirectoryTransferResult()
    {
        Request = new FtpDirectoryTransferRequest();
        Plan = new FtpDirectoryPlan();
        Items = new FtpDirectoryItemResult[0];
        Succeeded = false;
        TotalItems = 0;
        SucceededItems = 0;
        FailedItems = 0;
        SkippedItems = 0;
        DeletedItems = 0;
        LogicalBytes = 0;
        FailureMessage = null;
    }
}

