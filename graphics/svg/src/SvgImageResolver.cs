namespace svg;

internal class SvgPngDecodeState
{
    internal byte[] Bytes;
    internal SvgRasterLimits Limits;
    internal SvgPngDecodeResult Result;
    internal int Width;
    internal int Height;
    internal int BitDepth;
    internal int ColorType;
    internal int InterlaceMethod;
    internal byte[]? Palette;
    internal byte[]? PaletteAlpha;
    internal bool HasTransparency;
    internal int TransparentGray;
    internal int TransparentRed;
    internal int TransparentGreen;
    internal int TransparentBlue;
    internal byte[]? Compressed;
    internal byte[]? Inflated;
    internal int RawOffset;
    internal SvgPixelBuffer? Pixels;

    internal SvgPngDecodeState(byte[] bytes, SvgRasterLimits limits, SvgPngDecodeResult result)
    {
        Bytes = bytes;
        Limits = limits;
        Result = result;
        Width = 0;
        Height = 0;
        BitDepth = 0;
        ColorType = 0;
        InterlaceMethod = 0;
        Palette = null;
        PaletteAlpha = null;
        HasTransparency = false;
        TransparentGray = -1;
        TransparentRed = -1;
        TransparentGreen = -1;
        TransparentBlue = -1;
        Compressed = null;
        Inflated = null;
        RawOffset = 0;
        Pixels = null;
    }
}

internal class SvgPngInflateResult
{
    internal bool Success;
    internal byte[]? Bytes;

    internal SvgPngInflateResult(bool success, byte[]? bytes)
    {
        Success = success;
        Bytes = bytes;
    }
}

internal class SvgPngCompressResult
{
    internal bool Success;
    internal byte[]? Bytes;

    internal SvgPngCompressResult(bool success, byte[]? bytes)
    {
        Success = success;
        Bytes = bytes;
    }
}
