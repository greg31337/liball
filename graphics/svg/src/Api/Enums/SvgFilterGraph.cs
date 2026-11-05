namespace svg;

/// <summary>Defines stable filter-graph planning failures.</summary>
public enum SvgFilterGraphError
{
    /// <summary>No planning failure occurred.</summary>
    None = 0,
    /// <summary>The resource, graph, transform, clip, input surface, or limits were invalid.</summary>
    InvalidArgument = 1,
    /// <summary>A filter or primitive region could not be represented by finite pixel bounds.</summary>
    NonFiniteRegion = 2,
    /// <summary>The primitive count exceeded the configured filter limit.</summary>
    PrimitiveLimitExceeded = 3,
    /// <summary>The filter region exceeded its configured pixel-area limit.</summary>
    AreaLimitExceeded = 4,
    /// <summary>The number of simultaneously required surfaces exceeded its configured limit.</summary>
    SurfaceLimitExceeded = 5,
    /// <summary>A required pixel buffer could not be allocated.</summary>
    AllocationFailed = 6,
    /// <summary>An input referred to a missing or nonpreceding primitive.</summary>
    InvalidGraph = 7
}

/// <summary>Identifies the lifetime role of one planned filter surface.</summary>
public enum SvgFilterSurfaceKind
{
    /// <summary>An immutable snapshot of one standard filter input.</summary>
    StandardInput = 0,
    /// <summary>A reusable surface carrying one or more nonoverlapping primitive lifetimes.</summary>
    PrimitiveOutput = 1
}
