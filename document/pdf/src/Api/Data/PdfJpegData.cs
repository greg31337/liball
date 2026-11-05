namespace pdf;

/// <summary>
/// Contains validated dimensions, color information, and resolution metadata loaded from a JPEG image.
/// </summary>
public class PdfJpegData
{
    /// <summary>
    /// Stores the pixel width declared by the JPEG frame.
    /// </summary>
    public int Width;
    /// <summary>
    /// Stores the pixel height declared by the JPEG frame.
    /// </summary>
    public int Height;
    /// <summary>
    /// Stores the number of image components declared by the JPEG frame.
    /// </summary>
    public int ComponentCount;
    /// <summary>
    /// Stores the number of bits used for each JPEG image component.
    /// </summary>
    public int BitsPerComponent;
    /// <summary>
    /// Stores the PDF device color space selected for the JPEG components.
    /// </summary>
    public PdfJpegColorSpace ColorSpace;
    /// <summary>
    /// Indicates whether physical resolution metadata was found.
    /// </summary>
    public bool HasDpi;
    /// <summary>
    /// Stores the horizontal resolution in dots per inch, or zero when unavailable.
    /// </summary>
    public double HorizontalDpi;
    /// <summary>
    /// Stores the vertical resolution in dots per inch, or zero when unavailable.
    /// </summary>
    public double VerticalDpi;
    /// <summary>
    /// Indicates whether an Adobe marker requires inverted CMYK sample decoding.
    /// </summary>
    public bool HasInvertedCmykSamples;
    internal byte[] EncodedData;

    internal PdfJpegData(int width, int height, int componentCount, int bitsPerComponent, PdfJpegColorSpace colorSpace, bool hasDpi, double horizontalDpi, double verticalDpi, bool hasInvertedCmykSamples, byte[] encodedData)
    {
        Width = width;
        Height = height;
        ComponentCount = componentCount;
        BitsPerComponent = bitsPerComponent;
        ColorSpace = colorSpace;
        HasDpi = hasDpi;
        HorizontalDpi = horizontalDpi;
        VerticalDpi = verticalDpi;
        HasInvertedCmykSamples = hasInvertedCmykSamples;
        EncodedData = encodedData;
    }
}
