namespace svg;

/// <summary>Defines stable adaptive flattening failures.</summary>
public enum SvgFlattenError
{
    /// <summary>No flattening failure occurred.</summary>
    None = 0,
    /// <summary>The segment, tolerance, transform, or limits were invalid.</summary>
    InvalidArgument = 1,
    /// <summary>Measurement or subdivision produced a non-finite coordinate.</summary>
    NonFiniteResult = 2,
    /// <summary>The configured flattened-segment count was exceeded.</summary>
    SegmentLimitExceeded = 3,
    /// <summary>The requested tolerance required more subdivision depth.</summary>
    SubdivisionLimitExceeded = 4
}
