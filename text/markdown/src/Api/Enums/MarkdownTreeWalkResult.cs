namespace markdown;

/// <summary>
/// Controls whether tree traversal continues, skips children, or stops.
/// </summary>
public enum MarkdownTreeWalkResult
{
    /// <summary>
    /// Continues traversal into children and later siblings.
    /// </summary>
    Continue = 0,
    /// <summary>
    /// Skips the current node's children and continues with its next sibling.
    /// </summary>
    SkipChildren = 1,
    /// <summary>
    /// Stops traversal immediately.
    /// </summary>
    Stop = 2
}
