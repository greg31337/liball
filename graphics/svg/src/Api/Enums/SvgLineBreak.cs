namespace svg;

/// <summary>Defines CSS white-space processing modes.</summary>
public enum SvgWhiteSpaceMode
{
    /// <summary>Collapse white space and allow wrapping.</summary>
    Normal = 0,
    /// <summary>Preserve white space and only honor forced breaks.</summary>
    Pre = 1,
    /// <summary>Collapse white space without soft wrapping.</summary>
    NoWrap = 2,
    /// <summary>Preserve white space and allow wrapping.</summary>
    PreWrap = 3,
    /// <summary>Preserve white space and allow a break after every preserved space.</summary>
    BreakSpaces = 4,
    /// <summary>Collapse white space, preserve segment breaks, and allow wrapping.</summary>
    PreLine = 5
}

/// <summary>Defines the inline base direction used for each laid-out line.</summary>
public enum SvgTextDirection
{
    /// <summary>Choose the direction from the first strong scalar.</summary>
    Auto = 0,
    /// <summary>Use left-to-right inline direction.</summary>
    LeftToRight = 1,
    /// <summary>Use right-to-left inline direction.</summary>
    RightToLeft = 2
}

/// <summary>Defines horizontal, vertical, and sideways writing modes.</summary>
public enum SvgWritingMode
{
    /// <summary>Horizontal lines progress from top to bottom.</summary>
    HorizontalTopToBottom = 0,
    /// <summary>Vertical lines progress from right to left.</summary>
    VerticalRightToLeft = 1,
    /// <summary>Vertical lines progress from left to right.</summary>
    VerticalLeftToRight = 2,
    /// <summary>Sideways lines progress from right to left.</summary>
    SidewaysRightToLeft = 3,
    /// <summary>Sideways lines progress from left to right.</summary>
    SidewaysLeftToRight = 4
}

/// <summary>Defines the requested orientation of characters in vertical text.</summary>
public enum SvgVerticalOrientation
{
    /// <summary>Use upright East Asian characters and sideways Western characters.</summary>
    Mixed = 0,
    /// <summary>Keep every character upright.</summary>
    Upright = 1,
    /// <summary>Rotate every character sideways.</summary>
    Sideways = 2
}

/// <summary>Defines the resolved glyph orientation for one grapheme cluster.</summary>
public enum SvgGlyphOrientation
{
    /// <summary>The glyph uses ordinary horizontal orientation.</summary>
    Horizontal = 0,
    /// <summary>The glyph remains upright on a vertical line.</summary>
    Upright = 1,
    /// <summary>The glyph is rotated sideways on a vertical line.</summary>
    Sideways = 2
}

/// <summary>Defines the line break opportunity following an item.</summary>
public enum SvgLineBreakOpportunity
{
    /// <summary>No line break is permitted.</summary>
    Prohibited = 0,
    /// <summary>A soft line break is permitted.</summary>
    Allowed = 1,
    /// <summary>A forced line break occurs.</summary>
    Mandatory = 2
}

/// <summary>Defines stable inline text processing failures.</summary>
public enum SvgInlineTextError
{
    /// <summary>No failure occurred.</summary>
    None = 0,
    /// <summary>The text, Unicode model, or options were invalid.</summary>
    InvalidArgument = 1,
    /// <summary>A configured item or line bound was exceeded.</summary>
    LimitExceeded = 2,
    /// <summary>Inline text processing was cancelled.</summary>
    Cancelled = 3
}
