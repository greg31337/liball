namespace barcode;

/// <summary>
/// Holds rendered barcode bytes or text together with output dimensions and diagnostics.
/// </summary>
public class BarcodeRenderedImage
{
    /// <summary>
    /// The rendered binary or raster bytes when the selected format is byte based.
    /// </summary>
    public byte[]? Bytes;

    /// <summary>
    /// The rendered text when the selected format is textual.
    /// </summary>
    public string? Text;

    /// <summary>
    /// The media type of the rendered content.
    /// </summary>
    public string? MediaType;

    /// <summary>
    /// The exact rendered output format.
    /// </summary>
    public BarcodeRenderFormat Format;

    /// <summary>
    /// The output width in pixels.
    /// </summary>
    public int WidthPixels;

    /// <summary>
    /// The output height in pixels.
    /// </summary>
    public int HeightPixels;

    /// <summary>
    /// The number of bytes between adjacent raster rows, or zero for encoded and vector formats.
    /// </summary>
    public int Stride;

    /// <summary>
    /// The pixel format for raw raster output.
    /// </summary>
    public BarcodePixelFormat PixelFormat;

    /// <summary>
    /// The output width in millimetres.
    /// </summary>
    public double WidthMillimetres;

    /// <summary>
    /// The output height in millimetres.
    /// </summary>
    public double HeightMillimetres;

    /// <summary>
    /// The horizontal output resolution in dots per inch.
    /// </summary>
    public double DpiX;

    /// <summary>
    /// The vertical output resolution in dots per inch.
    /// </summary>
    public double DpiY;

    /// <summary>
    /// The complete resolved logical, raster, and physical size.
    /// </summary>
    public BarcodeSize? Size;

    /// <summary>
    /// The rendered human-readable text layout.
    /// </summary>
    public BarcodeTextLayout? TextLayout;

    /// <summary>
    /// The diagnostics produced while planning and rendering the output.
    /// </summary>
    public BarcodeDiagnostic[]? Diagnostics;
}
