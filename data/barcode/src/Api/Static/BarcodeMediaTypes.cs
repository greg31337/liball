namespace barcode;

/// <summary>
/// Provides media types for supported rendered barcode outputs.
/// </summary>
public static class BarcodeMediaTypes
{
    /// <summary>
    /// The Portable Network Graphics media type.
    /// </summary>
    public const string Png = "image/png";

    /// <summary>
    /// The bitmap image media type.
    /// </summary>
    public const string Bmp = "image/bmp";

    /// <summary>
    /// The Scalable Vector Graphics media type.
    /// </summary>
    public const string Svg = "image/svg+xml";

    /// <summary>
    /// The generic binary media type used for packed one-bit raster output.
    /// </summary>
    public const string Raw1 = "application/octet-stream";

    /// <summary>
    /// The generic binary media type used for raw eight-bit grayscale output.
    /// </summary>
    public const string Gray8 = "application/octet-stream";
}
