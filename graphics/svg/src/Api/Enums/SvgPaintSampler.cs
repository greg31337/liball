namespace svg;

/// <summary>Defines stable paint-sampling failures.</summary>
public enum SvgPaintSampleError
{
    /// <summary>No paint-sampling failure occurred.</summary>
    None = 0,
    /// <summary>The resource, point, options, paint data, or mutable state was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The resource is not a solid, linear-gradient, or radial-gradient paint.</summary>
    UnsupportedPaint = 2,
    /// <summary>The configured normalized stop limit was exceeded.</summary>
    StopLimitExceeded = 3,
    /// <summary>The paint transform could not be inverted.</summary>
    SingularTransform = 4,
    /// <summary>Geometry arithmetic produced a non-finite result.</summary>
    NonFiniteResult = 5,
    /// <summary>A stop or solid color could not be converted to the requested output.</summary>
    ColorConversionFailed = 6,
    /// <summary>The pattern tile exceeded a configured raster allocation limit.</summary>
    PatternTileLimitExceeded = 7,
    /// <summary>A pattern directly or indirectly referenced itself.</summary>
    PatternRecursionDetected = 8,
    /// <summary>Nested pattern sampling exceeded its configured depth.</summary>
    PatternDepthLimitExceeded = 9,
    /// <summary>Pattern content traversal exceeded its configured node count.</summary>
    PatternNodeLimitExceeded = 10,
    /// <summary>Pattern content could not be rasterized.</summary>
    PatternRenderFailed = 11
}
