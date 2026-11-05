namespace png;

internal class PngNonInterlacedImageEncoder
{
    internal static byte[] Encode(PngPixelBuffer buffer, PngIhdr ihdr)
    {
        return Encode(buffer, ihdr, new PngEncoderOptions());
    }

    internal static byte[] Encode(PngPixelBuffer buffer, PngIhdr ihdr, PngEncoderOptions options)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(ihdr);
        ArgumentNullException.ThrowIfNull(options);

        if (ihdr.InterlaceMethod != PngInterlaceMethod.None)
        {
            throw new ArgumentOutOfRangeException("InterlaceMethod");
        }

        PngScanlineLayout layout = PngScanlineLayoutCalculator.Calculate(ihdr);
        ValidateBuffer(buffer, ihdr, layout);

        if (layout.FilteredImageBytes > int.MaxValue || layout.RowDataBytes > int.MaxValue)
        {
            throw new OverflowException();
        }

        byte[] filteredScanlines = new byte[(int)layout.FilteredImageBytes];
        byte[] previousRow = new byte[0];
        bool hasPreviousRow = false;
        int destinationOffset = 0;
        int rowIndex = 0;
        while (rowIndex < ihdr.Height)
        {
            byte[] rowData = ReadRow(buffer, rowIndex, (int)layout.RowDataBytes);
            byte[] filteredRow;
            if (hasPreviousRow)
            {
                filteredRow = PngFilterEncoder.EncodeRow(rowData, layout.FilterBytesPerPixel, previousRow, options);
            }
            else
            {
                filteredRow = PngFilterEncoder.EncodeRow(rowData, layout.FilterBytesPerPixel, options);
            }

            CopyBytes(filteredRow, 0, filteredScanlines, destinationOffset, filteredRow.Length);
            previousRow = rowData;
            hasPreviousRow = true;
            destinationOffset += filteredRow.Length;
            rowIndex += 1;
        }

        return filteredScanlines;
    }

    internal static void ValidateBuffer(PngPixelBuffer buffer, PngIhdr ihdr, PngScanlineLayout layout)
    {
        ArgumentNullException.ThrowIfNull(buffer.Data);

        if (buffer.Width != ihdr.Width)
        {
            throw new ArgumentOutOfRangeException("Width");
        }

        if (buffer.Height != ihdr.Height)
        {
            throw new ArgumentOutOfRangeException("Height");
        }

        PngPixelFormat expectedFormat = PngNonInterlacedImageDecoder.GetNativePixelFormat(ihdr.ColorType, ihdr.BitDepth);
        if (buffer.Format != expectedFormat)
        {
            throw new ArgumentOutOfRangeException("Format");
        }

        if (layout.RowDataBytes > int.MaxValue)
        {
            throw new OverflowException();
        }

        if (buffer.Stride < (int)layout.RowDataBytes)
        {
            throw new ArgumentOutOfRangeException("Stride");
        }

        long requiredBytes = CalculateRequiredBytes(buffer.Stride, buffer.Height, (int)layout.RowDataBytes);
        if (requiredBytes > buffer.Data.Length)
        {
            throw new ArgumentOutOfRangeException("Data");
        }
    }

    internal static long CalculateRequiredBytes(int stride, int height, int rowDataBytes)
    {
        if (height == 1)
        {
            return rowDataBytes;
        }

        long leadingRows = (long)stride * (height - 1);
        return leadingRows + rowDataBytes;
    }

    internal static byte[] ReadRow(PngPixelBuffer buffer, int rowIndex, int rowDataBytes)
    {
        byte[] rowData = new byte[rowDataBytes];
        int sourceOffset = (int)((long)buffer.Stride * rowIndex);
        CopyBytes(buffer.Data, sourceOffset, rowData, 0, rowDataBytes);
        return rowData;
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
