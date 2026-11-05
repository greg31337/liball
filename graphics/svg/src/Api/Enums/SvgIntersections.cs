namespace svg;

/// <summary>Identifies a supported intersection segment representation.</summary>
public enum SvgIntersectionSegmentKind
{
    /// <summary>A line segment.</summary>
    Line = 0,
    /// <summary>A quadratic Bézier segment.</summary>
    Quadratic = 1,
    /// <summary>A cubic Bézier segment.</summary>
    Cubic = 2,
    /// <summary>A center-parameterized elliptical arc.</summary>
    Arc = 3
}

/// <summary>Classifies one isolated or overlapping intersection.</summary>
public enum SvgIntersectionKind
{
    /// <summary>The segment tangents cross transversely.</summary>
    Crossing = 0,
    /// <summary>The segment tangents are parallel at an isolated contact.</summary>
    Tangent = 1,
    /// <summary>The result bounds a collinear overlapping interval.</summary>
    Overlap = 2
}

/// <summary>Defines stable intersection failures.</summary>
public enum SvgIntersectionError
{
    /// <summary>No intersection failure occurred.</summary>
    None = 0,
    /// <summary>A segment, tolerance, or configured limit was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>Geometry arithmetic produced a non-finite result.</summary>
    NonFiniteResult = 2,
    /// <summary>Adaptive flattening exceeded its segment limit.</summary>
    SegmentLimitExceeded = 3,
    /// <summary>Adaptive flattening exceeded its subdivision limit.</summary>
    SubdivisionLimitExceeded = 4,
    /// <summary>Pair comparison exceeded its configured limit.</summary>
    ComparisonLimitExceeded = 5,
    /// <summary>The distinct intersection count exceeded its configured limit.</summary>
    IntersectionLimitExceeded = 6
}
