namespace svg;

/// <summary>Defines SVG pointer-event geometry selection modes.</summary>
public enum SvgPointerEventsMode
{
    /// <summary>Uses visible-painted behavior.</summary>
    Auto = 0,
    /// <summary>Disables pointer hits.</summary>
    None = 1,
    /// <summary>Tests the transformed object bounding box.</summary>
    BoundingBox = 2,
    /// <summary>Tests visible painted fill and stroke geometry.</summary>
    VisiblePainted = 3,
    /// <summary>Tests visible fill geometry regardless of fill paint.</summary>
    VisibleFill = 4,
    /// <summary>Tests visible stroke geometry regardless of stroke paint.</summary>
    VisibleStroke = 5,
    /// <summary>Tests visible fill and stroke geometry regardless of paint.</summary>
    Visible = 6,
    /// <summary>Tests painted fill and stroke geometry regardless of visibility.</summary>
    Painted = 7,
    /// <summary>Tests fill geometry regardless of paint and visibility.</summary>
    Fill = 8,
    /// <summary>Tests stroke geometry regardless of paint and visibility.</summary>
    Stroke = 9,
    /// <summary>Tests fill and stroke geometry regardless of paint and visibility.</summary>
    All = 10
}

/// <summary>Defines stable geometry and document hit-test failures.</summary>
public enum SvgHitTestError
{
    /// <summary>No hit-test failure occurred.</summary>
    None = 0,
    /// <summary>An input, option, enum, transform, or mutable state was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The mutable path command sequence was invalid.</summary>
    InvalidPath = 2,
    /// <summary>Geometry arithmetic produced a non-finite result.</summary>
    NonFiniteResult = 3,
    /// <summary>A geometry, traversal, or result limit was exceeded.</summary>
    LimitExceeded = 4,
    /// <summary>A resolved shape could not be converted to path geometry.</summary>
    ShapeConversionFailed = 5
}
