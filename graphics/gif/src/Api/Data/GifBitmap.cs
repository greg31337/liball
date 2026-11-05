namespace gif;

/// <summary>Stores either RGBA32 pixels or indexed pixels and a palette for a rectangular image.</summary>
/// <remarks>RGBA bytes are ordered red, green, blue, alpha. Indexed pixels contain one palette index per pixel. The supplied arrays are not cloned.</remarks>
public class GifBitmap
{
    /// <summary>Contains the bitmap width in pixels.</summary>
    public int Width;
    /// <summary>Contains the bitmap height in pixels.</summary>
    public int Height;
    /// <summary>Contains the number of bytes from one row start to the next: width times four for RGBA or width for indexed data.</summary>
    public int Stride;
    /// <summary>Contains row-major RGBA32 bytes when <see cref="UsesIndexedPixels"/> is <c>false</c>.</summary>
    public byte[] RgbaPixels;
    /// <summary>Contains one row-major palette index per pixel when <see cref="UsesIndexedPixels"/> is <c>true</c>.</summary>
    public byte[] IndexedPixels;
    /// <summary>Contains the color table used by <see cref="IndexedPixels"/>.</summary>
    public GifPalette Palette;
    /// <summary>Indicates which pixel buffer represents the bitmap.</summary>
    public bool UsesIndexedPixels;

    /// <summary>Creates an RGBA32 bitmap over the supplied row-major byte array.</summary>
    /// <param name="width">The bitmap width in pixels.</param>
    /// <param name="height">The bitmap height in pixels.</param>
    /// <param name="rgbaPixels">Four bytes per pixel in red, green, blue, alpha order; <c>null</c> is normalized to an empty array.</param>
    public GifBitmap(int width, int height, byte[]? rgbaPixels)
    {
        Width = width;
        Height = height;
        Stride = CalculateRgbaStride(width);
        if (rgbaPixels == null)
        {
            RgbaPixels = new byte[0];
        }
        else
        {
            RgbaPixels = rgbaPixels;
        }

        IndexedPixels = new byte[0];
        Palette = new GifPalette(null);
        UsesIndexedPixels = false;
    }

    internal static GifBitmap CreateIndexed(int width, int height, byte[]? indexedPixels, GifPalette? palette)
    {
        GifBitmap bitmap = new GifBitmap(width, height, null);
        bitmap.Stride = width;
        if (indexedPixels == null)
        {
            bitmap.IndexedPixels = new byte[0];
        }
        else
        {
            bitmap.IndexedPixels = indexedPixels;
        }

        if (palette == null)
        {
            bitmap.Palette = new GifPalette(null);
        }
        else
        {
            bitmap.Palette = palette;
        }

        bitmap.UsesIndexedPixels = true;
        return bitmap;
    }

    internal static int CalculateRgbaStride(int width)
    {
        if (width > 536870911 || width < -536870912)
        {
            return 0;
        }

        return width * 4;
    }
}

/// <summary>Combines a bitmap with its playback timing, disposal, transparency, and source rectangle.</summary>
public class GifAnimationFrame
{
    /// <summary>Contains the frame pixels; decoded animations normally use a full-canvas RGBA bitmap.</summary>
    public GifBitmap Bitmap;
    /// <summary>Contains the display duration in milliseconds.</summary>
    public int DelayMilliseconds;
    /// <summary>Controls how this frame is removed before the next frame.</summary>
    public GifDisposalMethod DisposalMethod;
    /// <summary>Indicates whether <see cref="TransparentColorIndex"/> is active for indexed input.</summary>
    public bool HasTransparency;
    /// <summary>Contains the transparent palette index for indexed input, or <c>-1</c> when absent.</summary>
    public int TransparentColorIndex;
    /// <summary>Contains the horizontal destination offset used when the bitmap is a frame sub-rectangle.</summary>
    public int SourceLeft;
    /// <summary>Contains the vertical destination offset used when the bitmap is a frame sub-rectangle.</summary>
    public int SourceTop;
    /// <summary>Contains the source rectangle width; a zero value lets encoders use the bitmap width.</summary>
    public int SourceWidth;
    /// <summary>Contains the source rectangle height; a zero value lets encoders use the bitmap height.</summary>
    public int SourceHeight;
    /// <summary>Contains the frame's original or intended zero-based position in the animation.</summary>
    public int FrameIndex;

    /// <summary>Creates an empty animation frame with no transparency and unspecified disposal.</summary>
    public GifAnimationFrame()
    {
        Bitmap = new GifBitmap(0, 0, null);
        DelayMilliseconds = 0;
        DisposalMethod = GifDisposalMethod.Unspecified;
        HasTransparency = false;
        TransparentColorIndex = -1;
        SourceLeft = 0;
        SourceTop = 0;
        SourceWidth = 0;
        SourceHeight = 0;
        FrameIndex = -1;
    }
}
