namespace svg;

/// <summary>Identifies the supported structural role of an SVG element.</summary>
public enum SvgStructuralElementKind
{
    /// <summary>No supported structural role.</summary>
    None = 0,
    /// <summary>A <c>g</c> grouping container.</summary>
    Group = 1,
    /// <summary>A non-rendered <c>defs</c> definition container.</summary>
    Definitions = 2,
    /// <summary>A reference-only <c>symbol</c> container.</summary>
    Symbol = 3,
    /// <summary>A non-rendered <c>view</c> specification.</summary>
    View = 4,
    /// <summary>An <c>a</c> link container.</summary>
    Link = 5,
    /// <summary>A non-rendered <c>metadata</c> element.</summary>
    Metadata = 6,
    /// <summary>A non-rendered <c>title</c> element.</summary>
    Title = 7,
    /// <summary>A non-rendered <c>desc</c> element.</summary>
    Description = 8
}
