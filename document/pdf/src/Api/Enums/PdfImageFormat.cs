namespace pdf;

/// <summary>
/// Identifies the source representation used to create a PDF image resource.
/// </summary>
public enum PdfImageFormat
{
    /// <summary>
    /// Represents packed eight-bit grayscale samples.
    /// </summary>
    Grayscale = 0,
    /// <summary>
    /// Represents packed eight-bit red, green, and blue samples.
    /// </summary>
    Rgb = 1,
    /// <summary>
    /// Represents packed eight-bit red, green, blue, and alpha samples.
    /// </summary>
    Rgba = 2,
    /// <summary>
    /// Represents an encoded JPEG source.
    /// </summary>
    Jpeg = 3,
    /// <summary>
    /// Represents an encoded PNG source.
    /// </summary>
    Png = 4
}
