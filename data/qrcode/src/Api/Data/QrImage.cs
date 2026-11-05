namespace qrcode;

/// <summary>Contains a caller-owned raw pixel buffer and its layout.</summary>
public class QrImage
{
    /// <summary>The raw pixel bytes.</summary>
    public byte[] Pixels = new byte[0];

    /// <summary>The image width in pixels.</summary>
    public int Width;

    /// <summary>The image height in pixels.</summary>
    public int Height;

    /// <summary>The number of bytes from one row start to the next.</summary>
    public int Stride;

    /// <summary>The storage format of <see cref="Pixels"/>.</summary>
    public QrPixelFormat PixelFormat;
}
