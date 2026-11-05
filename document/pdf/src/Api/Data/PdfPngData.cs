namespace pdf;

/// <summary>
/// Contains validated dimensions, format information, transparency, and resolution metadata loaded from a PNG image.
/// </summary>
public class PdfPngData
{
    /// <summary>
    /// Stores the pixel width declared by the PNG header.
    /// </summary>
    public int Width;
    /// <summary>
    /// Stores the pixel height declared by the PNG header.
    /// </summary>
    public int Height;
    /// <summary>
    /// Stores the source sample bit depth declared by the PNG header.
    /// </summary>
    public int BitDepth;
    /// <summary>
    /// Stores the baseline source color type declared by the PNG header.
    /// </summary>
    public PdfPngColorType ColorType;
    /// <summary>
    /// Stores the PDF device color space produced by PNG decoding.
    /// </summary>
    public PdfPngColorSpace ColorSpace;
    /// <summary>
    /// Indicates whether decoded transparency is represented by a soft mask.
    /// </summary>
    public bool HasAlpha;
    /// <summary>
    /// Indicates whether the PNG uses Adam7 interlacing.
    /// </summary>
    public bool IsInterlaced;
    /// <summary>
    /// Indicates whether physical resolution metadata was found.
    /// </summary>
    public bool HasDpi;
    /// <summary>
    /// Stores horizontal resolution in dots per inch, or zero when unavailable.
    /// </summary>
    public double HorizontalDpi;
    /// <summary>
    /// Stores vertical resolution in dots per inch, or zero when unavailable.
    /// </summary>
    public double VerticalDpi;
    internal int OutputBitsPerComponent;
    internal int AlphaBitsPerComponent;
    internal byte[] EncodedData;
    internal byte[] CompressedPixelData;
    internal byte[] CompressedAlphaData;

    internal PdfPngData(int width, int height, int bitDepth, PdfPngColorType colorType, PdfPngColorSpace colorSpace, bool hasAlpha, bool isInterlaced, bool hasDpi, double horizontalDpi, double verticalDpi, int outputBitsPerComponent, int alphaBitsPerComponent, byte[] encodedData, byte[] compressedPixelData, byte[] compressedAlphaData)
    {
        Width = width;
        Height = height;
        BitDepth = bitDepth;
        ColorType = colorType;
        ColorSpace = colorSpace;
        HasAlpha = hasAlpha;
        IsInterlaced = isInterlaced;
        HasDpi = hasDpi;
        HorizontalDpi = horizontalDpi;
        VerticalDpi = verticalDpi;
        OutputBitsPerComponent = outputBitsPerComponent;
        AlphaBitsPerComponent = alphaBitsPerComponent;
        EncodedData = encodedData;
        CompressedPixelData = compressedPixelData;
        CompressedAlphaData = compressedAlphaData;
    }
}
