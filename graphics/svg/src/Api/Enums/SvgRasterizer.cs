namespace svg;

/// <summary>Defines stable fill scan-conversion failures.</summary>
public enum SvgRasterError
{
    /// <summary>No rasterization failure occurred.</summary>
    None = 0,
    /// <summary>A path, surface, color, rule, transform, clip, sample count, or limit was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The mutable path command sequence was invalid.</summary>
    InvalidPath = 2,
    /// <summary>Geometry arithmetic or transformation produced a non-finite result.</summary>
    NonFiniteResult = 3,
    /// <summary>The flattened or implicitly closing edge count exceeded its configured limit.</summary>
    EdgeLimitExceeded = 4,
    /// <summary>The configured scan-conversion work limit was exceeded.</summary>
    WorkLimitExceeded = 5,
    /// <summary>The requested tolerance required more adaptive subdivision depth.</summary>
    SubdivisionLimitExceeded = 6
}
