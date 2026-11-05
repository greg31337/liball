namespace qrcode;

/// <summary>Contains rendered eight-bit grayscale pixels.</summary>
public class QrRaster
{
    /// <summary>The row-major grayscale pixel bytes.</summary>
    public byte[] Pixels = new byte[0];

    /// <summary>The raster width in pixels.</summary>
    public int Width;

    /// <summary>The raster height in pixels.</summary>
    public int Height;

    /// <summary>The number of bytes from one row start to the next.</summary>
    public int Stride;
}
