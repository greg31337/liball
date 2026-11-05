namespace ftp;

/// <summary>
/// Carries a deterministic, non-mutating directory synchronization plan.
/// </summary>
public class FtpDirectoryPlan
{
    /// <summary>
    /// The request used to build the plan.
    /// </summary>
    public FtpDirectoryTransferRequest Request;

    /// <summary>
    /// Planned items in deterministic execution order.
    /// </summary>
    public FtpDirectoryItemResult[] Items;

    /// <summary>
    /// Indicates that planning completed within all configured bounds.
    /// </summary>
    public bool Complete;

    /// <summary>
    /// The optional planning failure description.
    /// </summary>
    public string? FailureMessage;

    /// <summary>
    /// Creates an empty, incomplete directory plan.
    /// </summary>
    public FtpDirectoryPlan()
    {
        Request = new FtpDirectoryTransferRequest();
        Items = new FtpDirectoryItemResult[0];
        Complete = false;
        FailureMessage = null;
    }
}

