namespace svg;

/// <summary>Defines stable OpenType shaping outcomes.</summary>
public enum SvgShapingError
{
    /// <summary>No shaping failure occurred.</summary>
    None = 0,
    /// <summary>The font, text, or options were invalid.</summary>
    InvalidArgument = 1,
    /// <summary>A retained OpenType layout table was malformed.</summary>
    InvalidTable = 2,
    /// <summary>A glyph, lookup, operation, or variation-axis bound was exceeded.</summary>
    LimitExceeded = 3,
    /// <summary>Shaping was cancelled.</summary>
    Cancelled = 4
}

/// <summary>Defines glyph classes read from the OpenType GDEF table.</summary>
public enum SvgOpenTypeGlyphClass
{
    /// <summary>No glyph class is assigned.</summary>
    Unclassified = 0,
    /// <summary>A base glyph.</summary>
    Base = 1,
    /// <summary>A ligature glyph.</summary>
    Ligature = 2,
    /// <summary>A combining mark glyph.</summary>
    Mark = 3,
    /// <summary>A glyph component.</summary>
    Component = 4
}
