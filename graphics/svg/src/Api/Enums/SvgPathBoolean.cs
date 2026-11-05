namespace svg;

/// <summary>Defines supported filled-path Boolean operations.</summary>
public enum SvgPathBooleanOperation
{
    /// <summary>Keeps points filled by either operand.</summary>
    Union = 0,
    /// <summary>Keeps points filled by both operands.</summary>
    Intersection = 1,
    /// <summary>Keeps points filled by the first operand but not the second.</summary>
    Difference = 2,
    /// <summary>Keeps points filled by exactly one operand.</summary>
    Xor = 3
}

/// <summary>Defines stable path Boolean failures.</summary>
public enum SvgPathBooleanError
{
    /// <summary>No Boolean operation failed.</summary>
    None = 0,
    /// <summary>A path, operation, fill rule, tolerance, or limit was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>A mutable path command sequence was invalid.</summary>
    InvalidPath = 2,
    /// <summary>Geometry arithmetic produced a non-finite result.</summary>
    NonFiniteResult = 3,
    /// <summary>Flattening, arrangement, or output exceeded the edge limit.</summary>
    EdgeLimitExceeded = 4,
    /// <summary>Adaptive flattening exceeded the subdivision limit.</summary>
    SubdivisionLimitExceeded = 5,
    /// <summary>Arrangement construction exceeded the comparison limit.</summary>
    ComparisonLimitExceeded = 6,
    /// <summary>Arrangement construction exceeded the intersection limit.</summary>
    IntersectionLimitExceeded = 7,
    /// <summary>Selected boundary edges could not form closed contours.</summary>
    ReconstructionFailed = 8
}
