namespace svg;

/// <summary>Defines stable TrueType glyph decoding failures.</summary>
public enum SvgTrueTypeGlyphError
{
    /// <summary>No glyph decoding failure occurred.</summary>
    None = 0,
    /// <summary>The font, glyph index, or options were invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The font does not use TrueType <c>glyf</c> outlines.</summary>
    UnsupportedOutline = 2,
    /// <summary>The <c>loca</c> table was missing, truncated, unordered, or outside <c>glyf</c>.</summary>
    InvalidLocation = 3,
    /// <summary>A simple or composite glyph description was malformed.</summary>
    InvalidGlyphData = 4,
    /// <summary>The configured expanded-point limit was exceeded.</summary>
    PointLimitExceeded = 5,
    /// <summary>The configured composite-component limit was exceeded.</summary>
    ComponentLimitExceeded = 6,
    /// <summary>The configured or declared composite recursion depth was exceeded.</summary>
    RecursionLimitExceeded = 7,
    /// <summary>A composite glyph reference cycle was detected.</summary>
    CyclicComposite = 8,
    /// <summary>A component transformation produced a non-finite coordinate.</summary>
    NonFiniteResult = 9
}
