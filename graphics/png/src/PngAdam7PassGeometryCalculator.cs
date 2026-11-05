namespace png;

internal class PngAdam7PassGeometryCalculator
{
    internal const int PassCount = 7;

    internal static PngAdam7PassGeometry[] CalculatePasses(PngIhdr ihdr)
    {
        ArgumentNullException.ThrowIfNull(ihdr);

        return CalculatePasses(ihdr.Width, ihdr.Height, ihdr.ColorType, ihdr.BitDepth);
    }

    internal static PngAdam7PassGeometry[] CalculatePasses(int width, int height, PngColorType colorType, PngBitDepth bitDepth)
    {
        PngAdam7PassGeometry[] passes = new PngAdam7PassGeometry[PassCount];
        int passIndex = 1;
        while (passIndex <= PassCount)
        {
            passes[passIndex - 1] = CalculatePass(width, height, colorType, bitDepth, passIndex);
            passIndex += 1;
        }

        return passes;
    }

    internal static PngAdam7PassGeometry CalculatePass(PngIhdr ihdr, int passIndex)
    {
        ArgumentNullException.ThrowIfNull(ihdr);

        return CalculatePass(ihdr.Width, ihdr.Height, ihdr.ColorType, ihdr.BitDepth, passIndex);
    }

    internal static PngAdam7PassGeometry CalculatePass(int width, int height, PngColorType colorType, PngBitDepth bitDepth, int passIndex)
    {
        ValidatePassIndex(passIndex);
        PngScanlineLayout baseLayout = PngScanlineLayoutCalculator.Calculate(width, height, colorType, bitDepth);

        PngAdam7PassGeometry pass = new PngAdam7PassGeometry();
        pass.PassIndex = passIndex;
        pass.XStart = GetXStart(passIndex);
        pass.YStart = GetYStart(passIndex);
        pass.XStep = GetXStep(passIndex);
        pass.YStep = GetYStep(passIndex);
        pass.Width = CalculatePassDimension(width, pass.XStart, pass.XStep);
        pass.Height = CalculatePassDimension(height, pass.YStart, pass.YStep);
        pass.ColorType = colorType;
        pass.BitDepth = bitDepth;
        pass.BitsPerPixel = baseLayout.BitsPerPixel;
        pass.FilterBytesPerPixel = baseLayout.FilterBytesPerPixel;
        pass.IsEmpty = pass.Width == 0 || pass.Height == 0;
        CalculateByteCounts(pass);
        return pass;
    }

    internal static long CalculateFilteredImageBytes(PngIhdr ihdr)
    {
        ArgumentNullException.ThrowIfNull(ihdr);

        return CalculateFilteredImageBytes(ihdr.Width, ihdr.Height, ihdr.ColorType, ihdr.BitDepth);
    }

    internal static long CalculateFilteredImageBytes(int width, int height, PngColorType colorType, PngBitDepth bitDepth)
    {
        PngAdam7PassGeometry[] passes = CalculatePasses(width, height, colorType, bitDepth);
        long totalBytes = 0;
        int index = 0;
        while (index < passes.Length)
        {
            if (passes[index].FilteredImageBytes > long.MaxValue - totalBytes)
            {
                throw new OverflowException();
            }

            totalBytes += passes[index].FilteredImageBytes;
            index += 1;
        }

        return totalBytes;
    }

    internal static int GetPixelX(int passIndex, int passColumn)
    {
        ValidatePassIndex(passIndex);
        if (passColumn < 0)
        {
            throw new ArgumentOutOfRangeException("passColumn");
        }

        return MapPassCoordinate(GetXStart(passIndex), GetXStep(passIndex), passColumn);
    }

    internal static int GetPixelY(int passIndex, int passRow)
    {
        ValidatePassIndex(passIndex);
        if (passRow < 0)
        {
            throw new ArgumentOutOfRangeException("passRow");
        }

        return MapPassCoordinate(GetYStart(passIndex), GetYStep(passIndex), passRow);
    }

    internal static void CalculateByteCounts(PngAdam7PassGeometry pass)
    {
        if (pass.IsEmpty)
        {
            pass.RowDataBytes = 0;
            pass.FilteredRowBytes = 0;
            pass.ImageDataBytes = 0;
            pass.FilteredImageBytes = 0;
            return;
        }

        long bitsPerRow = (long)pass.Width * pass.BitsPerPixel;
        pass.RowDataBytes = (bitsPerRow + 7) / 8;
        pass.FilteredRowBytes = pass.RowDataBytes + 1;
        pass.ImageDataBytes = CalculateTotalBytes(pass.RowDataBytes, pass.Height);
        pass.FilteredImageBytes = CalculateTotalBytes(pass.FilteredRowBytes, pass.Height);
    }

    internal static int CalculatePassDimension(int dimension, int start, int step)
    {
        if (dimension <= start)
        {
            return 0;
        }

        long remaining = dimension - start;
        return (int)((remaining + step - 1) / step);
    }

    internal static int MapPassCoordinate(int start, int step, int coordinate)
    {
        if (coordinate > (int.MaxValue - start) / step)
        {
            throw new OverflowException();
        }

        return start + coordinate * step;
    }

    internal static long CalculateTotalBytes(long rowBytes, int height)
    {
        if (rowBytes > long.MaxValue / height)
        {
            throw new OverflowException();
        }

        return rowBytes * height;
    }

    internal static void ValidatePassIndex(int passIndex)
    {
        if (passIndex < 1 || passIndex > PassCount)
        {
            throw new ArgumentOutOfRangeException("passIndex");
        }
    }

    internal static int GetXStart(int passIndex)
    {
        switch (passIndex)
        {
            case 1:
                return 0;
            case 2:
                return 4;
            case 3:
                return 0;
            case 4:
                return 2;
            case 5:
                return 0;
            case 6:
                return 1;
            default:
                return 0;
        }
    }

    internal static int GetYStart(int passIndex)
    {
        switch (passIndex)
        {
            case 1:
                return 0;
            case 2:
                return 0;
            case 3:
                return 4;
            case 4:
                return 0;
            case 5:
                return 2;
            case 6:
                return 0;
            default:
                return 1;
        }
    }

    internal static int GetXStep(int passIndex)
    {
        switch (passIndex)
        {
            case 1:
                return 8;
            case 2:
                return 8;
            case 3:
                return 4;
            case 4:
                return 4;
            case 5:
                return 2;
            case 6:
                return 2;
            default:
                return 1;
        }
    }

    internal static int GetYStep(int passIndex)
    {
        switch (passIndex)
        {
            case 1:
                return 8;
            case 2:
                return 8;
            case 3:
                return 8;
            case 4:
                return 4;
            case 5:
                return 4;
            case 6:
                return 2;
            default:
                return 2;
        }
    }
}
