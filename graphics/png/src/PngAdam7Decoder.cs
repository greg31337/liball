using System.IO;

namespace png;

internal class PngAdam7Decoder
{
    internal static byte[] Decode(byte[] filteredScanlines, PngIhdr ihdr)
    {
        return Decode(filteredScanlines, 0, ihdr);
    }

    internal static byte[] Decode(byte[] filteredScanlines, int offset, PngIhdr ihdr)
    {
        ArgumentNullException.ThrowIfNull(filteredScanlines);
        ArgumentNullException.ThrowIfNull(ihdr);

        if (offset < 0 || offset > filteredScanlines.Length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (ihdr.InterlaceMethod != PngInterlaceMethod.Adam7)
        {
            throw new ArgumentOutOfRangeException("InterlaceMethod");
        }

        long expectedBytes = PngAdam7PassGeometryCalculator.CalculateFilteredImageBytes(ihdr);
        if (expectedBytes > int.MaxValue)
        {
            throw new OverflowException();
        }

        if ((int)expectedBytes != filteredScanlines.Length - offset)
        {
            throw new ArgumentOutOfRangeException("filteredScanlines");
        }

        PngScanlineLayout finalLayout = PngScanlineLayoutCalculator.Calculate(ihdr);
        if (finalLayout.ImageDataBytes > int.MaxValue)
        {
            throw new OverflowException();
        }

        byte[] imageData = new byte[(int)finalLayout.ImageDataBytes];
        PngAdam7PassGeometry[] passes = PngAdam7PassGeometryCalculator.CalculatePasses(ihdr);

        int sourceOffset = offset;
        int passIndex = 0;
        while (passIndex < passes.Length)
        {
            sourceOffset = DecodePass(filteredScanlines, sourceOffset, passes[passIndex], finalLayout, imageData);
            passIndex += 1;
        }

        return imageData;
    }

    internal static int DecodePass(byte[] filteredScanlines, int sourceOffset, PngAdam7PassGeometry pass, PngScanlineLayout finalLayout, byte[] imageData)
    {
        if (pass.IsEmpty)
        {
            return sourceOffset;
        }

        byte[] previousRow = new byte[0];
        bool hasPreviousRow = false;
        int passRow = 0;
        while (passRow < pass.Height)
        {
            byte[] rowData = DecodePassRow(filteredScanlines, sourceOffset, (int)pass.RowDataBytes, pass.FilterBytesPerPixel, previousRow, hasPreviousRow);
            ScatterPassRow(rowData, pass, passRow, finalLayout, imageData);
            previousRow = rowData;
            hasPreviousRow = true;
            sourceOffset += (int)pass.FilteredRowBytes;
            passRow += 1;
        }

        return sourceOffset;
    }

    internal static byte[] DecodePassRow(byte[] filteredScanlines, int offset, int rowDataBytes, int bytesPerPixel, byte[] previousRow, bool hasPreviousRow)
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

    internal static void ScatterPassRow(byte[] rowData, PngAdam7PassGeometry pass, int passRow, PngScanlineLayout finalLayout, byte[] imageData)
    {
        if (pass.BitDepth == PngBitDepth.One || pass.BitDepth == PngBitDepth.Two || pass.BitDepth == PngBitDepth.Four)
        {
            ScatterPackedPassRow(rowData, pass, passRow, finalLayout, imageData);
            return;
        }

        ScatterByteAlignedPassRow(rowData, pass, passRow, finalLayout, imageData);
    }

    internal static void ScatterByteAlignedPassRow(byte[] rowData, PngAdam7PassGeometry pass, int passRow, PngScanlineLayout finalLayout, byte[] imageData)
    {
        int pixelBytes = pass.BitsPerPixel / 8;
        int imageY = PngAdam7PassGeometryCalculator.GetPixelY(pass.PassIndex, passRow);
        long imageRowOffset = finalLayout.RowDataBytes * imageY;
        int passColumn = 0;
        while (passColumn < pass.Width)
        {
            int imageX = PngAdam7PassGeometryCalculator.GetPixelX(pass.PassIndex, passColumn);
            int sourceOffset = passColumn * pixelBytes;
            int destinationOffset = (int)(imageRowOffset + imageX * pixelBytes);
            CopyPixel(rowData, sourceOffset, imageData, destinationOffset, pixelBytes);
            passColumn += 1;
        }
    }

    internal static void ScatterPackedPassRow(byte[] rowData, PngAdam7PassGeometry pass, int passRow, PngScanlineLayout finalLayout, byte[] imageData)
    {
        byte[] samples = PngPackedSampleUnpacker.Unpack(rowData, 0, pass.Width, pass.BitDepth);
        int imageY = PngAdam7PassGeometryCalculator.GetPixelY(pass.PassIndex, passRow);
        long imageRowOffset = finalLayout.RowDataBytes * imageY;
        int passColumn = 0;
        while (passColumn < pass.Width)
        {
            int imageX = PngAdam7PassGeometryCalculator.GetPixelX(pass.PassIndex, passColumn);
            WritePackedSample(imageData, imageRowOffset, imageX, samples[passColumn], pass.BitDepth);
            passColumn += 1;
        }
    }

    internal static void WritePackedSample(byte[] imageData, long rowOffset, int x, byte sample, PngBitDepth bitDepth)
    {
        int bitsPerSample = PngSizeCalculator.GetBitDepthValue(bitDepth);
        int bitIndex = x * bitsPerSample;
        int byteIndex = (int)(rowOffset + bitIndex / 8);
        int bitOffset = bitIndex % 8;
        int shift = 8 - bitsPerSample - bitOffset;
        int mask = ((1 << bitsPerSample) - 1) << shift;
        int value = imageData[byteIndex] & (255 - mask);
        imageData[byteIndex] = (byte)(value | (sample << shift));
    }

    internal static void CopyPixel(byte[] source, int sourceOffset, byte[] destination, int destinationOffset, int pixelBytes)
    {
        int index = 0;
        while (index < pixelBytes)
        {
            destination[destinationOffset + index] = source[sourceOffset + index];
            index += 1;
        }
    }
}
