namespace tiff;

/// <summary>Defines one writer image without requiring callers to construct structural TIFF tags.</summary>
public class TiffWriterImageConfiguration
{
    /// <summary>Contains the image width in pixels.</summary>
    public int Width;
    /// <summary>Contains the image length in rows.</summary>
    public int Length;
    /// <summary>Contains the image depth in slices.</summary>
    public int Depth;
    /// <summary>Describes the source pixel samples.</summary>
    public TiffWriterPixelFormat PixelFormat;
    /// <summary>Controls strip or tile organization.</summary>
    public TiffWriterLayoutConfiguration Layout;
    /// <summary>Specifies the compression of segment bytes supplied to the writer.</summary>
    public TiffCompression Compression;
    /// <summary>Specifies the TIFF predictor value: 1 for none, 2 for horizontal integer differencing, or 3 for floating-point differencing.</summary>
    public int Predictor;
    /// <summary>Specifies file byte order, or <see cref="TiffByteOrder.Unknown"/> to inherit the writer setting.</summary>
    public TiffByteOrder ByteOrder;

    /// <summary>Creates an empty image configuration with normalized pixel, layout, compression, and depth defaults.</summary>
    public TiffWriterImageConfiguration()
    {
        Width = 0;
        Length = 0;
        Depth = 1;
        PixelFormat = new TiffWriterPixelFormat();
        Layout = new TiffWriterLayoutConfiguration();
        Compression = TiffCompression.None;
        Predictor = 1;
        ByteOrder = TiffByteOrder.Unknown;
    }
}
