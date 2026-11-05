namespace svg;

/// <summary>Identifies the kind of one nested clipping-stack entry.</summary>
public enum SvgClipStackEntryKind
{
    /// <summary>Clips to an axis-aligned surface-local pixel rectangle.</summary>
    Rectangle = 0,
    /// <summary>Clips to a normalized scene clip resource.</summary>
    Resource = 1
}

/// <summary>Defines stable clipping and masking failures.</summary>
public enum SvgClipMaskError
{
    /// <summary>No clipping or masking failure occurred.</summary>
    None = 0,
    /// <summary>A surface, stack entry, resource, transform, option, or limit was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>An offscreen coverage or mask surface exceeded allocation limits.</summary>
    AllocationLimitExceeded = 2,
    /// <summary>Pixel processing exceeded its configured bound.</summary>
    WorkLimitExceeded = 3,
    /// <summary>Nested clip or mask resources exceeded their configured depth.</summary>
    DepthLimitExceeded = 4,
    /// <summary>Effect-content traversal exceeded its configured node bound.</summary>
    NodeLimitExceeded = 5,
    /// <summary>A clip or mask directly or indirectly referenced itself.</summary>
    RecursionDetected = 6,
    /// <summary>Clip geometry could not be scan converted.</summary>
    RasterizationFailed = 7,
    /// <summary>Mask paint could not be sampled.</summary>
    PaintSamplingFailed = 8,
    /// <summary>Effect geometry or a transform produced a non-finite result.</summary>
    NonFiniteResult = 9,
    /// <summary>A required transform was singular.</summary>
    SingularTransform = 10
}
