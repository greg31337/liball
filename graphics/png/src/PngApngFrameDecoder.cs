namespace png;

internal class PngApngFrameDecoder
{
    internal static PngPixelBuffer Decode(PngFrameControl frameControl, PngIhdr ihdr, byte[] compressedFrameData)
    {
        return Decode(frameControl, ihdr, compressedFrameData, new PngSecurityLimits());
    }

    internal static PngPixelBuffer Decode(PngFrameControl frameControl, PngIhdr ihdr, byte[] compressedFrameData, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(frameControl);
        ArgumentNullException.ThrowIfNull(ihdr);
        ArgumentNullException.ThrowIfNull(compressedFrameData);
        ArgumentNullException.ThrowIfNull(limits);

        PngIhdr frameIhdr = CreateFrameIhdr(frameControl, ihdr);
        byte[] filteredScanlines = PngZlibDecompressor.Decompress(compressedFrameData, limits);
        return DecodeFilteredScanlines(filteredScanlines, frameIhdr);
    }

    internal static PngPixelBuffer Decode(PngFrameControl frameControl, PngIhdr ihdr, PngFrameData[] frameDataChunks)
    {
        return Decode(frameControl, ihdr, frameDataChunks, new PngSecurityLimits());
    }

    internal static PngPixelBuffer Decode(PngFrameControl frameControl, PngIhdr ihdr, PngFrameData[] frameDataChunks, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(frameControl);
        ArgumentNullException.ThrowIfNull(ihdr);
        ArgumentNullException.ThrowIfNull(frameDataChunks);
        ArgumentNullException.ThrowIfNull(limits);

        byte[] compressedFrameData = ConcatenateFrameData(frameDataChunks);
        return Decode(frameControl, ihdr, compressedFrameData, limits);
    }

    internal static byte[] ConcatenateFrameData(PngFrameData[] frameDataChunks)
    {
        ArgumentNullException.ThrowIfNull(frameDataChunks);

        int totalLength = CalculateFrameDataLength(frameDataChunks);
        byte[] data = new byte[totalLength];
        int offset = 0;
        int index = 0;
        while (index < frameDataChunks.Length)
        {
            PngFrameData frameData = frameDataChunks[index];
            ArgumentNullException.ThrowIfNull(frameData);
            ArgumentNullException.ThrowIfNull(frameData.Data);
            CopyBytes(frameData.Data, 0, data, offset, frameData.Data.Length);
            offset += frameData.Data.Length;
            index += 1;
        }

        return data;
    }

    internal static int CalculateFrameDataLength(PngFrameData[] frameDataChunks)
    {
        long totalLength = 0;
        int index = 0;
        while (index < frameDataChunks.Length)
        {
            PngFrameData frameData = frameDataChunks[index];
            ArgumentNullException.ThrowIfNull(frameData);
            ArgumentNullException.ThrowIfNull(frameData.Data);
            if (totalLength > int.MaxValue - frameData.Data.Length)
            {
                throw new OverflowException();
            }

            totalLength += frameData.Data.Length;
            index += 1;
        }

        return (int)totalLength;
    }

    internal static PngIhdr CreateFrameIhdr(PngFrameControl frameControl, PngIhdr ihdr)
    {
        PngIhdr frameIhdr = new PngIhdr();
        frameIhdr.Width = ConvertToInt(frameControl.Width, "Width");
        frameIhdr.Height = ConvertToInt(frameControl.Height, "Height");
        frameIhdr.BitDepth = ihdr.BitDepth;
        frameIhdr.ColorType = ihdr.ColorType;
        frameIhdr.CompressionMethod = ihdr.CompressionMethod;
        frameIhdr.FilterMethod = ihdr.FilterMethod;
        frameIhdr.InterlaceMethod = ihdr.InterlaceMethod;
        return frameIhdr;
    }

    internal static int ConvertToInt(uint value, string name)
    {
        if (value > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(name);
        }

        return (int)value;
    }

    internal static PngPixelBuffer DecodeFilteredScanlines(byte[] filteredScanlines, PngIhdr frameIhdr)
    {
        if (frameIhdr.InterlaceMethod == PngInterlaceMethod.None)
        {
            return PngNonInterlacedImageDecoder.Decode(filteredScanlines, frameIhdr);
        }

        if (frameIhdr.InterlaceMethod == PngInterlaceMethod.Adam7)
        {
            return DecodeAdam7(filteredScanlines, frameIhdr);
        }

        throw new ArgumentOutOfRangeException("InterlaceMethod");
    }

    internal static PngPixelBuffer DecodeAdam7(byte[] filteredScanlines, PngIhdr frameIhdr)
    {
        byte[] imageData = PngAdam7Decoder.Decode(filteredScanlines, frameIhdr);
        PngScanlineLayout layout = PngScanlineLayoutCalculator.Calculate(frameIhdr);
        if (layout.RowDataBytes > int.MaxValue)
        {
            throw new OverflowException();
        }

        PngPixelBuffer buffer = new PngPixelBuffer();
        buffer.Format = PngNonInterlacedImageDecoder.GetNativePixelFormat(frameIhdr.ColorType, frameIhdr.BitDepth);
        buffer.Width = frameIhdr.Width;
        buffer.Height = frameIhdr.Height;
        buffer.Stride = (int)layout.RowDataBytes;
        buffer.Data = imageData;
        return buffer;
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
