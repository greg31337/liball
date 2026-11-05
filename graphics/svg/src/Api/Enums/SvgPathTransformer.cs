namespace svg;

/// <summary>Defines stable path transformation failures.</summary>
public enum SvgPathTransformError
{
    /// <summary>No transformation failure occurred.</summary>
    None = 0,
    /// <summary>The path, matrix, or options were invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The mutable path command sequence was invalid.</summary>
    InvalidPath = 2,
    /// <summary>The transformed output exceeded its command limit.</summary>
    CommandLimitExceeded = 3,
    /// <summary>Coordinate transformation produced a non-finite result.</summary>
    NonFiniteResult = 4
}
