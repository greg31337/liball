namespace svg;

/// <summary>Defines preserveAspectRatio alignment modes.</summary>
public enum SvgAspectRatioAlignment
{
    /// <summary>Uses independent horizontal and vertical scaling without alignment.</summary>
    None = 0,

    /// <summary>Aligns the minimum x and minimum y edges.</summary>
    XMinYMin = 1,

    /// <summary>Centers horizontally and aligns the minimum y edge.</summary>
    XMidYMin = 2,

    /// <summary>Aligns the maximum x and minimum y edges.</summary>
    XMaxYMin = 3,

    /// <summary>Aligns the minimum x edge and centers vertically.</summary>
    XMinYMid = 4,

    /// <summary>Centers horizontally and vertically.</summary>
    XMidYMid = 5,

    /// <summary>Aligns the maximum x edge and centers vertically.</summary>
    XMaxYMid = 6,

    /// <summary>Aligns the minimum x and maximum y edges.</summary>
    XMinYMax = 7,

    /// <summary>Centers horizontally and aligns the maximum y edge.</summary>
    XMidYMax = 8,

    /// <summary>Aligns the maximum x and maximum y edges.</summary>
    XMaxYMax = 9
}

/// <summary>Defines uniform preserveAspectRatio scaling modes.</summary>
public enum SvgAspectRatioScale
{
    /// <summary>Fits the complete view box inside the viewport.</summary>
    Meet = 0,

    /// <summary>Covers the complete viewport with the view box.</summary>
    Slice = 1
}

/// <summary>Defines stable failures produced while fitting a view box.</summary>
public enum SvgViewBoxFitError
{
    /// <summary>No fitting failure occurred.</summary>
    None = 0,

    /// <summary>A value, enum, viewport dimension, or object state was invalid.</summary>
    InvalidArgument = 1,

    /// <summary>The view box had a zero width or height and cannot define a transform.</summary>
    DegenerateViewBox = 2,

    /// <summary>A computed matrix component was not finite.</summary>
    NonFiniteResult = 3
}
