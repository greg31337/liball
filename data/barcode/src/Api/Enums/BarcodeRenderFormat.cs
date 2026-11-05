namespace barcode;

/// <summary>
/// Identifies a rendered barcode output format.
/// </summary>
public enum BarcodeRenderFormat
{
    /// <summary>
    /// Produces packed one-bit raster data.
    /// </summary>
    Raw1 = 0,

    /// <summary>
    /// Produces eight-bit grayscale raster data.
    /// </summary>
    Gray8 = 1,

    /// <summary>
    /// Produces a Portable Network Graphics image.
    /// </summary>
    Png = 2,

    /// <summary>
    /// Produces a bitmap image file.
    /// </summary>
    Bmp = 3,

    /// <summary>
    /// Produces a Scalable Vector Graphics document.
    /// </summary>
    Svg = 4
}
