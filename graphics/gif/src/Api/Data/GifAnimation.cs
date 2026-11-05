namespace gif;

/// <summary>Represents an animation as display-ready bitmap frames and playback metadata.</summary>
/// <remarks>Animations returned by <see cref="GifCodec.DecodeAnimationBytes(byte[], GifDecodeOptions?)"/> contain full-canvas RGBA frames with disposal already applied between frames.</remarks>
public class GifAnimation
{
    /// <summary>Contains the logical animation width in pixels.</summary>
    public int Width;
    /// <summary>Contains the logical animation height in pixels.</summary>
    public int Height;
    /// <summary>Contains frames in playback order.</summary>
    public GifAnimationFrame[] Frames;
    /// <summary>Contains the repeat count: <c>-1</c> means absent, <c>0</c> means forever, and positive values are finite repeats.</summary>
    public int LoopCount;
    /// <summary>Contains the sum of all frame delays in milliseconds.</summary>
    public int TotalDurationMilliseconds;
    /// <summary>Contains recoverable conditions produced while decoding or transforming the animation.</summary>
    public GifWarningDiagnostic[] Warnings;
    /// <summary>Contains structured plain-text rendering blocks exposed during decoding; the library does not rasterize them into frames.</summary>
    public GifPlainTextExtension[] PlainTextGraphics;

    /// <summary>Creates an empty animation with no loop extension.</summary>
    public GifAnimation()
    {
        Width = 0;
        Height = 0;
        Frames = new GifAnimationFrame[0];
        LoopCount = -1;
        TotalDurationMilliseconds = 0;
        Warnings = new GifWarningDiagnostic[0];
        PlainTextGraphics = new GifPlainTextExtension[0];
    }
}
