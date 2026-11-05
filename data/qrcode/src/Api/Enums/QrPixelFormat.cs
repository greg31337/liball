namespace qrcode;

/// <summary>Identifies the storage layout of caller-supplied raster pixels.</summary>
public enum QrPixelFormat
{
    /// <summary>One eight-bit grayscale sample per pixel.</summary>
    Gray8 = 0,

    /// <summary>Three bytes per pixel in red, green, blue order.</summary>
    Rgb24 = 1,

    /// <summary>Four bytes per pixel in red, green, blue, alpha order.</summary>
    Rgba32 = 2,

    /// <summary>Four bytes per pixel in blue, green, red, alpha order.</summary>
    Bgra32 = 3
}
