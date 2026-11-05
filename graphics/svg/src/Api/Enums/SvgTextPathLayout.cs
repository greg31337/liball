namespace svg;

/// <summary>Identifies how textPath geometry entered layout.</summary>
public enum SvgTextPathSourceKind
{
    /// <summary>A caller-resolved path was supplied directly.</summary>
    ReferencedPath = 0,
    /// <summary>A resolved basic shape was converted to a path.</summary>
    ReferencedShape = 1,
    /// <summary>Inline SVG path data was parsed for text layout.</summary>
    InlinePathData = 2
}

/// <summary>Defines glyph treatment along the text path.</summary>
public enum SvgTextPathMethod
{
    /// <summary>Preserve the positioned text's inline scale.</summary>
    Align = 0,
    /// <summary>Scale inline glyph geometry and distances to the available path length.</summary>
    Stretch = 1
}

/// <summary>Defines textPath spacing behavior.</summary>
public enum SvgTextPathSpacing
{
    /// <summary>Use exact positioned-text spacing.</summary>
    Exact = 0,
    /// <summary>Distribute available path space between addressable characters.</summary>
    Auto = 1
}

/// <summary>Defines the side and traversal direction of a text path.</summary>
public enum SvgTextPathSide
{
    /// <summary>Use the left side while traversing the path forward.</summary>
    Left = 0,
    /// <summary>Use the right side while traversing the path backward.</summary>
    Right = 1
}

/// <summary>Defines rendering of glyphs whose midpoint is outside an open path.</summary>
public enum SvgTextPathOverflow
{
    /// <summary>Suppress glyphs beyond either open endpoint.</summary>
    Hidden = 0,
    /// <summary>Extrapolate the endpoint tangent for glyphs beyond the path.</summary>
    Visible = 1
}

/// <summary>Defines stable textPath layout failures.</summary>
public enum SvgTextPathError
{
    /// <summary>No textPath failure occurred.</summary>
    None = 0,
    /// <summary>The positioned text, path, or options were invalid.</summary>
    InvalidArgument = 1,
    /// <summary>Path parsing, conversion, normalization, or measurement failed.</summary>
    InvalidPath = 2,
    /// <summary>A configured glyph, query, command, or flattening bound was exceeded.</summary>
    LimitExceeded = 3,
    /// <summary>Text-path layout was cancelled.</summary>
    Cancelled = 4
}
