namespace png;

internal class PngImageFactoryCore
{
    internal static PngImage CreateBlank(int width, int height, PngPixelFormat pixelFormat)
    {
        long rowBytes = PngSizeCalculator.CalculateFrameRowBytes(width, pixelFormat);
        long imageBytes = PngSizeCalculator.CalculateFrameBytes(width, height, pixelFormat);
        if (rowBytes > int.MaxValue || imageBytes > int.MaxValue)
        {
            throw new OverflowException();
        }

        PngImage image = new PngImage();
        image.Info = CreateInfo(width, height, pixelFormat);
        image.Pixels = CreatePixelBuffer(width, height, pixelFormat, (int)rowBytes, (int)imageBytes);
        return image;
    }

    internal static PngImage CreateFromBytes(int width, int height, PngPixelFormat pixelFormat, byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        long rowBytes = PngSizeCalculator.CalculateFrameRowBytes(width, pixelFormat);
        long imageBytes = PngSizeCalculator.CalculateFrameBytes(width, height, pixelFormat);
        if (rowBytes > int.MaxValue || imageBytes > int.MaxValue)
        {
            throw new OverflowException();
        }

        if (data.Length != imageBytes)
        {
            throw new ArgumentOutOfRangeException("data");
        }

        PngImage image = new PngImage();
        image.Info = CreateInfo(width, height, pixelFormat);
        image.Pixels = CreatePixelBuffer(width, height, pixelFormat, (int)rowBytes, CopyData(data));
        return image;
    }

    internal static PngImage CreateFromSpan(int width, int height, PngPixelFormat pixelFormat, ReadOnlySpan<byte> data)
    {
        long rowBytes = PngSizeCalculator.CalculateFrameRowBytes(width, pixelFormat);
        long imageBytes = PngSizeCalculator.CalculateFrameBytes(width, height, pixelFormat);
        if (rowBytes > int.MaxValue || imageBytes > int.MaxValue)
        {
            throw new OverflowException();
        }

        if (data.Length != imageBytes)
        {
            throw new ArgumentOutOfRangeException("data");
        }

        return CreateWithStride(width, height, pixelFormat, (int)rowBytes, data);
    }

    internal static PngImage CreateWithStride(int width, int height, PngPixelFormat pixelFormat, int stride, ReadOnlySpan<byte> data)
    {
        PngImage image = new PngImage();
        image.Info = CreateInfo(width, height, pixelFormat);
        image.Pixels = CreatePixelBufferFromSpan(width, height, pixelFormat, stride, data);
        return image;
    }

    internal static PngImage CreateIndexed(int width, int height, PngPixelFormat pixelFormat, int stride, ReadOnlySpan<byte> data, PngPalette palette)
    {
        ArgumentNullException.ThrowIfNull(palette);
        ValidateIndexedFormat(pixelFormat);
        PngPalette paletteCopy = CopyPalette(palette);
        ValidatePaletteSize(pixelFormat, paletteCopy);

        PngImage image = CreateWithStride(width, height, pixelFormat, stride, data);
        ValidatePaletteIndices(image.Pixels, paletteCopy.Entries.Length);
        image.Metadata.Palette = paletteCopy;
        return image;
    }

    internal static PngImage CreateWithMetadata(int width, int height, PngPixelFormat pixelFormat, int stride, ReadOnlySpan<byte> data, PngMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        PngImage image = CreateWithStride(width, height, pixelFormat, stride, data);
        image.Metadata = metadata;
        return image;
    }

    internal static PngFrame CreateFrame(int width, int height, int xOffset, int yOffset, PngPixelFormat pixelFormat, int stride, ReadOnlySpan<byte> data, int delayNumerator, int delayDenominator, PngApngDisposeOperation disposeOperation, PngApngBlendOperation blendOperation)
    {
        if (xOffset < 0)
        {
            throw new ArgumentOutOfRangeException("xOffset");
        }

        if (yOffset < 0)
        {
            throw new ArgumentOutOfRangeException("yOffset");
        }

        ValidateFrameDelay(delayNumerator, delayDenominator);
        ValidateFrameOperations(disposeOperation, blendOperation);

        PngFrame frame = new PngFrame();
        frame.Width = width;
        frame.Height = height;
        frame.XOffset = xOffset;
        frame.YOffset = yOffset;
        frame.DelayNumerator = delayNumerator;
        frame.DelayDenominator = delayDenominator;
        frame.DisposeOperation = disposeOperation;
        frame.BlendOperation = blendOperation;
        frame.Pixels = CreatePixelBufferFromSpan(width, height, pixelFormat, stride, data);
        return frame;
    }

