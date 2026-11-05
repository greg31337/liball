namespace png;

internal class PngScanlineLayoutCalculator
{
    internal static PngScanlineLayout Calculate(PngIhdr ihdr)
    {
        ArgumentNullException.ThrowIfNull(ihdr);

        return Calculate(ihdr.Width, ihdr.Height, ihdr.ColorType, ihdr.BitDepth);
    }

    internal static PngScanlineLayout Calculate(int width, int height, PngColorType colorType, PngBitDepth bitDepth)
    {
        ValidateLayoutInputs(width, height, colorType, bitDepth);

        PngScanlineLayout layout = new PngScanlineLayout();
        layout.Width = width;
        layout.Height = height;
        layout.ColorType = colorType;
        layout.BitDepth = bitDepth;
        layout.SamplesPerPixel = PngSizeCalculator.GetSamplesPerPixel(colorType);
        layout.BitsPerSample = PngSizeCalculator.GetBitDepthValue(bitDepth);
        layout.BitsPerPixel = layout.SamplesPerPixel * layout.BitsPerSample;
        layout.FilterBytesPerPixel = CalculateFilterBytesPerPixel(layout.BitsPerPixel);
        layout.RowDataBytes = CalculateRowDataBytes(width, layout.BitsPerPixel);
        layout.FilteredRowBytes = CalculateFilteredRowBytes(layout.RowDataBytes);
        layout.ImageDataBytes = CalculateTotalBytes(layout.RowDataBytes, height);
        layout.FilteredImageBytes = CalculateTotalBytes(layout.FilteredRowBytes, height);
        return layout;
    }

    internal static void ValidateLayoutInputs(int width, int height, PngColorType colorType, PngBitDepth bitDepth)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException("width");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException("height");
        }

        if (!IsBitDepthAllowedForColorType(bitDepth, colorType))
        {
            throw new ArgumentOutOfRangeException("bitDepth");
        }
    }

    internal static bool IsBitDepthAllowedForColorType(PngBitDepth bitDepth, PngColorType colorType)
    {
        switch (colorType)
        {
            case PngColorType.Grayscale:
                return bitDepth == PngBitDepth.One
                    || bitDepth == PngBitDepth.Two
                    || bitDepth == PngBitDepth.Four
                    || bitDepth == PngBitDepth.Eight
                    || bitDepth == PngBitDepth.Sixteen;
            case PngColorType.Truecolor:
                return bitDepth == PngBitDepth.Eight || bitDepth == PngBitDepth.Sixteen;
            case PngColorType.IndexedColor:
                return bitDepth == PngBitDepth.One
                    || bitDepth == PngBitDepth.Two
                    || bitDepth == PngBitDepth.Four
                    || bitDepth == PngBitDepth.Eight;
            case PngColorType.GrayscaleAlpha:
                return bitDepth == PngBitDepth.Eight || bitDepth == PngBitDepth.Sixteen;
            case PngColorType.TruecolorAlpha:
                return bitDepth == PngBitDepth.Eight || bitDepth == PngBitDepth.Sixteen;
            default:
                throw new ArgumentOutOfRangeException("colorType");
        }
    }

    internal static int CalculateFilterBytesPerPixel(int bitsPerPixel)
    {
        int bytesPerPixel = (bitsPerPixel + 7) / 8;
        if (bytesPerPixel < 1)
        {
            return 1;
        }

        return bytesPerPixel;
    }

    internal static long CalculateRowDataBytes(int width, int bitsPerPixel)
    {
        long bitsPerRow = (long)width * bitsPerPixel;
        return (bitsPerRow + 7) / 8;
    }

    internal static long CalculateFilteredRowBytes(long rowDataBytes)
    {
        if (rowDataBytes == long.MaxValue)
        {
            throw new OverflowException();
        }

        return rowDataBytes + 1;
    }

    internal static long CalculateTotalBytes(long rowBytes, int height)
    {
        if (rowBytes > long.MaxValue / height)
        {
            throw new OverflowException();
        }

        return rowBytes * height;
    }
}
