namespace email;
/// <summary>
/// Specifies the supported HTML mutation values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailHtmlMutationKind
{
    /// <summary>
    /// Represents the removed element choice or state.
    /// </summary>
    RemovedElement = 0,
    /// <summary>
    /// Represents the removed attribute choice or state.
    /// </summary>
    RemovedAttribute = 1,
    /// <summary>
    /// Represents the rewritten attribute choice or state.
    /// </summary>
    RewrittenAttribute = 2,
    /// <summary>
    /// Represents the blocked remote resource choice or state.
    /// </summary>
    BlockedRemoteResource = 3,
    /// <summary>
    /// Represents the blocked unsafe url choice or state.
    /// </summary>
    BlockedUnsafeUrl = 4,
    /// <summary>
    /// Represents the removed comment choice or state.
    /// </summary>
    RemovedComment = 5,
    /// <summary>
    /// Represents the rebalanced markup choice or state.
    /// </summary>
    RebalancedMarkup = 6
}