    internal static PngImage CreateAnimation(PngImage image, int playCount, bool defaultImageIsFirstFrame, ReadOnlySpan<PngFrame> frames)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(image.Pixels);
        ArgumentNullException.ThrowIfNull(image.Pixels.Data);
        ArgumentNullException.ThrowIfNull(image.Metadata);
        if (playCount < 0)
        {
            throw new ArgumentOutOfRangeException("playCount");
        }

        if (frames.Length == 0)
        {
            throw new ArgumentOutOfRangeException("frames");
        }

        PngImage result = new PngImage();
        result.Pixels = CopyPixelBuffer(image.Pixels);
        result.Info = CreateInfo(result.Pixels.Width, result.Pixels.Height, result.Pixels.Format);
        result.Info.IsAnimated = true;
        result.Info.FrameCount = frames.Length;
        result.Info.PlayCount = playCount;
        result.Metadata = image.Metadata;

        PngFrame[] frameCopies = new PngFrame[frames.Length];
        int index = 0;
        while (index < frames.Length)
        {
            PngFrame frame = frames[index];
            ArgumentNullException.ThrowIfNull(frame);
            PngFrame frameCopy = CopyFrame(frame);
            ValidateFrameForCanvas(frameCopy, result.Pixels);
            frameCopy.UsesDefaultImage = defaultImageIsFirstFrame && index == 0;
            frameCopies[index] = frameCopy;
            index += 1;
        }

        if (defaultImageIsFirstFrame)
        {
            ValidateDefaultFrame(frameCopies[0], result.Pixels);
        }

