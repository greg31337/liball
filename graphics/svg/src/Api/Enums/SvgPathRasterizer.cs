namespace svg;

/// <summary>Defines sampling profiles requested by the SVG shape-rendering property.</summary>
public enum SvgShapeRendering
{
    /// <summary>Uses the caller's balanced sampling and tolerance settings.</summary>
    Auto = 0,
    /// <summary>Uses one sample per pixel and a coarser curve tolerance.</summary>
    OptimizeSpeed = 1,
    /// <summary>Uses one sample per pixel while retaining the configured curve tolerance.</summary>
    CrispEdges = 2,
    /// <summary>Uses denser sampling and a finer curve tolerance.</summary>
    GeometricPrecision = 3
}

/// <summary>Defines stable combined path-rasterization failures.</summary>
public enum SvgPathRasterError
{
    /// <summary>No path-rasterization failure occurred.</summary>
    None = 0,
    /// <summary>The path, surface, paint, opacity, order, hint, stroke, or raster options were invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The mutable path command sequence was invalid.</summary>
    InvalidPath = 2,
    /// <summary>Fill scan conversion failed.</summary>
    FillFailed = 3,
    /// <summary>Stroke transformation, outlining, or scan conversion failed.</summary>
    StrokeFailed = 4
}
