using System.IO;

namespace png;

internal class PngNonInterlacedImageDecoder
{
    internal static PngPixelBuffer Decode(byte[] filteredScanlines, PngIhdr ihdr)
    {
        return Decode(filteredScanlines, 0, ihdr);
    }

    internal static PngPixelBuffer Decode(byte[] filteredScanlines, int offset, PngIhdr ihdr)
    {
        ArgumentNullException.ThrowIfNull(filteredScanlines);
        ArgumentNullException.ThrowIfNull(ihdr);

        if (offset < 0 || offset > filteredScanlines.Length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (ihdr.InterlaceMethod != PngInterlaceMethod.None)
        {
            throw new ArgumentOutOfRangeException("InterlaceMethod");
        }

        PngScanlineLayout layout = PngScanlineLayoutCalculator.Calculate(ihdr);
        if (layout.FilteredImageBytes > int.MaxValue || layout.ImageDataBytes > int.MaxValue || layout.RowDataBytes > int.MaxValue)
        {
            throw new OverflowException();
        }

        if ((int)layout.FilteredImageBytes != filteredScanlines.Length - offset)
        {
            throw new ArgumentOutOfRangeException("filteredScanlines");
        }

        byte[] imageData = new byte[(int)layout.ImageDataBytes];
        byte[] previousRow = new byte[0];
        bool hasPreviousRow = false;
        int sourceOffset = offset;
        int destinationOffset = 0;
        int rowIndex = 0;
        while (rowIndex < layout.Height)
        {
            byte[] rowData = DecodeRow(filteredScanlines, sourceOffset, (int)layout.RowDataBytes, layout.FilterBytesPerPixel, previousRow, hasPreviousRow);
            CopyBytes(rowData, 0, imageData, destinationOffset, rowData.Length);
            previousRow = rowData;
            hasPreviousRow = true;
            sourceOffset += (int)layout.FilteredRowBytes;
            destinationOffset += (int)layout.RowDataBytes;
            rowIndex += 1;
        }

        PngPixelBuffer buffer = new PngPixelBuffer();
        buffer.Format = GetNativePixelFormat(ihdr.ColorType, ihdr.BitDepth);
        buffer.Width = ihdr.Width;
        buffer.Height = ihdr.Height;
        buffer.Stride = (int)layout.RowDataBytes;
        buffer.Data = imageData;
        return buffer;
    }

    internal static PngPixelFormat GetNativePixelFormat(PngColorType colorType, PngBitDepth bitDepth)
    {
        if (colorType == PngColorType.Grayscale)
        {
            return GetNativeGrayscalePixelFormat(bitDepth);
        }

        if (colorType == PngColorType.IndexedColor)
        {
            return GetNativeIndexedPixelFormat(bitDepth);
        }

        if (colorType == PngColorType.Truecolor)
        {
            if (bitDepth == PngBitDepth.Eight)
            {
                return PngPixelFormat.Rgb8;
            }

            if (bitDepth == PngBitDepth.Sixteen)
            {
                return PngPixelFormat.Rgb16;
            }

            throw new ArgumentOutOfRangeException("bitDepth");
        }

        if (colorType == PngColorType.GrayscaleAlpha)
        {
            if (bitDepth == PngBitDepth.Eight)
            {
                return PngPixelFormat.GrayscaleAlpha8;
            }

            if (bitDepth == PngBitDepth.Sixteen)
            {
                return PngPixelFormat.GrayscaleAlpha16;
            }

            throw new ArgumentOutOfRangeException("bitDepth");
        }

        if (colorType == PngColorType.TruecolorAlpha)
        {
            if (bitDepth == PngBitDepth.Eight)
            {
                return PngPixelFormat.Rgba8;
            }

            if (bitDepth == PngBitDepth.Sixteen)
            {
                return PngPixelFormat.Rgba16;
            }

            throw new ArgumentOutOfRangeException("bitDepth");
        }

        throw new ArgumentOutOfRangeException("colorType");
    }

    internal static PngPixelFormat GetNativeGrayscalePixelFormat(PngBitDepth bitDepth)
    {
        if (bitDepth == PngBitDepth.One)
        {
            return PngPixelFormat.Grayscale1;
        }

        if (bitDepth == PngBitDepth.Two)
        {
            return PngPixelFormat.Grayscale2;
        }

        if (bitDepth == PngBitDepth.Four)
        {
            return PngPixelFormat.Grayscale4;
        }

        if (bitDepth == PngBitDepth.Eight)
        {
            return PngPixelFormat.Grayscale8;
        }

        if (bitDepth == PngBitDepth.Sixteen)
        {
            return PngPixelFormat.Grayscale16;
        }

        throw new ArgumentOutOfRangeException("bitDepth");
    }

    internal static PngPixelFormat GetNativeIndexedPixelFormat(PngBitDepth bitDepth)
    {
        if (bitDepth == PngBitDepth.One)
        {
            return PngPixelFormat.Indexed1;
        }

        if (bitDepth == PngBitDepth.Two)
        {
            return PngPixelFormat.Indexed2;
        }

        if (bitDepth == PngBitDepth.Four)
        {
            return PngPixelFormat.Indexed4;
        }

        if (bitDepth == PngBitDepth.Eight)
        {
            return PngPixelFormat.Indexed8;
        }

        throw new ArgumentOutOfRangeException("bitDepth");
    }

    internal static byte[] DecodeRow(byte[] filteredScanlines, int offset, int rowDataBytes, int bytesPerPixel, byte[] previousRow, bool hasPreviousRow)
    {
        int filterType = filteredScanlines[offset];
        if (filterType == PngConstants.FilterTypeNone)
        {
            return PngFilterDecoder.DecodeNone(filteredScanlines, offset, rowDataBytes);
        }

        if (filterType == PngConstants.FilterTypeSub)
        {
            return PngFilterDecoder.DecodeSub(filteredScanlines, offset, rowDataBytes, bytesPerPixel);
        }

        if (filterType == PngConstants.FilterTypeUp)
        {
            if (hasPreviousRow)
            {
                return PngFilterDecoder.DecodeUp(filteredScanlines, offset, rowDataBytes, previousRow);
            }

            return PngFilterDecoder.DecodeUp(filteredScanlines, offset, rowDataBytes);
        }

        if (filterType == PngConstants.FilterTypeAverage)
        {
            if (hasPreviousRow)
            {
                return PngFilterDecoder.DecodeAverage(filteredScanlines, offset, rowDataBytes, bytesPerPixel, previousRow);
            }

            return PngFilterDecoder.DecodeAverage(filteredScanlines, offset, rowDataBytes, bytesPerPixel);
        }

        if (filterType == PngConstants.FilterTypePaeth)
        {
            if (hasPreviousRow)
            {
                return PngFilterDecoder.DecodePaeth(filteredScanlines, offset, rowDataBytes, bytesPerPixel, previousRow);
            }

            return PngFilterDecoder.DecodePaeth(filteredScanlines, offset, rowDataBytes, bytesPerPixel);
        }

        throw new InvalidDataException("PNG row filter type must be 0 through 4.");
    }

    internal static void CopyBytes(byte[] source, int sourceOffset, byte[] destination, int destinationOffset, int length)
    {
        int index = 0;
        while (index < length)
        {
            destination[destinationOffset + index] = source[sourceOffset + index];
            index += 1;
        }
    }
}
