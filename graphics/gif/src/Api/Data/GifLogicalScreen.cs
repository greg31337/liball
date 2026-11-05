namespace gif;

/// <summary>Describes the GIF logical canvas and its global color-table settings.</summary>
public class GifLogicalScreen
{
    /// <summary>Contains the logical canvas width in pixels.</summary>
    public int Width;
    /// <summary>Contains the logical canvas height in pixels.</summary>
    public int Height;
    /// <summary>Indicates whether the document carries a global color table.</summary>
    public bool HasGlobalColorTable;
    /// <summary>Contains the declared number of colors in the global table.</summary>
    public int GlobalColorTableSize;
    /// <summary>Contains the original image's color-resolution value from 1 through 8 bits per primary color.</summary>
    public int ColorResolution;
    /// <summary>Preserves the advisory global color-table sort flag.</summary>
    public bool SortFlag;
    /// <summary>Contains the palette index used as the logical-screen background.</summary>
    public int BackgroundColorIndex;
    /// <summary>Contains the raw GIF pixel-aspect-ratio byte; <c>0</c> means no aspect ratio is specified.</summary>
    public byte PixelAspectRatio;

    /// <summary>Creates a logical screen with the supplied dimensions and default display settings.</summary>
    /// <param name="width">The canvas width in pixels.</param>
    /// <param name="height">The canvas height in pixels.</param>
    public GifLogicalScreen(int width, int height)
    {
        Width = width;
        Height = height;
        HasGlobalColorTable = false;
        GlobalColorTableSize = 0;
        ColorResolution = 0;
        SortFlag = false;
        BackgroundColorIndex = 0;
        PixelAspectRatio = 0;
    }
}
