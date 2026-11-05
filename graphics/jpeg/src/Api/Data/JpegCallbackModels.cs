namespace jpeg;

/// <summary>Describes one tightly packed pixel row stored in a callback buffer that is reused between calls.</summary>
/// <remarks>Callbacks must consume decoded samples before returning and must not replace <c>Samples</c>. Encode callbacks fill the first <c>SampleCount</c> bytes.</remarks>
public class JpegRowBuffer
{
    /// <summary>Creates an empty row description with an initialized sample buffer.</summary>
    public JpegRowBuffer()
    {
    }

    /// <summary>The zero-based row index.</summary>
    public int RowIndex;
    /// <summary>The total number of rows in the image.</summary>
    public int RowCount;
    /// <summary>The row width in pixels.</summary>
    public int Width;
    /// <summary>The number of components represented by each pixel.</summary>
    public int ComponentCount;
    /// <summary>The packed pixel layout of the row samples.</summary>
    public JpegPixelFormat PixelFormat;
    /// <summary>The interpreted color space of the row samples.</summary>
    public JpegColorSpace ColorSpace;
    /// <summary>The number of meaningful bytes in <c>Samples</c>.</summary>
    public int SampleCount;
    /// <summary>The bounded row buffer reused for every callback in an operation.</summary>
    public byte[] Samples = new byte[0];
}

/// <summary>Describes a decoded image preview produced after one progressive scan.</summary>
public class JpegProgressivePreview
{
    /// <summary>Creates an empty progressive preview with an initialized image.</summary>
    public JpegProgressivePreview()
    {
    }

    /// <summary>The zero-based scan index that produced this preview.</summary>
    public int ScanIndex;
    /// <summary>The total number of scans in the input.</summary>
    public int ScanCount;
    /// <summary>Whether this preview follows the final scan.</summary>
    public bool Completed;
    /// <summary>The decoded pixels available after the current scan.</summary>
    public JpegImage Image = new JpegImage();
}
