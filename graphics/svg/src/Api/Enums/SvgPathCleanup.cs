namespace svg;

/// <summary>Defines stable exact path-cleanup failures.</summary>
public enum SvgPathCleanupError
{
    /// <summary>No cleanup operation failed.</summary>
    None = 0,
    /// <summary>The path, options, or configured command limit was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The mutable path command sequence was invalid.</summary>
    InvalidPath = 2,
    /// <summary>The normalized command count exceeded the configured limit.</summary>
    CommandLimitExceeded = 3,
    /// <summary>Geometry conversion produced a non-finite result.</summary>
    NonFiniteResult = 4
}
