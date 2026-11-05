namespace svg;

/// <summary>Defines stable planar-geometry failures.</summary>
public enum SvgPlanarError
{
    /// <summary>No planar operation failed.</summary>
    None = 0,
    /// <summary>An input, tolerance, rule, or configured limit was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The mutable path command sequence was invalid.</summary>
    InvalidPath = 2,
    /// <summary>Geometry arithmetic produced a non-finite result.</summary>
    NonFiniteResult = 3,
    /// <summary>Flattening or output exceeded the configured edge limit.</summary>
    EdgeLimitExceeded = 4,
    /// <summary>Adaptive flattening exceeded the configured subdivision depth.</summary>
    SubdivisionLimitExceeded = 5,
    /// <summary>Intersection discovery exceeded the configured comparison limit.</summary>
    ComparisonLimitExceeded = 6,
    /// <summary>Intersection discovery exceeded the configured result limit.</summary>
    IntersectionLimitExceeded = 7,
    /// <summary>Directed edges could not be reconstructed into closed contours.</summary>
    OpenContour = 8
}
