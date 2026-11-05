namespace barcode;

/// <summary>
/// Holds a caller-owned raw pixel buffer and its image layout metadata.
/// </summary>
public class BarcodeImage
{
    /// <summary>
    /// The caller-owned pixel buffer.
    /// </summary>
    public byte[]? Pixels;

    /// <summary>
    /// The image width in pixels.
    /// </summary>
    public int Width;

    /// <summary>
    /// The image height in pixels.
    /// </summary>
    public int Height;

    /// <summary>
    /// The signed number of bytes between the starts of adjacent rows.
    /// </summary>
    public int Stride;

    /// <summary>
    /// The byte offset of the first logical row in <see cref="Pixels"/>.
    /// </summary>
    public int Offset;

    /// <summary>
    /// The byte layout of each pixel.
    /// </summary>
    public BarcodePixelFormat PixelFormat;

    /// <summary>
    /// The horizontal resolution in dots per inch, or zero when unknown.
    /// </summary>
    public double DpiX;

    /// <summary>
    /// The vertical resolution in dots per inch, or zero when unknown.
    /// </summary>
    public double DpiY;
}
