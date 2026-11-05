namespace svg;

/// <summary>Defines stable filter primitive evaluation failures.</summary>
public enum SvgFilterRenderError
{
    /// <summary>No filter evaluation failure occurred.</summary>
    None = 0,
    /// <summary>The plan, options, primitive parameters, surfaces, or pixels were invalid.</summary>
    InvalidArgument = 1,
    /// <summary>Filter graph planning failed.</summary>
    PlanningFailed = 2,
    /// <summary>The graph contains a primitive implemented by a later evaluator.</summary>
    UnsupportedPrimitive = 3,
    /// <summary>The cumulative pixel work exceeded its configured bound.</summary>
    WorkLimitExceeded = 4,
    /// <summary>A required surface clear, read, write, or view operation failed.</summary>
    BufferOperationFailed = 5,
    /// <summary>A color conversion failed.</summary>
    ColorConversionFailed = 6,
    /// <summary>A transformed primitive parameter was non-finite.</summary>
    NonFiniteValue = 7
}
