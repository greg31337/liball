namespace svg;

/// <summary>Defines stable path-builder failures.</summary>
public enum SvgPathBuilderError
{
    /// <summary>No builder operation has failed.</summary>
    None = 0,
    /// <summary>An argument, option, or mutable builder setting was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The first path command was required to be a move command.</summary>
    ExpectedMoveTo = 2,
    /// <summary>An operation required an established current point.</summary>
    MissingCurrentPoint = 3,
    /// <summary>The configured command bound was exceeded.</summary>
    CommandLimitExceeded = 4,
    /// <summary>Coordinate arithmetic produced a non-finite value.</summary>
    NonFiniteResult = 5,
    /// <summary>An appended or mutable path was invalid.</summary>
    InvalidPath = 6,
    /// <summary>An affine path transformation failed.</summary>
    TransformFailed = 7,
    /// <summary>Path-data formatting failed.</summary>
    FormatFailed = 8,
    /// <summary>A destination document builder rejected the path element.</summary>
    MutationFailed = 9
}
