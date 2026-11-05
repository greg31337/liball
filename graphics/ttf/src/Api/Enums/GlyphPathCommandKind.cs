namespace ttf;

/// <summary>Identifies one normalized glyph path operation.</summary>
public enum GlyphPathCommandKind
{
    /// <summary>Starts a new contour at an endpoint.</summary>
    Move = 0,

    /// <summary>Adds a straight segment to an endpoint.</summary>
    Line = 1,

    /// <summary>Adds a quadratic Bézier segment.</summary>
    Quadratic = 2,

    /// <summary>Closes the current contour.</summary>
    Close = 3,

    /// <summary>Adds a cubic Bézier segment.</summary>
    Cubic = 4
}
