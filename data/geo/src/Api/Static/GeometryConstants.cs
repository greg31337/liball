namespace geo;

/// <summary>Provides shared geometry metadata, limits, and numeric tolerances.</summary>
public static class GeometryConstants
{
    /// <summary>The spatial reference identifier used when no SRID is known.</summary>
    public const int UnknownSpatialReferenceId = 0;

    /// <summary>The library convention for OGC CRS84 longitude and latitude coordinates.</summary>
    public const int Crs84SpatialReferenceId = 4326;

    /// <summary>The no-data value used for an unavailable Z or M ordinate.</summary>
    public static readonly double NoDataOrdinate = double.NaN;

    /// <summary>The default maximum number of coordinates accepted from one input.</summary>
    public const int DefaultMaximumCoordinates = 1000000;

    /// <summary>The default maximum number of components accepted from one input.</summary>
    public const int DefaultMaximumComponents = 100000;

    /// <summary>The default maximum nesting depth accepted from one input.</summary>
    public const int DefaultMaximumNestingDepth = 64;

    /// <summary>The default maximum encoded input size in bytes.</summary>
    public const int DefaultMaximumInputBytes = 67108864;

    /// <summary>The default absolute tolerance for explicitly tolerant numeric comparisons.</summary>
    public const double DefaultAbsoluteTolerance = 1e-12;

    /// <summary>The default relative tolerance for explicitly tolerant numeric comparisons.</summary>
    public const double DefaultRelativeTolerance = 1e-12;
}
