namespace svg;

/// <summary>Defines SVG fill winding rules.</summary>
public enum SvgFillRule
{
    /// <summary>Uses the signed nonzero winding-number rule.</summary>
    Nonzero = 0,
    /// <summary>Uses the parity of ray crossings.</summary>
    EvenOdd = 1
}

/// <summary>Defines a point's robust classification relative to filled geometry.</summary>
public enum SvgFillContainmentKind
{
    /// <summary>The point is outside the filled geometry.</summary>
    Outside = 0,
    /// <summary>The point is inside the filled geometry.</summary>
    Inside = 1,
    /// <summary>The point is within tolerance of the path boundary.</summary>
    Boundary = 2
}

/// <summary>Defines stable fill-containment failures.</summary>
public enum SvgFillContainmentError
{
    /// <summary>No containment failure occurred.</summary>
    None = 0,
    /// <summary>The path, point, rule, transform, tolerance, or limits were invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The mutable path command sequence was invalid.</summary>
    InvalidPath = 2,
    /// <summary>Coordinate transformation or geometry arithmetic produced a non-finite result.</summary>
    NonFiniteResult = 3,
    /// <summary>The flattened or implicitly closing edge count exceeded its configured limit.</summary>
    SegmentLimitExceeded = 4,
    /// <summary>The requested tolerance required more adaptive subdivision depth.</summary>
    SubdivisionLimitExceeded = 5
}