        result.Animation.FrameCount = frameCopies.Length;
        result.Animation.PlayCount = playCount;
        result.Animation.Frames = frameCopies;
        return result;
    }

    internal static PngImageInfo CreateInfo(int width, int height, PngPixelFormat pixelFormat)
    {
        PngImageInfo info = new PngImageInfo();
        info.Width = width;
        info.Height = height;
        info.PixelFormat = pixelFormat;
        info.ColorType = GetColorType(pixelFormat);
        info.BitDepth = GetBitDepth(pixelFormat);
        info.CompressionMethod = PngCompressionMethod.Deflate;
        info.FilterMethod = PngFilterMethod.Adaptive;
        info.InterlaceMethod = PngInterlaceMethod.None;
        return info;
    }

    internal static PngPixelBuffer CreatePixelBuffer(int width, int height, PngPixelFormat pixelFormat, int stride, int byteCount)
    {
        PngPixelBuffer buffer = new PngPixelBuffer();
        buffer.Format = pixelFormat;
        buffer.Width = width;
        buffer.Height = height;
        buffer.Stride = stride;
        buffer.Data = new byte[byteCount];
        return buffer;
    }

    internal static PngPixelBuffer CreatePixelBuffer(int width, int height, PngPixelFormat pixelFormat, int stride, byte[] data)
    {
        PngPixelBuffer buffer = new PngPixelBuffer();
        buffer.Format = pixelFormat;
        buffer.Width = width;
        buffer.Height = height;
        buffer.Stride = stride;
        buffer.Data = data;
        return buffer;
    }

    internal static byte[] CopyData(byte[] data)
    {
        byte[] copy = new byte[data.Length];
        int index = 0;
        while (index < data.Length)
        {
            copy[index] = data[index];
            index += 1;
        }

        return copy;
    }

    internal static byte[] CopySpan(ReadOnlySpan<byte> data)
    {
        byte[] copy = new byte[data.Length];
        int index = 0;
        while (index < data.Length)
        {
            copy[index] = data[index];
            index += 1;
        }

        return copy;
    }

    internal static PngPixelBuffer CreatePixelBufferFromSpan(int width, int height, PngPixelFormat pixelFormat, int stride, ReadOnlySpan<byte> data)
    {
        long rowBytes = PngSizeCalculator.CalculateFrameRowBytes(width, pixelFormat);
        if (rowBytes > int.MaxValue)
        {
            throw new OverflowException();
        }

        if (stride < rowBytes)
        {
            throw new ArgumentOutOfRangeException("stride");
        }

        long requiredBytes = rowBytes;
        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException("height");
        }

        if (height > 1)
        {
            requiredBytes = (long)stride * (height - 1) + rowBytes;
        }

        if (requiredBytes > int.MaxValue)
        {
            throw new OverflowException();
        }

        if (data.Length < requiredBytes)
        {
            throw new ArgumentOutOfRangeException("data");
        }

        return CreatePixelBuffer(width, height, pixelFormat, stride, CopySpan(data));
    }

    internal static PngPixelBuffer CopyPixelBuffer(PngPixelBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(buffer.Data);
        return CreatePixelBufferFromSpan(buffer.Width, buffer.Height, buffer.Format, buffer.Stride, buffer.Data);
    }

    internal static PngFrame CopyFrame(PngFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame.Pixels);
        ValidateFrameDelay(frame.DelayNumerator, frame.DelayDenominator);
        ValidateFrameOperations(frame.DisposeOperation, frame.BlendOperation);

        PngFrame copy = new PngFrame();
        copy.Width = frame.Width;
        copy.Height = frame.Height;
        copy.XOffset = frame.XOffset;
        copy.YOffset = frame.YOffset;
        copy.DelayNumerator = frame.DelayNumerator;
        copy.DelayDenominator = frame.DelayDenominator;
        copy.DisposeOperation = frame.DisposeOperation;
        copy.BlendOperation = frame.BlendOperation;
        copy.Pixels = CopyPixelBuffer(frame.Pixels);
        return copy;
    }

    internal static void ValidateFrameDelay(int delayNumerator, int delayDenominator)
    {
        if (delayNumerator < 0 || delayNumerator > ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException("delayNumerator");
        }

        if (delayDenominator < 0 || delayDenominator > ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException("delayDenominator");
        }
    }

    internal static void ValidateFrameOperations(PngApngDisposeOperation disposeOperation, PngApngBlendOperation blendOperation)
    {
        if (disposeOperation != PngApngDisposeOperation.None && disposeOperation != PngApngDisposeOperation.Background && disposeOperation != PngApngDisposeOperation.Previous)
        {
            throw new ArgumentOutOfRangeException("disposeOperation");
        }

        if (blendOperation != PngApngBlendOperation.Source && blendOperation != PngApngBlendOperation.Over)
        {
            throw new ArgumentOutOfRangeException("blendOperation");
        }
    }

    internal static void ValidateFrameForCanvas(PngFrame frame, PngPixelBuffer canvas)
    {
        if (frame.Width != frame.Pixels.Width || frame.Height != frame.Pixels.Height)
        {
            throw new ArgumentOutOfRangeException("frames");
        }

        if (frame.XOffset < 0 || frame.YOffset < 0)
        {
            throw new ArgumentOutOfRangeException("frames");
        }

        if (frame.Pixels.Format != canvas.Format)
        {
            throw new ArgumentOutOfRangeException("frames");
        }

        if ((long)frame.XOffset + frame.Width > canvas.Width || (long)frame.YOffset + frame.Height > canvas.Height)
        {
            throw new ArgumentOutOfRangeException("frames");
        }
    }

    internal static void ValidateDefaultFrame(PngFrame frame, PngPixelBuffer canvas)
    {
        if (frame.Width != canvas.Width || frame.Height != canvas.Height || frame.XOffset != 0 || frame.YOffset != 0)
        {
            throw new ArgumentOutOfRangeException("frames");
        }

        if (!PixelBuffersEqual(frame.Pixels, canvas))
        {
            throw new ArgumentOutOfRangeException("frames");
        }
    }

    internal static bool PixelBuffersEqual(PngPixelBuffer first, PngPixelBuffer second)
    {
        if (first.Format != second.Format || first.Width != second.Width || first.Height != second.Height)
        {
            return false;
        }

        int rowBytes = (int)PngSizeCalculator.CalculateFrameRowBytes(first.Width, first.Format);
        int y = 0;
        while (y < first.Height)
        {
            int x = 0;
            while (x < rowBytes)
            {
                if (first.Data[y * first.Stride + x] != second.Data[y * second.Stride + x])
                {
                    return false;
                }

                x += 1;
            }

            y += 1;
        }

        return true;
    }

    internal static void ValidateIndexedFormat(PngPixelFormat pixelFormat)
    {
        if (pixelFormat != PngPixelFormat.Indexed1 && pixelFormat != PngPixelFormat.Indexed2 && pixelFormat != PngPixelFormat.Indexed4 && pixelFormat != PngPixelFormat.Indexed8)
        {
            throw new ArgumentOutOfRangeException("pixelFormat");
        }
    }

    internal static PngPalette CopyPalette(PngPalette palette)
    {
        ArgumentNullException.ThrowIfNull(palette.Entries);
        PngPalette copy = new PngPalette();
        copy.Entries = new PngPaletteEntry[palette.Entries.Length];
        int index = 0;
        while (index < palette.Entries.Length)
        {
            PngPaletteEntry entry = palette.Entries[index];
            ArgumentNullException.ThrowIfNull(entry);
            PngPaletteEntry entryCopy = new PngPaletteEntry();
            entryCopy.Red = entry.Red;
            entryCopy.Green = entry.Green;
            entryCopy.Blue = entry.Blue;
            copy.Entries[index] = entryCopy;
            index += 1;
        }

        return copy;
    }

    internal static void ValidatePaletteSize(PngPixelFormat pixelFormat, PngPalette palette)
    {
        int maximumEntries = 256;
        switch (pixelFormat)
        {
            case PngPixelFormat.Indexed1:
                maximumEntries = 2;
                break;
            case PngPixelFormat.Indexed2:
                maximumEntries = 4;
                break;
            case PngPixelFormat.Indexed4:
                maximumEntries = 16;
                break;
            case PngPixelFormat.Indexed8:
                break;
            default:
                throw new ArgumentOutOfRangeException("pixelFormat");
        }

        if (palette.Entries.Length == 0 || palette.Entries.Length > maximumEntries)
        {
            throw new ArgumentOutOfRangeException("palette");
        }
    }

    internal static void ValidatePaletteIndices(PngPixelBuffer pixels, int paletteEntryCount)
    {
        int bitsPerIndex = PngSizeCalculator.GetPixelFormatBitsPerPixel(pixels.Format);
        int mask = (1 << bitsPerIndex) - 1;
        int y = 0;
        while (y < pixels.Height)
        {
            int x = 0;
            while (x < pixels.Width)
            {
                int bitOffset = x * bitsPerIndex;
                int shift = 8 - bitsPerIndex - bitOffset % 8;
                int index = (pixels.Data[y * pixels.Stride + bitOffset / 8] >> shift) & mask;
                if (index >= paletteEntryCount)
                {
                    throw new ArgumentOutOfRangeException("data");
                }

                x += 1;
            }

            y += 1;
        }
    }

    internal static PngColorType GetColorType(PngPixelFormat pixelFormat)
    {
        switch (pixelFormat)
        {
            case PngPixelFormat.Grayscale1:
            case PngPixelFormat.Grayscale2:
            case PngPixelFormat.Grayscale4:
            case PngPixelFormat.Grayscale8:
            case PngPixelFormat.Grayscale16:
                return PngColorType.Grayscale;
            case PngPixelFormat.Indexed1:
            case PngPixelFormat.Indexed2:
            case PngPixelFormat.Indexed4:
            case PngPixelFormat.Indexed8:
                return PngColorType.IndexedColor;
            case PngPixelFormat.Rgb8:
            case PngPixelFormat.Rgb16:
            case PngPixelFormat.Bgr8:
            case PngPixelFormat.Bgr16:
                return PngColorType.Truecolor;
            case PngPixelFormat.GrayscaleAlpha8:
            case PngPixelFormat.GrayscaleAlpha16:
                return PngColorType.GrayscaleAlpha;
            case PngPixelFormat.Rgba8:
            case PngPixelFormat.Rgba16:
            case PngPixelFormat.Bgra8:
            case PngPixelFormat.Bgra16:
            case PngPixelFormat.Argb8:
            case PngPixelFormat.Argb16:
                return PngColorType.TruecolorAlpha;
            default:
                throw new ArgumentOutOfRangeException("pixelFormat");
        }
    }

    internal static PngBitDepth GetBitDepth(PngPixelFormat pixelFormat)
    {
        switch (pixelFormat)
        {
            case PngPixelFormat.Grayscale1:
            case PngPixelFormat.Indexed1:
                return PngBitDepth.One;
            case PngPixelFormat.Grayscale2:
            case PngPixelFormat.Indexed2:
                return PngBitDepth.Two;
            case PngPixelFormat.Grayscale4:
            case PngPixelFormat.Indexed4:
                return PngBitDepth.Four;
            case PngPixelFormat.Grayscale8:
            case PngPixelFormat.Indexed8:
            case PngPixelFormat.Rgb8:
            case PngPixelFormat.Bgr8:
            case PngPixelFormat.GrayscaleAlpha8:
            case PngPixelFormat.Rgba8:
            case PngPixelFormat.Bgra8:
            case PngPixelFormat.Argb8:
                return PngBitDepth.Eight;
            case PngPixelFormat.Grayscale16:
            case PngPixelFormat.Rgb16:
            case PngPixelFormat.Bgr16:
            case PngPixelFormat.GrayscaleAlpha16:
            case PngPixelFormat.Rgba16:
            case PngPixelFormat.Bgra16:
            case PngPixelFormat.Argb16:
                return PngBitDepth.Sixteen;
            default:
                throw new ArgumentOutOfRangeException("pixelFormat");
        }
    }
}
