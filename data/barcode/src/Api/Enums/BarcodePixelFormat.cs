namespace barcode;

/// <summary>
/// Identifies the byte layout of pixels in a barcode image.
/// </summary>
public enum BarcodePixelFormat
{
    /// <summary>
    /// Uses one eight-bit grayscale channel per pixel.
    /// </summary>
    Gray8 = 0,

    /// <summary>
    /// Uses red, green, and blue channels per pixel.
    /// </summary>
    Rgb24 = 1,

    /// <summary>
    /// Uses blue, green, and red channels per pixel.
    /// </summary>
    Bgr24 = 2,

    /// <summary>
    /// Uses red, green, blue, and alpha channels per pixel.
    /// </summary>
    Rgba32 = 3,

    /// <summary>
    /// Uses blue, green, red, and alpha channels per pixel.
    /// </summary>
    Bgra32 = 4
}
