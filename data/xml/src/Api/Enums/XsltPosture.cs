namespace xml;

/// <summary>
/// Specifies how an XSLT construct returns nodes relative to its streamed input.
/// </summary>
public enum XsltPosture
{
    /// <summary>
    /// Indicates a grounded value that does not retain streamed nodes.
    /// </summary>
    Grounded = 0,
    /// <summary>
    /// Indicates nodes selected along a single downward path.
    /// </summary>
    Striding = 1,
    /// <summary>
    /// Indicates potentially overlapping descendant nodes.
    /// </summary>
    Crawling = 2,
    /// <summary>
    /// Indicates unrestricted navigation that is not guaranteed streamable.
    /// </summary>
    Roaming = 3
}
