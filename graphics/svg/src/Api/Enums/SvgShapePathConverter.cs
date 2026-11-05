namespace svg;

/// <summary>Defines stable basic-shape path conversion failures.</summary>
public enum SvgShapePathError
{
    /// <summary>No conversion failure occurred.</summary>
    None = 0,
    /// <summary>The shape or command limit was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The resolved shape kind is not a convertible basic shape.</summary>
    UnsupportedShape = 2,
    /// <summary>The generated command count would exceed the configured limit.</summary>
    CommandLimitExceeded = 3,
    /// <summary>Shape arithmetic produced a non-finite coordinate.</summary>
    NonFiniteResult = 4
}
