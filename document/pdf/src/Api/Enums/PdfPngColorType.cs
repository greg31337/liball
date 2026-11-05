namespace pdf;

/// <summary>
/// Identifies the baseline color type declared by a loaded PNG image.
/// </summary>
public enum PdfPngColorType
{
    /// <summary>
    /// Represents grayscale samples without an alpha channel.
    /// </summary>
    Grayscale = 0,
    /// <summary>
    /// Represents red, green, and blue samples without an alpha channel.
    /// </summary>
    Truecolor = 2,
    /// <summary>
    /// Represents samples that index a color palette.
    /// </summary>
    Indexed = 3,
    /// <summary>
    /// Represents grayscale samples with an alpha channel.
    /// </summary>
    GrayscaleWithAlpha = 4,
    /// <summary>
    /// Represents red, green, blue, and alpha samples.
    /// </summary>
    TruecolorWithAlpha = 6
}
