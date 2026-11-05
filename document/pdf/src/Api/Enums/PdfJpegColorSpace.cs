namespace pdf;

/// <summary>
/// Identifies the device color space used by a loaded JPEG image.
/// </summary>
public enum PdfJpegColorSpace
{
    /// <summary>
    /// Represents a single grayscale component.
    /// </summary>
    Grayscale = 0,
    /// <summary>
    /// Represents three red, green, and blue components after JPEG decoding.
    /// </summary>
    Rgb = 1,
    /// <summary>
    /// Represents four cyan, magenta, yellow, and black components after JPEG decoding.
    /// </summary>
    Cmyk = 2
}
