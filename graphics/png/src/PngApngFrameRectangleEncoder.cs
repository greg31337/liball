namespace png;

internal class PngApngFrameRectangleEncoder
{
    internal static PngChunk[] Encode(PngIhdr ihdr, PngAnimation animation)
    {
        return Encode(ihdr, animation, new PngEncoderOptions());
    }

    internal static PngChunk[] Encode(PngIhdr ihdr, PngAnimation animation, PngEncoderOptions options)
    {
        ArgumentNullException.ThrowIfNull(ihdr);
        ArgumentNullException.ThrowIfNull(animation);
        ArgumentNullException.ThrowIfNull(animation.Frames);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Limits);

        PngApngAnimationLimitEnforcer.Enforce(animation, ihdr, options.Limits);

        PngChunk[] chunks = new PngChunk[1 + animation.Frames.Length * 2];
        chunks[0] = PngAnimationControlWriter.WriteChunk(CreateAnimationControl(animation));

        uint sequenceNumber = 0;
        int chunkIndex = 1;
        int frameIndex = 0;
        while (frameIndex < animation.Frames.Length)
        {
            PngFrame frame = animation.Frames[frameIndex];
            ArgumentNullException.ThrowIfNull(frame);
            PngFrame encodedFrame = CreateEncodedFrame(frame, ihdr, frameIndex);
            chunks[chunkIndex] = PngFrameControlWriter.WriteChunk(CreateFrameControl(encodedFrame, sequenceNumber));
            chunkIndex += 1;
            sequenceNumber += 1;

            PngIhdr frameIhdr = CreateFrameIhdr(ihdr, encodedFrame.Width, encodedFrame.Height);
            byte[] compressedFrameData = EncodeFramePixels(encodedFrame.Pixels, frameIhdr, options);
            if (frameIndex == 0)
            {
                chunks[chunkIndex] = CreateIdatChunk(compressedFrameData);
            }
            else
            {
                chunks[chunkIndex] = PngFrameDataWriter.WriteChunk(CreateFrameData(sequenceNumber, compressedFrameData));
                sequenceNumber += 1;
            }

            chunkIndex += 1;
            frameIndex += 1;
        }

