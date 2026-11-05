namespace svg;

/// <summary>Defines stable path measurement and extraction failures.</summary>
public enum SvgPathMetricError
{
    /// <summary>No metric failure occurred.</summary>
    None = 0,
    /// <summary>The path, distance, calibration, or options were invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The mutable path command sequence was invalid.</summary>
    InvalidPath = 2,
    /// <summary>Length or coordinate arithmetic produced a non-finite result.</summary>
    NonFiniteResult = 3,
    /// <summary>Adaptive measurement exceeded its segment limit.</summary>
    SegmentLimitExceeded = 4,
    /// <summary>Adaptive measurement exceeded its subdivision limit.</summary>
    SubdivisionLimitExceeded = 5
}
