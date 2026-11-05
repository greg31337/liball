namespace png;

internal class PngAdam7Encoder
{
    internal static byte[] Encode(byte[] imageData, PngIhdr ihdr)
    {
        return Encode(imageData, 0, ihdr, new PngEncoderOptions());
    }

    internal static byte[] Encode(byte[] imageData, PngIhdr ihdr, PngEncoderOptions options)
    {
        return Encode(imageData, 0, ihdr, options);
    }

    internal static byte[] Encode(byte[] imageData, int offset, PngIhdr ihdr, PngEncoderOptions options)
    {
        ArgumentNullException.ThrowIfNull(imageData);
        ArgumentNullException.ThrowIfNull(ihdr);
        ArgumentNullException.ThrowIfNull(options);

        if (offset < 0 || offset > imageData.Length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (ihdr.InterlaceMethod != PngInterlaceMethod.Adam7)
        {
            throw new ArgumentOutOfRangeException("InterlaceMethod");
        }

        PngScanlineLayout finalLayout = PngScanlineLayoutCalculator.Calculate(ihdr);
        if (finalLayout.ImageDataBytes > int.MaxValue)
        {
            throw new OverflowException();
        }

        if ((int)finalLayout.ImageDataBytes != imageData.Length - offset)
        {
            throw new ArgumentOutOfRangeException("imageData");
        }

        long filteredByteCount = PngAdam7PassGeometryCalculator.CalculateFilteredImageBytes(ihdr);
        if (filteredByteCount > int.MaxValue)
        {
            throw new OverflowException();
        }

        byte[] filteredScanlines = new byte[(int)filteredByteCount];
        PngAdam7PassGeometry[] passes = PngAdam7PassGeometryCalculator.CalculatePasses(ihdr);

        int destinationOffset = 0;
        int passIndex = 0;
        while (passIndex < passes.Length)
        {
            destinationOffset = EncodePass(imageData, offset, finalLayout, passes[passIndex], options, filteredScanlines, destinationOffset);
            passIndex += 1;
        }

        return filteredScanlines;
    }

    internal static int EncodePass(byte[] imageData, int imageOffset, PngScanlineLayout finalLayout, PngAdam7PassGeometry pass, PngEncoderOptions options, byte[] filteredScanlines, int destinationOffset)
    {
        if (pass.IsEmpty)
        {
            return destinationOffset;
        }

        byte[] previousRow = new byte[0];
        bool hasPreviousRow = false;
        int passRow = 0;
        while (passRow < pass.Height)
        {
            byte[] rowData = GatherPassRow(imageData, imageOffset, finalLayout, pass, passRow);
            byte[] filteredRow;
            if (hasPreviousRow)
            {
                filteredRow = PngFilterEncoder.EncodeRow(rowData, pass.FilterBytesPerPixel, previousRow, options);
            }
            else
            {
                filteredRow = PngFilterEncoder.EncodeRow(rowData, pass.FilterBytesPerPixel, options);
            }

            CopyBytes(filteredRow, 0, filteredScanlines, destinationOffset, filteredRow.Length);
            previousRow = rowData;
            hasPreviousRow = true;
            destinationOffset += filteredRow.Length;
            passRow += 1;
        }

        return destinationOffset;
    }

    internal static byte[] GatherPassRow(byte[] imageData, int imageOffset, PngScanlineLayout finalLayout, PngAdam7PassGeometry pass, int passRow)
    {
        if (pass.BitDepth == PngBitDepth.One || pass.BitDepth == PngBitDepth.Two || pass.BitDepth == PngBitDepth.Four)
        {
            return GatherPackedPassRow(imageData, imageOffset, finalLayout, pass, passRow);
        }

        return GatherByteAlignedPassRow(imageData, imageOffset, finalLayout, pass, passRow);
    }

    internal static byte[] GatherByteAlignedPassRow(byte[] imageData, int imageOffset, PngScanlineLayout finalLayout, PngAdam7PassGeometry pass, int passRow)
    {
        int pixelBytes = pass.BitsPerPixel / 8;
        byte[] rowData = new byte[(int)pass.RowDataBytes];
        int imageY = PngAdam7PassGeometryCalculator.GetPixelY(pass.PassIndex, passRow);
        long imageRowOffset = imageOffset + finalLayout.RowDataBytes * imageY;
        int passColumn = 0;
        while (passColumn < pass.Width)
        {
            int imageX = PngAdam7PassGeometryCalculator.GetPixelX(pass.PassIndex, passColumn);
            int sourceOffset = (int)(imageRowOffset + imageX * pixelBytes);
            int destinationOffset = passColumn * pixelBytes;
            CopyBytes(imageData, sourceOffset, rowData, destinationOffset, pixelBytes);
            passColumn += 1;
        }

        return rowData;
    }

    internal static byte[] GatherPackedPassRow(byte[] imageData, int imageOffset, PngScanlineLayout finalLayout, PngAdam7PassGeometry pass, int passRow)
    {
        byte[] samples = new byte[pass.Width];
        int imageY = PngAdam7PassGeometryCalculator.GetPixelY(pass.PassIndex, passRow);
        long imageRowOffset = imageOffset + finalLayout.RowDataBytes * imageY;
        int passColumn = 0;
        while (passColumn < pass.Width)
        {
            int imageX = PngAdam7PassGeometryCalculator.GetPixelX(pass.PassIndex, passColumn);
            samples[passColumn] = ReadPackedSample(imageData, imageRowOffset, imageX, pass.BitDepth);
            passColumn += 1;
        }

        return PngPackedSampleWriter.Pack(samples, 0, pass.Width, pass.BitDepth);
    }

    internal static byte ReadPackedSample(byte[] imageData, long rowOffset, int x, PngBitDepth bitDepth)
    {
        int bitsPerSample = PngSizeCalculator.GetBitDepthValue(bitDepth);
        int bitIndex = x * bitsPerSample;
        int byteIndex = (int)(rowOffset + bitIndex / 8);
        int bitOffset = bitIndex % 8;
        int shift = 8 - bitsPerSample - bitOffset;
        int mask = (1 << bitsPerSample) - 1;
        return (byte)((imageData[byteIndex] >> shift) & mask);
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
