namespace svg;

/// <summary>Defines stable path normalization failures.</summary>
public enum SvgPathNormalizeError
{
    /// <summary>No normalization failure occurred.</summary>
    None = 0,
    /// <summary>The path or options were missing or invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The mutable path command sequence or payload was invalid.</summary>
    InvalidPath = 2,
    /// <summary>The normalized command count exceeded the configured limit.</summary>
    CommandLimitExceeded = 3,
    /// <summary>Coordinate or control-point arithmetic produced a non-finite value.</summary>
    NonFiniteResult = 4
}
