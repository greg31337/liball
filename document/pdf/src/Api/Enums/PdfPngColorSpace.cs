namespace pdf;

/// <summary>
/// Identifies the PDF device color space produced from a loaded PNG image.
/// </summary>
public enum PdfPngColorSpace
{
    /// <summary>
    /// Represents grayscale output samples.
    /// </summary>
    Grayscale = 0,
    /// <summary>
    /// Represents red, green, and blue output samples.
    /// </summary>
    Rgb = 1
}
