namespace png;

internal class PngSecurityLimitEnforcer
{
    internal static void EnforceDatastream(PngChunk[] chunks, PngSecurityLimits limits)
    {
        EnforceChunkAggregates(chunks, limits);
        int index = 0;
        while (index < chunks.Length)
        {
            PngChunk chunk = chunks[index];
            if (chunk.Type == PngConstants.ChunkIHDR && chunk.Data.Length == PngIhdrParser.DataLength)
            {
                PngIhdr ihdr = PngIhdrParser.Parse(chunk.Data);
                PngValidationResult validation = PngIhdrValidator.Validate(ihdr, limits);
                if (validation.IsValid)
                {
                    EnforceIhdr(ihdr, limits);
                    EnforceAnimationChunks(chunks, ihdr, limits);
                }

                return;
            }

            index += 1;
        }
    }

    internal static void EnforceIhdr(PngIhdr ihdr, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(ihdr);
        ArgumentNullException.ThrowIfNull(limits);

        PngValidationResult validation = PngIhdrValidator.Validate(ihdr, limits);
        if (!validation.IsValid)
        {
            if (HasLimitError(validation.Errors))
            {
                throw new OverflowException();
            }

            return;
        }

        PngScanlineLayout layout = PngScanlineLayoutCalculator.Calculate(ihdr);
        EnforceRowBytes(layout.FilteredRowBytes, limits);
        EnforceFrameBytes(layout.ImageDataBytes, limits);
        EnforceDecompressedBytes(layout.FilteredImageBytes, limits);
    }

