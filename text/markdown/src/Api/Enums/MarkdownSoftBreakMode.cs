namespace markdown;

/// <summary>
/// Specifies how a soft line break is rendered.
/// </summary>
public enum MarkdownSoftBreakMode
{
    /// <summary>
    /// Renders soft breaks using preserve behavior.
    /// </summary>
    Preserve = 0,
    /// <summary>
    /// Renders soft breaks using space behavior.
    /// </summary>
    Space = 1,
    /// <summary>
    /// Renders soft breaks using hard break behavior.
    /// </summary>
    HardBreak = 2,
    /// <summary>
    /// Renders soft breaks using CJK behavior.
    /// </summary>
    Cjk = 3
}
