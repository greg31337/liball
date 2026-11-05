namespace svg;

/// <summary>Defines stable drawing-bounds failures.</summary>
public enum SvgDrawingBoundsError
{
    /// <summary>No bounds operation failed.</summary>
    None = 0,
    /// <summary>A path, shape, transform, option, or limit was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The mutable path command sequence was invalid.</summary>
    InvalidPath = 2,
    /// <summary>Geometry arithmetic produced a non-finite result.</summary>
    NonFiniteResult = 3,
    /// <summary>Normalization, stroke, marker, or document traversal exceeded a limit.</summary>
    LimitExceeded = 4,
    /// <summary>A resolved shape could not be converted to path geometry.</summary>
    ShapeConversionFailed = 5
}
