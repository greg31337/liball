namespace svg;

/// <summary>Defines stable lossy path-simplification failures.</summary>
public enum SvgPathSimplifyError
{
    /// <summary>No simplification failed.</summary>
    None = 0,
    /// <summary>The path, tolerance, rounding precision, or limit was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The mutable path command sequence was invalid.</summary>
    InvalidPath = 2,
    /// <summary>Geometry arithmetic produced a non-finite result.</summary>
    NonFiniteResult = 3,
    /// <summary>Normalization or output exceeded the configured command limit.</summary>
    CommandLimitExceeded = 4,
    /// <summary>Flattening exceeded the configured point limit.</summary>
    PointLimitExceeded = 5,
    /// <summary>Adaptive curve flattening exceeded the subdivision limit.</summary>
    SubdivisionLimitExceeded = 6,
    /// <summary>Reduction and error measurement exceeded the operation limit.</summary>
    OperationLimitExceeded = 7,
    /// <summary>The requested coordinate rounding exceeded the geometric error limit.</summary>
    ErrorLimitExceeded = 8
}
