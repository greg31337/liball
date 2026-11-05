namespace svg;

/// <summary>Identifies a stable complete-rendering outcome.</summary>
public enum SvgRenderError
{
    /// <summary>Rendering completed successfully.</summary>
    None = 0,
    /// <summary>The document, scene, target, options, or mutable data was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The supplied resolved document or scene no longer matched its source.</summary>
    StaleInput = 2,
    /// <summary>The requested fragment, source node, or instance was not found.</summary>
    TargetNotFound = 3,
    /// <summary>Document resolution failed.</summary>
    ResolutionFailed = 4,
    /// <summary>Normalized scene construction or extraction failed.</summary>
    SceneFailed = 5,
    /// <summary>A raster surface exceeded configured dimensions or byte limits.</summary>
    AllocationLimitExceeded = 6,
    /// <summary>Raster, paint, compositing, or effect work exceeded a configured limit.</summary>
    WorkLimitExceeded = 7,
    /// <summary>Path, text, image, or paint rasterization failed.</summary>
    RasterizationFailed = 8,
    /// <summary>Clipping, masking, filtering, or compositing failed.</summary>
    EffectFailed = 9,
    /// <summary>PNG output encoding failed.</summary>
    EncodingFailed = 10,
    /// <summary>The synchronous operation was cancelled.</summary>
    Cancelled = 11
}
