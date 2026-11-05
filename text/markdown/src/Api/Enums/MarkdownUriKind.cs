namespace markdown;

/// <summary>
/// Classifies a Markdown destination by URI shape.
/// </summary>
public enum MarkdownUriKind
{
    /// <summary>
    /// Classifies the destination as empty.
    /// </summary>
    Empty = 0,
    /// <summary>
    /// Classifies the destination as fragment.
    /// </summary>
    Fragment = 1,
    /// <summary>
    /// Classifies the destination as relative.
    /// </summary>
    Relative = 2,
    /// <summary>
    /// Classifies the destination as absolute.
    /// </summary>
    Absolute = 3,
    /// <summary>
    /// Classifies the destination as mailto.
    /// </summary>
    Mailto = 4,
    /// <summary>
    /// Classifies the destination as data.
    /// </summary>
    Data = 5,
    /// <summary>
    /// Classifies the destination as unsafe.
    /// </summary>
    Unsafe = 6
}
