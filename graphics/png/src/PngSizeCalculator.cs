namespace png;

internal class PngSizeCalculator
{
    internal static int GetSamplesPerPixel(PngColorType colorType)
    {
        switch (colorType)
        {
            case PngColorType.Grayscale:
                return 1;
            case PngColorType.Truecolor:
                return 3;
            case PngColorType.IndexedColor:
                return 1;
            case PngColorType.GrayscaleAlpha:
                return 2;
            case PngColorType.TruecolorAlpha:
                return 4;
            default:
                throw new ArgumentOutOfRangeException("colorType");
        }
    }

    internal static int GetBitDepthValue(PngBitDepth bitDepth)
    {
        switch (bitDepth)
        {
            case PngBitDepth.One:
                return 1;
            case PngBitDepth.Two:
                return 2;
            case PngBitDepth.Four:
                return 4;
            case PngBitDepth.Eight:
                return 8;
            case PngBitDepth.Sixteen:
                return 16;
            default:
                throw new ArgumentOutOfRangeException("bitDepth");
        }
    }

    internal static int GetBitsPerPixel(PngColorType colorType, PngBitDepth bitDepth)
    {
        return GetSamplesPerPixel(colorType) * GetBitDepthValue(bitDepth);
    }

    internal static int GetPixelFormatBitsPerPixel(PngPixelFormat pixelFormat)
    {
        switch (pixelFormat)
        {
            case PngPixelFormat.Grayscale1:
                return 1;
            case PngPixelFormat.Grayscale2:
                return 2;
            case PngPixelFormat.Grayscale4:
                return 4;
            case PngPixelFormat.Grayscale8:
                return 8;
            case PngPixelFormat.Grayscale16:
                return 16;
            case PngPixelFormat.Indexed1:
                return 1;
            case PngPixelFormat.Indexed2:
                return 2;
            case PngPixelFormat.Indexed4:
                return 4;
            case PngPixelFormat.Indexed8:
                return 8;
            case PngPixelFormat.Rgb8:
            case PngPixelFormat.Bgr8:
                return 24;
            case PngPixelFormat.Rgb16:
            case PngPixelFormat.Bgr16:
                return 48;
            case PngPixelFormat.GrayscaleAlpha8:
                return 16;
            case PngPixelFormat.GrayscaleAlpha16:
                return 32;
            case PngPixelFormat.Rgba8:
            case PngPixelFormat.Bgra8:
            case PngPixelFormat.Argb8:
                return 32;
            case PngPixelFormat.Rgba16:
            case PngPixelFormat.Bgra16:
            case PngPixelFormat.Argb16:
                return 64;
            default:
                throw new ArgumentOutOfRangeException("pixelFormat");
        }
    }

    internal static long CalculateRowBytes(int width, PngColorType colorType, PngBitDepth bitDepth)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException("width");
        }

        long bitsPerPixel = GetBitsPerPixel(colorType, bitDepth);
        long bitsPerRow = (long)width * bitsPerPixel;
        return (bitsPerRow + 7) / 8;
    }

    internal static long CalculateImageBytes(int width, int height, PngColorType colorType, PngBitDepth bitDepth)
    {
        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException("height");
        }

        long rowBytes = CalculateRowBytes(width, colorType, bitDepth);
        if (rowBytes > long.MaxValue / height)
        {
            throw new OverflowException();
        }

        return rowBytes * height;
    }

    internal static long CalculateFrameRowBytes(int width, PngPixelFormat pixelFormat)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException("width");
        }

        long bitsPerPixel = GetPixelFormatBitsPerPixel(pixelFormat);
        long bitsPerRow = (long)width * bitsPerPixel;
        return (bitsPerRow + 7) / 8;
    }

    internal static long CalculateFrameBytes(int width, int height, PngPixelFormat pixelFormat)
    {
        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException("height");
        }

        long rowBytes = CalculateFrameRowBytes(width, pixelFormat);
        if (rowBytes > long.MaxValue / height)
        {
            throw new OverflowException();
        }

        return rowBytes * height;
    }

    internal static int CalculateChunkAllocation(uint length)
    {
        if (length > int.MaxValue)
        {
            throw new OverflowException();
        }

        return (int)length;
    }

    internal static int CalculateChunkAllocation(uint length, long limit)
    {
        if (limit > 0 && length > (ulong)limit)
        {
            throw new OverflowException();
        }

        return CalculateChunkAllocation(length);
    }
}
