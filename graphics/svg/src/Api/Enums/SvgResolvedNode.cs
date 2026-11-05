namespace svg;

/// <summary>Identifies the semantic payload carried by one resolved node.</summary>
public enum SvgResolvedNodeKind
{
    /// <summary>An unknown or foreign element.</summary>
    Unknown = 0,
    /// <summary>An ordinary structural container.</summary>
    Container = 1,
    /// <summary>An SVG element that establishes a viewport.</summary>
    Viewport = 2,
    /// <summary>A basic SVG shape.</summary>
    Shape = 3,
    /// <summary>An SVG text-content element or XML text node.</summary>
    Text = 4,
    /// <summary>A use element or generated use instance.</summary>
    Use = 5,
    /// <summary>A switch container.</summary>
    Switch = 6,
    /// <summary>An SVG link container.</summary>
    Link = 7,
    /// <summary>An image or foreignObject element.</summary>
    EmbeddedContent = 8,
    /// <summary>A gradient or pattern resource.</summary>
    PaintServer = 9,
    /// <summary>A marker, clipping-path, or mask resource.</summary>
    Effect = 10,
    /// <summary>A filter resource and primitive graph.</summary>
    Filter = 11,
    /// <summary>A descriptive element.</summary>
    Descriptive = 12,
    /// <summary>Another recognized resource, animation, style, or script element.</summary>
    Resource = 13,
    /// <summary>Another recognized SVG element.</summary>
    Other = 14
}
