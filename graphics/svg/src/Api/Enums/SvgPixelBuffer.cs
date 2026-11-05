namespace svg;

/// <summary>Defines the concrete RGB encoding carried by an RGBA8 pixel buffer.</summary>
public enum SvgPixelColorSpace
{
    /// <summary>Gamma-encoded standard RGB channels.</summary>
    Srgb = 0,
    /// <summary>Linear-light standard RGB channels.</summary>
    SrgbLinear = 1
}

/// <summary>Defines stable pixel-buffer allocation and operation failures.</summary>
public enum SvgPixelBufferError
{
    /// <summary>No pixel-buffer failure occurred.</summary>
    None = 0,
    /// <summary>An input, mutable buffer, rectangle, stride, or limit was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>A width, height, or pixel count exceeded configured bounds.</summary>
    DimensionLimitExceeded = 2,
    /// <summary>A required backing-store footprint exceeded configured or runtime bounds.</summary>
    ByteLimitExceeded = 3,
    /// <summary>A source or destination rectangle exceeded its buffer.</summary>
    BoundsExceeded = 4,
    /// <summary>A raw copy was requested between different concrete color spaces.</summary>
    ColorSpaceMismatch = 5,
    /// <summary>A pixel or wrapped byte sequence was not premultiplied by alpha.</summary>
    InvalidPremultipliedColor = 6
}