        return chunks;
    }

    internal static PngAnimationControl CreateAnimationControl(PngAnimation animation)
    {
        PngAnimationControl control = new PngAnimationControl();
        control.FrameCount = ConvertToUInt(animation.Frames.Length, "FrameCount");
        control.PlayCount = ConvertToUInt(animation.PlayCount, "PlayCount");
        return control;
    }

    internal static PngFrame CreateEncodedFrame(PngFrame frame, PngIhdr ihdr, int frameIndex)
    {
        ArgumentNullException.ThrowIfNull(frame.Pixels);

        int frameWidth = GetFrameDimension(frame.Width, frame.Pixels.Width);
        int frameHeight = GetFrameDimension(frame.Height, frame.Pixels.Height);
        if (frameIndex == 0)
        {
            ValidateDefaultFrame(frameWidth, frameHeight, frame.XOffset, frame.YOffset, ihdr);
        }
        else
        {
            ValidateFrameRectangle(frameWidth, frameHeight, frame.XOffset, frame.YOffset, ihdr);
        }

        PngFrame encodedFrame = new PngFrame();
        encodedFrame.Width = frameWidth;
        encodedFrame.Height = frameHeight;
        encodedFrame.XOffset = frame.XOffset;
        encodedFrame.YOffset = frame.YOffset;
        encodedFrame.DelayNumerator = frame.DelayNumerator;
        encodedFrame.DelayDenominator = frame.DelayDenominator;
        encodedFrame.DisposeOperation = frame.DisposeOperation;
        encodedFrame.BlendOperation = frame.BlendOperation;
        encodedFrame.Pixels = frame.Pixels;
        encodedFrame.UsesDefaultImage = frameIndex == 0 && frame.UsesDefaultImage;
        return encodedFrame;
    }

    internal static int GetFrameDimension(int frameDimension, int pixelDimension)
    {
        if (frameDimension == 0)
        {
            return pixelDimension;
        }

        return frameDimension;
    }

    internal static PngFrameControl CreateFrameControl(PngFrame frame, uint sequenceNumber)
    {
        PngFrameControl control = new PngFrameControl();
        control.SequenceNumber = sequenceNumber;
        control.Width = ConvertToUInt(frame.Width, "Width");
        control.Height = ConvertToUInt(frame.Height, "Height");
        control.XOffset = ConvertToUInt(frame.XOffset, "XOffset");
        control.YOffset = ConvertToUInt(frame.YOffset, "YOffset");
        control.DelayNumerator = ConvertToUShort(frame.DelayNumerator, "DelayNumerator");
        control.DelayDenominator = ConvertToUShort(frame.DelayDenominator, "DelayDenominator");
        control.DisposeOperation = frame.DisposeOperation;
        control.BlendOperation = frame.BlendOperation;
        return control;
    }

    internal static PngFrameData CreateFrameData(uint sequenceNumber, byte[] compressedFrameData)
    {
        PngFrameData frameData = new PngFrameData();
        frameData.SequenceNumber = sequenceNumber;
        frameData.Data = compressedFrameData;
        return frameData;
    }

    internal static PngIhdr CreateFrameIhdr(PngIhdr ihdr, int width, int height)
    {
        PngIhdr frameIhdr = new PngIhdr();
        frameIhdr.Width = width;
        frameIhdr.Height = height;
        frameIhdr.BitDepth = ihdr.BitDepth;
        frameIhdr.ColorType = ihdr.ColorType;
        frameIhdr.CompressionMethod = ihdr.CompressionMethod;
        frameIhdr.FilterMethod = ihdr.FilterMethod;
        frameIhdr.InterlaceMethod = ihdr.InterlaceMethod;
        return frameIhdr;
    }

    internal static byte[] EncodeFramePixels(PngPixelBuffer pixels, PngIhdr ihdr, PngEncoderOptions options)
    {
        PngPixelBuffer normalizedPixels = PngEncoder.NormalizeChannelOrder(pixels);
        byte[] filteredScanlines;
        if (ihdr.InterlaceMethod == PngInterlaceMethod.None)
        {
            filteredScanlines = PngNonInterlacedImageEncoder.Encode(normalizedPixels, ihdr, options);
            return CompressFrameData(filteredScanlines, options);
        }

        if (ihdr.InterlaceMethod == PngInterlaceMethod.Adam7)
        {
            byte[] imageData = CreateContiguousImageData(normalizedPixels, ihdr);
            filteredScanlines = PngAdam7Encoder.Encode(imageData, ihdr, options);
            return CompressFrameData(filteredScanlines, options);
        }

        throw new ArgumentOutOfRangeException("InterlaceMethod");
    }

    internal static byte[] CompressFrameData(byte[] filteredScanlines, PngEncoderOptions options)
    {
        if (options.EarlyBailoutByteCount > 0)
        {
            return PngZlibCompressor.CompressWithBailout(filteredScanlines, 0, filteredScanlines.Length, PngCompressionLevelMapper.Map(options.CompressionLevel), options.Limits, options);
        }

        return PngZlibCompressor.Compress(filteredScanlines, 0, filteredScanlines.Length, PngCompressionLevelMapper.Map(options.CompressionLevel), options.Limits);
    }

    internal static PngChunk CreateIdatChunk(byte[] compressedFrameData)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = PngConstants.ChunkIDAT;
        chunk.Data = compressedFrameData;
        chunk.Length = (uint)compressedFrameData.Length;
        chunk.IsKnown = true;
        chunk.IsCritical = true;
        chunk.IsPublic = true;
        chunk.IsReservedBitValid = true;
        chunk.IsSafeToCopy = false;
        return chunk;
    }

    internal static void ValidateDefaultFrame(int frameWidth, int frameHeight, int xOffset, int yOffset, PngIhdr ihdr)
    {
        if (frameWidth != ihdr.Width)
        {
            throw new ArgumentOutOfRangeException("Width");
        }

        if (frameHeight != ihdr.Height)
        {
            throw new ArgumentOutOfRangeException("Height");
        }

        if (xOffset != 0)
        {
            throw new ArgumentOutOfRangeException("XOffset");
        }

        if (yOffset != 0)
        {
            throw new ArgumentOutOfRangeException("YOffset");
        }
    }

    internal static void ValidateFrameRectangle(int frameWidth, int frameHeight, int xOffset, int yOffset, PngIhdr ihdr)
    {
        if (frameWidth <= 0)
        {
            throw new ArgumentOutOfRangeException("Width");
        }

        if (frameHeight <= 0)
        {
            throw new ArgumentOutOfRangeException("Height");
        }

        if (xOffset < 0)
        {
            throw new ArgumentOutOfRangeException("XOffset");
        }

        if (yOffset < 0)
        {
            throw new ArgumentOutOfRangeException("YOffset");
        }

        if (xOffset > ihdr.Width - frameWidth)
        {
            throw new ArgumentOutOfRangeException("XOffset");
        }

        if (yOffset > ihdr.Height - frameHeight)
        {
            throw new ArgumentOutOfRangeException("YOffset");
        }
    }

    internal static byte[] CreateContiguousImageData(PngPixelBuffer pixels, PngIhdr ihdr)
    {
        PngScanlineLayout layout = PngScanlineLayoutCalculator.Calculate(ihdr);
        PngPixelFormat expectedFormat = PngNonInterlacedImageDecoder.GetNativePixelFormat(ihdr.ColorType, ihdr.BitDepth);
        if (pixels.Width != ihdr.Width)
        {
            throw new ArgumentOutOfRangeException("Width");
        }

        if (pixels.Height != ihdr.Height)
        {
            throw new ArgumentOutOfRangeException("Height");
        }

        if (pixels.Format != expectedFormat)
        {
            throw new ArgumentOutOfRangeException("Format");
        }

        if (layout.RowDataBytes > int.MaxValue || layout.ImageDataBytes > int.MaxValue)
        {
            throw new OverflowException();
        }

        int rowBytes = (int)layout.RowDataBytes;
        if (pixels.Stride < rowBytes)
        {
            throw new ArgumentOutOfRangeException("Stride");
        }

        long requiredBytes = CalculateRequiredBytes(pixels.Stride, pixels.Height, rowBytes);
        if (requiredBytes > pixels.Data.Length)
        {
            throw new ArgumentOutOfRangeException("Data");
        }

        byte[] imageData = new byte[(int)layout.ImageDataBytes];
        int rowIndex = 0;
        while (rowIndex < pixels.Height)
        {
            CopyBytes(pixels.Data, rowIndex * pixels.Stride, imageData, rowIndex * rowBytes, rowBytes);
            rowIndex += 1;
        }

        return imageData;
    }

    internal static long CalculateRequiredBytes(int stride, int height, int rowBytes)
    {
        if (height == 1)
        {
            return rowBytes;
        }

        return (long)stride * (height - 1) + rowBytes;
    }

    internal static uint ConvertToUInt(int value, string name)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(name);
        }

        return (uint)value;
    }

    internal static ushort ConvertToUShort(int value, string name)
    {
        if (value < 0 || value > ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException(name);
        }

        return (ushort)value;
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
