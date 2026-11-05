namespace svg;

/// <summary>Identifies Porter-Duff pixel compositing operators.</summary>
public enum SvgCompositeOperator
{
    /// <summary>Clears the destination covered by the source rectangle.</summary>
    Clear = 0,
    /// <summary>Replaces the destination with the source.</summary>
    Copy = 1,
    /// <summary>Retains the destination unchanged.</summary>
    Destination = 2,
    /// <summary>Places the source over the destination.</summary>
    SourceOver = 3,
    /// <summary>Places the destination over the source.</summary>
    DestinationOver = 4,
    /// <summary>Keeps source content inside destination alpha.</summary>
    SourceIn = 5,
    /// <summary>Keeps destination content inside source alpha.</summary>
    DestinationIn = 6,
    /// <summary>Keeps source content outside destination alpha.</summary>
    SourceOut = 7,
    /// <summary>Keeps destination content outside source alpha.</summary>
    DestinationOut = 8,
    /// <summary>Places source inside destination and destination outside source.</summary>
    SourceAtop = 9,
    /// <summary>Places destination inside source and source outside destination.</summary>
    DestinationAtop = 10,
    /// <summary>Keeps the non-overlapping portions of source and destination.</summary>
    Xor = 11,
    /// <summary>Adds source and destination with channel saturation.</summary>
    Lighter = 12
}

/// <summary>Defines stable premultiplied compositing failures.</summary>
public enum SvgCompositeError
{
    /// <summary>No compositing failure occurred.</summary>
    None = 0,
    /// <summary>A buffer, rectangle, clip, option, operator, opacity, or limit was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The source and destination concrete color spaces differed.</summary>
    ColorSpaceMismatch = 2,
    /// <summary>The clipped pixel count exceeded its configured bound.</summary>
    WorkLimitExceeded = 3,
    /// <summary>An overlap snapshot or opacity-group surface exceeded allocation limits.</summary>
    AllocationLimitExceeded = 4,
    /// <summary>A required pixel-buffer operation failed.</summary>
    BufferOperationFailed = 5,
    /// <summary>A sampled source or destination pixel was not premultiplied.</summary>
    InvalidPremultipliedPixel = 6
}
