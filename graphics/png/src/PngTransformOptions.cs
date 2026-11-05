namespace png;

internal class PngTransformOptions
{
    internal PngPixelFormat OutputPixelFormat = PngPixelFormat.Rgba8;
    internal bool ExpandPalette;
    internal bool ExpandTransparency;
    internal bool StripAlpha;
    internal bool AddOpaqueAlpha;
    internal bool PremultiplyAlpha;
    internal bool UnpremultiplyAlpha;
    internal bool ClearTransparentPixels;
    internal bool InvertAlpha;
    internal bool InvertMonochrome;
    internal bool SwapRgbBgr;
    internal bool SwapRgbaArgb;
    internal bool ByteSwap16BitSamples;
    internal bool FlipVertical;
    internal bool Crop;
    internal int CropX;
    internal int CropY;
    internal int CropWidth;
    internal int CropHeight;
    internal bool PadCanvas;
    internal int PadLeft;
    internal int PadTop;
    internal int PadRight;
    internal int PadBottom;
    internal bool ResizeNearestNeighbor;
    internal bool ResizeBilinear;
    internal int ResizeWidth;
    internal int ResizeHeight;
}
