namespace markdown;

/// <summary>
/// Identifies whether an event enters or leaves a node.
/// </summary>
public enum MarkdownEventKind
{
    /// <summary>
    /// Marks an event that nones a node.
    /// </summary>
    None = 0,
    /// <summary>
    /// Marks an event that enter nodes a node.
    /// </summary>
    EnterNode = 1,
    /// <summary>
    /// Marks an event that leave nodes a node.
    /// </summary>
    LeaveNode = 2
}
