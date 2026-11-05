namespace odf;

/// <summary>
/// Identifies an image format recognized from bounded signature inspection.
/// </summary>
public enum OdfImageFormat
{
    /// <summary>The image format is absent or unrecognized.</summary>
    Unknown = 0,
    /// <summary>Portable Network Graphics.</summary>
    Png = 1,
    /// <summary>JPEG image data.</summary>
    Jpeg = 2,
    /// <summary>Graphics Interchange Format.</summary>
    Gif = 3,
    /// <summary>Scalable Vector Graphics.</summary>
    Svg = 4,
    /// <summary>Windows bitmap image data.</summary>
    Bmp = 5,
    /// <summary>Tagged Image File Format.</summary>
    Tiff = 6,
    /// <summary>WebP image data.</summary>
    WebP = 7
}
