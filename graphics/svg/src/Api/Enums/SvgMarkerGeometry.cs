namespace svg;

/// <summary>Defines stable marker-geometry failures.</summary>
public enum SvgMarkerGeometryError
{
    /// <summary>No marker failure occurred.</summary>
    None = 0,
    /// <summary>The path, marker definition, enum, scale, transform, or limit was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The mutable path command sequence was invalid.</summary>
    InvalidPath = 2,
    /// <summary>Geometry or transform arithmetic produced a non-finite result.</summary>
    NonFiniteResult = 3,
    /// <summary>The configured marker vertex or instance limit was exceeded.</summary>
    VertexLimitExceeded = 4
}
