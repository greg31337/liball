namespace pdf;

/// <summary>
/// Identifies the device color space used by a PDF image resource.
/// </summary>
public enum PdfImageColorSpace
{
    /// <summary>
    /// Represents grayscale samples.
    /// </summary>
    Grayscale = 0,
    /// <summary>
    /// Represents red, green, and blue samples.
    /// </summary>
    Rgb = 1,
    /// <summary>
    /// Represents cyan, magenta, yellow, and black samples.
    /// </summary>
    Cmyk = 2
}
