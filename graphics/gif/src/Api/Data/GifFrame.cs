namespace gif;

/// <summary>Represents one raw GIF image frame and the control metadata scoped to it.</summary>
/// <remarks>The frame rectangle may cover only part of the logical screen. <see cref="IndexedPixels"/> is row-major after decoding even when <see cref="Interlace"/> is set.</remarks>
public class GifFrame
{
    /// <summary>Contains the frame's horizontal offset from the logical screen's left edge.</summary>
    public int Left;
    /// <summary>Contains the frame's vertical offset from the logical screen's top edge.</summary>
    public int Top;
    /// <summary>Contains the frame rectangle width in pixels.</summary>
    public int Width;
    /// <summary>Contains the frame rectangle height in pixels.</summary>
    public int Height;
    /// <summary>Contains the GIF delay in hundredths of a second.</summary>
    public int DelayHundredths;
    /// <summary>Controls how this frame is removed before the next frame is rendered.</summary>
    public GifDisposalMethod DisposalMethod;
    /// <summary>Indicates whether <see cref="TransparentColorIndex"/> is active.</summary>
    public bool HasTransparency;
    /// <summary>Contains the palette index skipped while the frame is composited, or <c>-1</c> when absent.</summary>
    public int TransparentColorIndex;
    /// <summary>Preserves the Graphic Control Extension user-input flag.</summary>
    public bool UserInputFlag;
    /// <summary>Indicates whether compressed rows are stored in GIF's four-pass interlace order.</summary>
    public bool Interlace;
    /// <summary>Indicates whether this frame declares its own color table.</summary>
    public bool HasLocalColorTable;
    /// <summary>Preserves the advisory local color-table sort flag.</summary>
    public bool LocalColorTableSortFlag;
    /// <summary>Contains the declared number of entries in the local color table.</summary>
    public int LocalColorTableSize;
    /// <summary>Contains the raw three-bit local color-table size code.</summary>
    public int LocalColorTableSizeCode;
    /// <summary>Contains the palette used instead of the document's global palette.</summary>
    public GifPalette LocalColorTable;
    /// <summary>Contains the minimum LZW code size stored before the image-data sub-blocks.</summary>
    public int LzwMinimumCodeSize;
    /// <summary>Contains concatenated image-data sub-block payload bytes without length bytes or the terminator.</summary>
    public byte[] CompressedImageData;
    /// <summary>Contains decoded row-major palette indexes for the frame rectangle.</summary>
    public byte[] IndexedPixels;
    /// <summary>Contains the parsed Graphic Control Extension associated with this frame.</summary>
    public GifGraphicControl GraphicControl;
    /// <summary>Contains preserved unrecognized extensions scoped before this frame.</summary>
    public GifUnknownExtension[] UnknownExtensions;
    /// <summary>Contains recoverable conditions associated with this frame.</summary>
    public GifWarningDiagnostic[] Warnings;

    /// <summary>Creates an empty raw frame for the supplied logical-screen rectangle.</summary>
    /// <param name="left">The horizontal frame offset in pixels.</param>
    /// <param name="top">The vertical frame offset in pixels.</param>
    /// <param name="width">The frame width in pixels.</param>
    /// <param name="height">The frame height in pixels.</param>
    public GifFrame(int left, int top, int width, int height)
    {
        Left = left;
        Top = top;
        Width = width;
        Height = height;
        DelayHundredths = 0;
        DisposalMethod = GifDisposalMethod.Unspecified;
        HasTransparency = false;
        TransparentColorIndex = -1;
        UserInputFlag = false;
        Interlace = false;
        HasLocalColorTable = false;
        LocalColorTableSortFlag = false;
        LocalColorTableSize = 0;
        LocalColorTableSizeCode = 0;
        LocalColorTable = new GifPalette(null);
        LzwMinimumCodeSize = 0;
        CompressedImageData = new byte[0];
        IndexedPixels = new byte[0];
        GraphicControl = new GifGraphicControl();
        UnknownExtensions = new GifUnknownExtension[0];
        Warnings = new GifWarningDiagnostic[0];
    }
}