    internal static bool HasLimitError(PngError[] errors)
    {
        int index = 0;
        while (index < errors.Length)
        {
            string code = errors[index].Code;
            if (code == "PNG_IHDR_WIDTH_LIMIT_EXCEEDED"
                || code == "PNG_IHDR_HEIGHT_LIMIT_EXCEEDED"
                || code == "PNG_IHDR_PIXEL_COUNT_LIMIT_EXCEEDED")
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    internal static void EnforcePixelBuffer(PngPixelBuffer buffer, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(buffer.Data);
        ArgumentNullException.ThrowIfNull(limits);

        EnforceDimensions(buffer.Width, buffer.Height, limits);
        if (buffer.Width <= 0 || buffer.Height <= 0 || buffer.Format == PngPixelFormat.Unknown)
        {
            return;
        }

        long rowBytes = PngSizeCalculator.CalculateFrameRowBytes(buffer.Width, buffer.Format);
        long frameBytes = PngSizeCalculator.CalculateFrameBytes(buffer.Width, buffer.Height, buffer.Format);
        EnforceRowBytes(rowBytes, limits);
        EnforceFrameBytes(frameBytes, limits);
        EnforceDecompressedBytes(frameBytes, limits);
    }

    internal static void EnforceImage(PngImage image, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(image.Pixels);
        ArgumentNullException.ThrowIfNull(limits);

        EnforcePixelBuffer(image.Pixels, limits);
    }

    internal static void EnforceDimensions(int width, int height, PngSecurityLimits limits)
    {
        if (limits.MaxWidth > 0 && width > limits.MaxWidth)
        {
            throw new OverflowException();
        }

        if (limits.MaxHeight > 0 && height > limits.MaxHeight)
        {
            throw new OverflowException();
        }

        if (width > 0 && height > 0 && limits.MaxPixelCount > 0 && (long)width * height > limits.MaxPixelCount)
        {
            throw new OverflowException();
        }
    }

    internal static void EnforceRowBytes(long rowBytes, PngSecurityLimits limits)
    {
        if (limits.MaxRowBytes > 0 && rowBytes > limits.MaxRowBytes)
        {
            throw new OverflowException();
        }
    }

    internal static void EnforceFrameBytes(long frameBytes, PngSecurityLimits limits)
    {
        if (limits.MaxFrameBytes > 0 && frameBytes > limits.MaxFrameBytes)
        {
            throw new OverflowException();
        }
    }

    internal static void EnforceDecompressedBytes(long byteCount, PngSecurityLimits limits)
    {
        if (limits.MaxDecompressedBytes > 0 && byteCount > limits.MaxDecompressedBytes)
        {
            throw new OverflowException();
        }
    }

    internal static void EnforceChunkAggregates(PngChunk[] chunks, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(chunks);
        ArgumentNullException.ThrowIfNull(limits);

        if (limits.MaxChunkCount > 0 && chunks.Length > limits.MaxChunkCount)
        {
            throw new OverflowException();
        }

        long metadataBytes = 0;
        long imageDataBytes = 0;
        int textEntries = 0;
        int index = 0;
        while (index < chunks.Length)
        {
            PngChunk chunk = chunks[index];
            ArgumentNullException.ThrowIfNull(chunk);
            ArgumentNullException.ThrowIfNull(chunk.Type);
            ArgumentNullException.ThrowIfNull(chunk.Data);
            if (PngChunkTypeFlags.IsAncillary(chunk.Type) && chunk.Type != PngConstants.ChunkFrameData)
            {
                metadataBytes = AddBytes(metadataBytes, chunk.Data.Length, limits.MaxMetadataBytes);
            }

            if (PngIdentifier.IsTextChunk(chunk.Type))
            {
                textEntries += 1;
                if (limits.MaxTextEntries > 0 && textEntries > limits.MaxTextEntries)
                {
                    throw new OverflowException();
                }
            }

            if (chunk.Type == PngConstants.ChunkIDAT)
            {
                long storedLength = chunk.Data.Length;
                if (storedLength == 0 && chunk.Length > 0)
                {
                    storedLength = chunk.Length;
                }

                imageDataBytes = AddBytes(imageDataBytes, storedLength, limits.MaxIdatBytes);
            }
            else if (chunk.Type == PngConstants.ChunkFrameData)
            {
                int frameDataBytes = chunk.Data.Length - PngFrameDataParser.SequenceNumberLength;
                if (frameDataBytes < 0)
                {
                    frameDataBytes = 0;
                }

                imageDataBytes = AddBytes(imageDataBytes, frameDataBytes, limits.MaxIdatBytes);
            }

            index += 1;
        }
    }

    internal static void EnforceAnimationChunks(PngChunk[] chunks, PngIhdr canvas, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(chunks);
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(limits);

        long animationBytes = 0;
        int frameCount = 0;
        int index = 0;
        while (index < chunks.Length)
        {
            PngChunk chunk = chunks[index];
            if (chunk.Type == PngConstants.ChunkFrameControl && chunk.Data.Length == PngFrameControlParser.DataLength)
            {
                uint width = PngBinaryReader.ReadUInt32(chunk.Data, 4);
                uint height = PngBinaryReader.ReadUInt32(chunk.Data, 8);
                if (width > 0 && height > 0 && width <= int.MaxValue && height <= int.MaxValue)
                {
                    PngIhdr frame = new PngIhdr();
                    frame.Width = (int)width;
                    frame.Height = (int)height;
                    frame.BitDepth = canvas.BitDepth;
                    frame.ColorType = canvas.ColorType;
                    frame.CompressionMethod = canvas.CompressionMethod;
                    frame.FilterMethod = canvas.FilterMethod;
                    frame.InterlaceMethod = canvas.InterlaceMethod;
                    PngValidationResult validation = PngIhdrValidator.Validate(frame, limits);
                    if (HasLimitError(validation.Errors))
                    {
                        throw new OverflowException();
                    }

                    if (validation.IsValid)
                    {
                        EnforceIhdr(frame, limits);
                        PngScanlineLayout layout = PngScanlineLayoutCalculator.Calculate(frame);
                        animationBytes = AddBytes(animationBytes, layout.ImageDataBytes, limits.MaxAnimationBytes);
                        PngPixelFormat compositionFormat = PngDecoder.GetCompositionPixelFormat(canvas);
                        long compositionRowBytes = PngSizeCalculator.CalculateFrameRowBytes(canvas.Width, compositionFormat);
                        long compositionBytes = PngSizeCalculator.CalculateFrameBytes(canvas.Width, canvas.Height, compositionFormat);
                        EnforceRowBytes(compositionRowBytes, limits);
                        EnforceFrameBytes(compositionBytes, limits);
                        animationBytes = AddBytes(animationBytes, compositionBytes, limits.MaxAnimationBytes);
                        frameCount += 1;
                    }
                }
            }

            index += 1;
        }

        if (frameCount > 0)
        {
            EnforceDecompressedBytes(animationBytes, limits);
        }
    }

    internal static long AddBytes(long current, long addition, long limit)
    {
        if (addition < 0 || current > long.MaxValue - addition)
        {
            throw new OverflowException();
        }

        long result = current + addition;
        if (limit > 0 && result > limit)
        {
            throw new OverflowException();
        }

        return result;
    }

    internal static void EnforceTextBytes(long textBytes, PngSecurityLimits limits)
    {
        if (limits.MaxTextBytes > 0 && textBytes > limits.MaxTextBytes)
        {
            throw new OverflowException();
        }
    }
}
