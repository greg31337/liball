namespace svg;

/// <summary>Identifies explicit decoded-image interpolation modes.</summary>
public enum SvgImageSamplingMode
{
    /// <summary>Uses the scene image-rendering hint.</summary>
    Auto = 0,
    /// <summary>Selects the containing source pixel.</summary>
    Nearest = 1,
    /// <summary>Interpolates the four neighboring premultiplied pixels.</summary>
    Bilinear = 2
}

/// <summary>Defines stable image sampling and drawing failures.</summary>
public enum SvgImageSampleError
{
    /// <summary>No image sampling failure occurred.</summary>
    None = 0,
    /// <summary>An image node, point, buffer, mapping, option, or mutable value was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The image resource had no decoded pixel buffer.</summary>
    MissingDecodedPixels = 2,
    /// <summary>An image or node transform could not be inverted.</summary>
    SingularTransform = 3,
    /// <summary>Mapping arithmetic produced a non-finite result.</summary>
    NonFiniteResult = 4,
    /// <summary>The source and destination concrete color spaces differed.</summary>
    ColorSpaceMismatch = 5,
    /// <summary>The clipped destination work exceeded its configured pixel limit.</summary>
    WorkLimitExceeded = 6
}
