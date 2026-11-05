namespace svg;

/// <summary>Defines stable text geometry, extraction, and hit-testing failures.</summary>
public enum SvgTextGeometryError
{
    /// <summary>No text geometry failure occurred.</summary>
    None = 0,
    /// <summary>The layout, point, font resource, or options were invalid.</summary>
    InvalidArgument = 1,
    /// <summary>A positioned glyph referenced no supplied font resource.</summary>
    FontNotFound = 2,
    /// <summary>The font outline format requires an unavailable caller fallback.</summary>
    UnsupportedOutline = 3,
    /// <summary>A glyph outline could not be decoded or transformed.</summary>
    InvalidOutline = 4,
    /// <summary>A configured glyph, path-command, or geometry-work bound was exceeded.</summary>
    LimitExceeded = 5,
    /// <summary>Text geometry processing was cancelled.</summary>
    Cancelled = 6
}

/// <summary>Identifies one generated text decoration line.</summary>
public enum SvgTextDecorationKind
{
    /// <summary>An underline on the text-under side of the baseline.</summary>
    Underline = 0,
    /// <summary>An overline near the text-over edge.</summary>
    Overline = 1,
    /// <summary>A line through the central glyph region.</summary>
    LineThrough = 2
}

/// <summary>Selects glyph-ink, glyph-logical, or cluster-logical hit testing.</summary>
public enum SvgTextHitTestMode
{
    /// <summary>Test the filled glyph outline.</summary>
    GlyphInk = 0,
    /// <summary>Test the glyph logical cell.</summary>
    GlyphLogical = 1,
    /// <summary>Test the unioned logical cell for each source cluster.</summary>
    ClusterLogical = 2
}
