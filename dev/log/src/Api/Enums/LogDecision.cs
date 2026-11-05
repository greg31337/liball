namespace log;

/// <summary>
/// Specifies how a filter contributes to the decision to include an event.
/// </summary>
public enum LogDecision
{
    /// <summary>
    /// Leaves the decision to the remaining filters.
    /// </summary>
    Neutral = 0,
    /// <summary>
    /// Allows the event unless another filter excludes it.
    /// </summary>
    Include = 1,
    /// <summary>
    /// Rejects the event.
    /// </summary>
    Exclude = 2
}
