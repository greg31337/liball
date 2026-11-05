namespace xml;

/// <summary>
/// Specifies the disposition of an XSLT result destination.
/// </summary>
public enum XsltResultDisposition
{
    /// <summary>
    /// Leaves the destination pending.
    /// </summary>
    Pending = 0,
    /// <summary>
    /// Commits the destination.
    /// </summary>
    Commit = 1,
    /// <summary>
    /// Rolls back the destination.
    /// </summary>
    Rollback = 2,
    /// <summary>
    /// Rejects the destination.
    /// </summary>
    Reject = 3
}
