namespace svg;

/// <summary>Identifies the inline direction supported by a legacy SVG glyph.</summary>
public enum SvgSvgFontOrientation
{
    /// <summary>The glyph or lookup is valid in either inline direction.</summary>
    Both = 0,
    /// <summary>The glyph or lookup is horizontal.</summary>
    Horizontal = 1,
    /// <summary>The glyph or lookup is vertical.</summary>
    Vertical = 2
}

/// <summary>Identifies an Arabic contextual form declared by a legacy SVG glyph.</summary>
public enum SvgSvgArabicForm
{
    /// <summary>No contextual Arabic form is declared or requested.</summary>
    None = 0,
    /// <summary>The glyph is an initial form.</summary>
    Initial = 1,
    /// <summary>The glyph is a medial form.</summary>
    Medial = 2,
    /// <summary>The glyph is a terminal form.</summary>
    Terminal = 3,
    /// <summary>The glyph is an isolated form.</summary>
    Isolated = 4
}

/// <summary>Defines stable legacy SVG font parsing failures.</summary>
public enum SvgSvgFontError
{
    /// <summary>No legacy font failure occurred.</summary>
    None = 0,
    /// <summary>An argument or option was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The supplied node was not a legacy SVG font element.</summary>
    NotFontElement = 2,
    /// <summary>A required font-face child was absent.</summary>
    MissingFontFace = 3,
    /// <summary>A font or glyph metric was invalid.</summary>
    InvalidMetric = 4,
    /// <summary>A glyph selector or outline was invalid.</summary>
    InvalidGlyph = 5,
    /// <summary>A horizontal or vertical kerning rule was invalid.</summary>
    InvalidKerning = 6,
    /// <summary>A configured glyph, kerning, path, or token bound was exceeded.</summary>
    LimitExceeded = 7,
    /// <summary>Parsing was cancelled.</summary>
    Cancelled = 8
}
