namespace svg;

/// <summary>Defines stable geometry and affine-matrix failures.</summary>
public enum SvgGeometryError
{
    /// <summary>No geometry failure occurred.</summary>
    None = 0,
    /// <summary>An argument, enum, or mutable object state was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>A computed geometry or matrix component was not finite.</summary>
    NonFiniteResult = 2,
    /// <summary>The matrix was singular and could not be inverted or decomposed.</summary>
    SingularMatrix = 3
}
