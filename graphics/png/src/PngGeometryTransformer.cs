namespace png;

/// <summary>
/// Provides vertical flipping, cropping, padding, rectangle copying, and image resizing for pixel buffers.
/// </summary>
internal static class PngGeometryTransformer
{
    /// <summary>
    /// Flips the row order vertically and returns a new buffer without changing the source.
    /// </summary>
    /// <param name="buffer">The source pixel buffer.</param>
    /// <returns>A newly allocated pixel buffer containing the result.</returns>
    public static PngPixelBuffer FlipVertical(PngPixelBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(buffer.Data);

        int rowBytes = CalculateRowBytes(buffer.Width, buffer.Format);
        ValidateBuffer(buffer, rowBytes);

        byte[] output = CreateOutputData(rowBytes, buffer.Height);
        int rowIndex = 0;
        while (rowIndex < buffer.Height)
        {
            CopyFlippedRow(buffer, rowIndex, rowBytes, output);
            rowIndex += 1;
        }

        return CreateBuffer(buffer.Format, buffer.Width, buffer.Height, rowBytes, output);
    }

    /// <summary>
    /// Copies the requested pixel rectangle into a tightly sized new buffer.
    /// </summary>
    /// <param name="buffer">The source pixel buffer.</param>
    /// <param name="x">The zero-based horizontal coordinate.</param>
    /// <param name="y">The zero-based vertical coordinate.</param>
    /// <param name="width">The width in pixels.</param>
    /// <param name="height">The height in pixels.</param>
    /// <returns>A newly allocated pixel buffer containing the result.</returns>
    public static PngPixelBuffer Crop(PngPixelBuffer buffer, int x, int y, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(buffer.Data);

        int sourceRowBytes = CalculateRowBytes(buffer.Width, buffer.Format);
        ValidateBuffer(buffer, sourceRowBytes);
        ValidateCrop(buffer, x, y, width, height);

        int outputRowBytes = CalculateRowBytes(width, buffer.Format);
        byte[] output = CreateOutputData(outputRowBytes, height);

        int rowIndex = 0;
        while (rowIndex < height)
        {
            CopyCroppedRow(buffer, x, y + rowIndex, width, rowIndex, outputRowBytes, output);
            rowIndex += 1;
        }

        return CreateBuffer(buffer.Format, width, height, outputRowBytes, output);
    }

    /// <summary>
    /// Returns a larger zero-filled canvas with the source pixels copied at the requested offset.
    /// </summary>
    /// <param name="buffer">The source pixel buffer.</param>
    /// <param name="left">The number of blank pixels to add on the left.</param>
    /// <param name="top">The number of blank pixels to add above.</param>
    /// <param name="right">The number of blank pixels to add on the right.</param>
    /// <param name="bottom">The number of blank pixels to add below.</param>
    /// <returns>A newly allocated pixel buffer containing the result.</returns>
    public static PngPixelBuffer PadCanvas(PngPixelBuffer buffer, int left, int top, int right, int bottom)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(buffer.Data);

        int sourceRowBytes = CalculateRowBytes(buffer.Width, buffer.Format);
        ValidateBuffer(buffer, sourceRowBytes);
        ValidatePadding(left, top, right, bottom);

        int outputWidth = CalculatePaddedSize(buffer.Width, left, right, "width");
        int outputHeight = CalculatePaddedSize(buffer.Height, top, bottom, "height");
        int outputRowBytes = CalculateRowBytes(outputWidth, buffer.Format);
        byte[] output = CreateOutputData(outputRowBytes, outputHeight);

        int rowIndex = 0;
        while (rowIndex < buffer.Height)
        {
            CopyPaddedRow(buffer, rowIndex, left, top, sourceRowBytes, outputRowBytes, output);
            rowIndex += 1;
        }

        return CreateBuffer(buffer.Format, outputWidth, outputHeight, outputRowBytes, output);
    }

    /// <summary>
    /// Copies a rectangular pixel region into an existing destination buffer.
    /// </summary>
    /// <param name="source">The source pixel buffer.</param>
    /// <param name="sourceX">The source rectangle's zero-based horizontal coordinate.</param>
    /// <param name="sourceY">The source rectangle's zero-based vertical coordinate.</param>
    /// <param name="destination">The destination pixel buffer, which is modified in place.</param>
    /// <param name="destinationX">The destination rectangle's zero-based horizontal coordinate.</param>
    /// <param name="destinationY">The destination rectangle's zero-based vertical coordinate.</param>
    /// <param name="width">The width in pixels.</param>
    /// <param name="height">The height in pixels.</param>
    public static void CopyRectangle(PngPixelBuffer source, int sourceX, int sourceY, PngPixelBuffer destination, int destinationX, int destinationY, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(source.Data);
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(destination.Data);

        int sourceRowBytes = CalculateRowBytes(source.Width, source.Format);
        int destinationRowBytes = CalculateRowBytes(destination.Width, destination.Format);
        ValidateBuffer(source, sourceRowBytes);
        ValidateBuffer(destination, destinationRowBytes);
        ValidateCopyRectangle(source, destination, sourceX, sourceY, destinationX, destinationY, width, height);

        if (IsPackedFormat(source.Format))
        {
            CopyRectanglePacked(source, sourceX, sourceY, destination, destinationX, destinationY, width, height);
            return;
        }

        CopyRectangleByteAligned(source, sourceX, sourceY, destination, destinationX, destinationY, width, height);
    }

    /// <summary>
    /// Resizes into a new buffer by copying the nearest source pixel for each destination pixel.
    /// </summary>
    /// <param name="buffer">The source pixel buffer.</param>
    /// <param name="width">The width in pixels.</param>
    /// <param name="height">The height in pixels.</param>
    /// <returns>A newly allocated pixel buffer containing the result.</returns>
    public static PngPixelBuffer ResizeNearestNeighbor(PngPixelBuffer buffer, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(buffer.Data);

        int sourceRowBytes = CalculateRowBytes(buffer.Width, buffer.Format);
        ValidateBuffer(buffer, sourceRowBytes);
        ValidateResize(width, height);

        int outputRowBytes = CalculateRowBytes(width, buffer.Format);
        byte[] output = CreateOutputData(outputRowBytes, height);

        if (IsPackedFormat(buffer.Format))
        {
            ResizeNearestNeighborPacked(buffer, width, height, outputRowBytes, output);
        }
        else
        {
            ResizeNearestNeighborByteAligned(buffer, width, height, outputRowBytes, output);
        }

        return CreateBuffer(buffer.Format, width, height, outputRowBytes, output);
    }

    /// <summary>
    /// Resizes into a new buffer by interpolating the four nearest source samples.
    /// </summary>
    /// <param name="buffer">The source pixel buffer.</param>
    /// <param name="width">The width in pixels.</param>
    /// <param name="height">The height in pixels.</param>
    /// <returns>A newly allocated pixel buffer containing the result.</returns>
    public static PngPixelBuffer ResizeBilinear(PngPixelBuffer buffer, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(buffer.Data);

        int sourceRowBytes = CalculateRowBytes(buffer.Width, buffer.Format);
        ValidateBuffer(buffer, sourceRowBytes);
        ValidateResize(width, height);
        bool packedGrayscale = PngGrayscaleExpander.IsLowBitGrayscaleFormat(buffer.Format);
        if (!packedGrayscale)
        {
            ValidateBilinearFormat(buffer.Format);
        }

        int outputRowBytes = CalculateRowBytes(width, buffer.Format);
        byte[] output = CreateOutputData(outputRowBytes, height);
        if (packedGrayscale)
        {
            ResizeBilinearPackedGrayscale(buffer, width, height, outputRowBytes, output);
        }
        else
        {
            ResizeBilinearCore(buffer, width, height, outputRowBytes, output);
        }

        return CreateBuffer(buffer.Format, width, height, outputRowBytes, output);
    }

    internal static void CopyFlippedRow(PngPixelBuffer buffer, int rowIndex, int rowBytes, byte[] output)
    {
        int sourceRowIndex = buffer.Height - 1 - rowIndex;
        int sourceOffset = buffer.Stride * sourceRowIndex;
        int destinationOffset = rowBytes * rowIndex;
        CopyBytes(buffer.Data, sourceOffset, output, destinationOffset, rowBytes);
    }

    internal static void CopyCroppedRow(PngPixelBuffer buffer, int x, int sourceRowIndex, int width, int destinationRowIndex, int outputRowBytes, byte[] output)
    {
        if (IsPackedFormat(buffer.Format))
        {
            CopyCroppedPackedRow(buffer, x, sourceRowIndex, width, destinationRowIndex, outputRowBytes, output);
            return;
        }

        CopyCroppedByteAlignedRow(buffer, x, sourceRowIndex, destinationRowIndex, outputRowBytes, output);
    }

    internal static void CopyCroppedByteAlignedRow(PngPixelBuffer buffer, int x, int sourceRowIndex, int destinationRowIndex, int outputRowBytes, byte[] output)
    {
        int bitsPerPixel = GetBitsPerPixel(buffer.Format);
        int bytesPerPixel = bitsPerPixel / 8;
        int sourceOffset = buffer.Stride * sourceRowIndex + x * bytesPerPixel;
        int destinationOffset = outputRowBytes * destinationRowIndex;
        CopyBytes(buffer.Data, sourceOffset, output, destinationOffset, outputRowBytes);
    }

    internal static void CopyCroppedPackedRow(PngPixelBuffer buffer, int x, int sourceRowIndex, int width, int destinationRowIndex, int outputRowBytes, byte[] output)
    {
        PngBitDepth bitDepth = GetPackedBitDepth(buffer.Format);
        int sourceOffset = buffer.Stride * sourceRowIndex;
        int destinationOffset = outputRowBytes * destinationRowIndex;
        byte[] samples = PngPackedSampleUnpacker.Unpack(buffer.Data, sourceOffset, buffer.Width, bitDepth);
        byte[] croppedSamples = new byte[width];
        CopyBytes(samples, x, croppedSamples, 0, width);
        PngPackedSampleWriter.PackInto(output, destinationOffset, croppedSamples, 0, width, bitDepth);
    }

    internal static void CopyPaddedRow(PngPixelBuffer buffer, int sourceRowIndex, int left, int top, int sourceRowBytes, int outputRowBytes, byte[] output)
    {
        if (IsPackedFormat(buffer.Format))
        {
            CopyPaddedPackedRow(buffer, sourceRowIndex, left, top, outputRowBytes, output);
            return;
        }

        CopyPaddedByteAlignedRow(buffer, sourceRowIndex, left, top, sourceRowBytes, outputRowBytes, output);
    }

    internal static void CopyPaddedByteAlignedRow(PngPixelBuffer buffer, int sourceRowIndex, int left, int top, int sourceRowBytes, int outputRowBytes, byte[] output)
    {
        int bitsPerPixel = GetBitsPerPixel(buffer.Format);
        int bytesPerPixel = bitsPerPixel / 8;
        int sourceOffset = buffer.Stride * sourceRowIndex;
        int destinationOffset = outputRowBytes * (top + sourceRowIndex) + left * bytesPerPixel;
        CopyBytes(buffer.Data, sourceOffset, output, destinationOffset, sourceRowBytes);
    }

    internal static void CopyPaddedPackedRow(PngPixelBuffer buffer, int sourceRowIndex, int left, int top, int outputRowBytes, byte[] output)
    {
        PngBitDepth bitDepth = GetPackedBitDepth(buffer.Format);
        int bitsPerSample = GetPackedBitsPerSample(buffer.Format);
        int sourceOffset = buffer.Stride * sourceRowIndex;
        int destinationOffset = outputRowBytes * (top + sourceRowIndex);
        byte[] samples = PngPackedSampleUnpacker.Unpack(buffer.Data, sourceOffset, buffer.Width, bitDepth);
        WritePackedSamples(output, destinationOffset, left, samples, 0, samples.Length, bitsPerSample);
    }

    internal static void CopyRectangleByteAligned(PngPixelBuffer source, int sourceX, int sourceY, PngPixelBuffer destination, int destinationX, int destinationY, int width, int height)
    {
        int bitsPerPixel = GetBitsPerPixel(source.Format);
        int bytesPerPixel = bitsPerPixel / 8;
        int copyRowBytes = width * bytesPerPixel;
        byte[] copied = CreateOutputData(copyRowBytes, height);

        int rowIndex = 0;
        while (rowIndex < height)
        {
            int sourceOffset = source.Stride * (sourceY + rowIndex) + sourceX * bytesPerPixel;
            int copiedOffset = copyRowBytes * rowIndex;
            CopyBytes(source.Data, sourceOffset, copied, copiedOffset, copyRowBytes);
            rowIndex += 1;
        }

        rowIndex = 0;
        while (rowIndex < height)
        {
            int destinationOffset = destination.Stride * (destinationY + rowIndex) + destinationX * bytesPerPixel;
            int copiedOffset = copyRowBytes * rowIndex;
            CopyBytes(copied, copiedOffset, destination.Data, destinationOffset, copyRowBytes);
            rowIndex += 1;
        }
    }

    internal static void CopyRectanglePacked(PngPixelBuffer source, int sourceX, int sourceY, PngPixelBuffer destination, int destinationX, int destinationY, int width, int height)
    {
        PngBitDepth bitDepth = GetPackedBitDepth(source.Format);
        int bitsPerSample = GetPackedBitsPerSample(source.Format);
        byte[] copied = CreateSampleData(width, height);

        int rowIndex = 0;
        while (rowIndex < height)
        {
            int sourceOffset = source.Stride * (sourceY + rowIndex);
            int copiedOffset = width * rowIndex;
            byte[] sourceSamples = PngPackedSampleUnpacker.Unpack(source.Data, sourceOffset, source.Width, bitDepth);
            CopyBytes(sourceSamples, sourceX, copied, copiedOffset, width);
            rowIndex += 1;
        }

        rowIndex = 0;
        while (rowIndex < height)
        {
            int destinationOffset = destination.Stride * (destinationY + rowIndex);
            int copiedOffset = width * rowIndex;
            WritePackedSamples(destination.Data, destinationOffset, destinationX, copied, copiedOffset, width, bitsPerSample);
            rowIndex += 1;
        }
    }

    internal static void ResizeNearestNeighborByteAligned(PngPixelBuffer buffer, int width, int height, int outputRowBytes, byte[] output)
    {
        int bitsPerPixel = GetBitsPerPixel(buffer.Format);
        int bytesPerPixel = bitsPerPixel / 8;
        int y = 0;
        while (y < height)
        {
            int sourceY = MapNearestCoordinate(y, buffer.Height, height);
            ResizeNearestNeighborByteAlignedRow(buffer, sourceY, width, y, outputRowBytes, output, bytesPerPixel);
            y += 1;
        }
    }

    internal static void ResizeNearestNeighborByteAlignedRow(PngPixelBuffer buffer, int sourceY, int width, int destinationY, int outputRowBytes, byte[] output, int bytesPerPixel)
    {
        int x = 0;
        while (x < width)
        {
            int sourceX = MapNearestCoordinate(x, buffer.Width, width);
            int sourceOffset = buffer.Stride * sourceY + sourceX * bytesPerPixel;
            int destinationOffset = outputRowBytes * destinationY + x * bytesPerPixel;
            CopyBytes(buffer.Data, sourceOffset, output, destinationOffset, bytesPerPixel);
            x += 1;
        }
    }

    internal static void ResizeNearestNeighborPacked(PngPixelBuffer buffer, int width, int height, int outputRowBytes, byte[] output)
    {
        PngBitDepth bitDepth = GetPackedBitDepth(buffer.Format);
        int bitsPerSample = GetPackedBitsPerSample(buffer.Format);
        int y = 0;
        while (y < height)
        {
            int sourceY = MapNearestCoordinate(y, buffer.Height, height);
            ResizeNearestNeighborPackedRow(buffer, sourceY, width, y, outputRowBytes, output, bitDepth, bitsPerSample);
            y += 1;
        }
    }

    internal static void ResizeNearestNeighborPackedRow(PngPixelBuffer buffer, int sourceY, int width, int destinationY, int outputRowBytes, byte[] output, PngBitDepth bitDepth, int bitsPerSample)
    {
        int sourceOffset = buffer.Stride * sourceY;
        int destinationOffset = outputRowBytes * destinationY;
        byte[] samples = PngPackedSampleUnpacker.Unpack(buffer.Data, sourceOffset, buffer.Width, bitDepth);
        int x = 0;
        while (x < width)
        {
            int sourceX = MapNearestCoordinate(x, buffer.Width, width);
            WritePackedSample(output, destinationOffset, x, samples[sourceX], bitsPerSample);
            x += 1;
        }
    }

    internal static void ResizeBilinearCore(PngPixelBuffer buffer, int width, int height, int outputRowBytes, byte[] output)
    {
        int sampleBytes = GetBilinearSampleBytes(buffer.Format);
        int samplesPerPixel = GetBilinearSamplesPerPixel(buffer.Format);
        int bytesPerPixel = sampleBytes * samplesPerPixel;
        int y = 0;
        while (y < height)
        {
            long sourceY = MapBilinearCoordinate(y, buffer.Height, height);
            ResizeBilinearRow(buffer, width, y, sourceY, outputRowBytes, output, sampleBytes, samplesPerPixel, bytesPerPixel);
            y += 1;
        }
    }

    internal static void ResizeBilinearPackedGrayscale(PngPixelBuffer buffer, int width, int height, int outputRowBytes, byte[] output)
    {
        PngBitDepth bitDepth = GetPackedBitDepth(buffer.Format);
        int bitsPerSample = GetPackedBitsPerSample(buffer.Format);
        int y = 0;
        while (y < height)
        {
            long sourceY = MapBilinearCoordinate(y, buffer.Height, height);
            int y0 = GetFixedWhole(sourceY);
            int y1 = GetNextSampleIndex(y0, buffer.Height);
            int yWeight = GetFixedFraction(sourceY);
            byte[] topSamples = PngPackedSampleUnpacker.Unpack(buffer.Data, buffer.Stride * y0, buffer.Width, bitDepth);
            byte[] bottomSamples = PngPackedSampleUnpacker.Unpack(buffer.Data, buffer.Stride * y1, buffer.Width, bitDepth);
            int destinationOffset = outputRowBytes * y;
            int x = 0;
            while (x < width)
            {
                long sourceX = MapBilinearCoordinate(x, buffer.Width, width);
                int x0 = GetFixedWhole(sourceX);
                int x1 = GetNextSampleIndex(x0, buffer.Width);
                int xWeight = GetFixedFraction(sourceX);
                int top = InterpolateSamples(topSamples[x0], topSamples[x1], xWeight);
                int bottom = InterpolateSamples(bottomSamples[x0], bottomSamples[x1], xWeight);
                int sample = InterpolateSamples(top, bottom, yWeight);
                WritePackedSample(output, destinationOffset, x, (byte)sample, bitsPerSample);
                x += 1;
            }

            y += 1;
        }
    }

    internal static void ResizeBilinearRow(PngPixelBuffer buffer, int width, int destinationY, long sourceY, int outputRowBytes, byte[] output, int sampleBytes, int samplesPerPixel, int bytesPerPixel)
    {
        int y0 = GetFixedWhole(sourceY);
        int y1 = GetNextSampleIndex(y0, buffer.Height);
        int yWeight = GetFixedFraction(sourceY);
        int x = 0;
        while (x < width)
        {
            long sourceX = MapBilinearCoordinate(x, buffer.Width, width);
            WriteBilinearPixel(buffer, sourceX, y0, y1, yWeight, output, outputRowBytes, destinationY, x, sampleBytes, samplesPerPixel, bytesPerPixel);
            x += 1;
        }
    }

    internal static void WriteBilinearPixel(PngPixelBuffer buffer, long sourceX, int y0, int y1, int yWeight, byte[] output, int outputRowBytes, int destinationY, int destinationX, int sampleBytes, int samplesPerPixel, int bytesPerPixel)
    {
        int x0 = GetFixedWhole(sourceX);
        int x1 = GetNextSampleIndex(x0, buffer.Width);
        int xWeight = GetFixedFraction(sourceX);
        int destinationOffset = outputRowBytes * destinationY + destinationX * bytesPerPixel;

        int sampleIndex = 0;
        while (sampleIndex < samplesPerPixel)
        {
            int sample = ReadBilinearSample(buffer, x0, y0, x1, y1, xWeight, yWeight, sampleIndex, sampleBytes, bytesPerPixel);
            WriteSample(output, destinationOffset + sampleIndex * sampleBytes, sample, sampleBytes);
            sampleIndex += 1;
        }
    }

    internal static int ReadBilinearSample(PngPixelBuffer buffer, int x0, int y0, int x1, int y1, int xWeight, int yWeight, int sampleIndex, int sampleBytes, int bytesPerPixel)
    {
        int sampleOffset = sampleIndex * sampleBytes;
        int topLeft = ReadSample(buffer.Data, buffer.Stride * y0 + x0 * bytesPerPixel + sampleOffset, sampleBytes);
        int topRight = ReadSample(buffer.Data, buffer.Stride * y0 + x1 * bytesPerPixel + sampleOffset, sampleBytes);
        int bottomLeft = ReadSample(buffer.Data, buffer.Stride * y1 + x0 * bytesPerPixel + sampleOffset, sampleBytes);
        int bottomRight = ReadSample(buffer.Data, buffer.Stride * y1 + x1 * bytesPerPixel + sampleOffset, sampleBytes);
        int top = InterpolateSamples(topLeft, topRight, xWeight);
        int bottom = InterpolateSamples(bottomLeft, bottomRight, xWeight);
        return InterpolateSamples(top, bottom, yWeight);
    }

    internal static int ReadSample(byte[] data, int offset, int sampleBytes)
    {
        if (sampleBytes == 2)
        {
            return PngBinaryReader.ReadUInt16(data, offset);
        }

        return data[offset];
    }

    internal static void WriteSample(byte[] data, int offset, int sample, int sampleBytes)
    {
        if (sampleBytes == 2)
        {
            PngBinaryWriter.WriteUInt16(data, offset, (ushort)sample);
            return;
        }

        data[offset] = (byte)sample;
    }

    internal static int InterpolateSamples(int first, int second, int weight)
    {
        long inverseWeight = 65536 - weight;
        long sample = ((long)first * inverseWeight + (long)second * weight + 32768) / 65536;
        return (int)sample;
    }

    internal static void WritePackedSamples(byte[] output, int destinationOffset, int destinationSampleOffset, byte[] samples, int sampleOffset, int sampleCount, int bitsPerSample)
    {
        int index = 0;
        while (index < sampleCount)
        {
            WritePackedSample(output, destinationOffset, destinationSampleOffset + index, samples[sampleOffset + index], bitsPerSample);
            index += 1;
        }
    }

    internal static void WritePackedSample(byte[] output, int destinationOffset, int sampleIndex, byte sample, int bitsPerSample)
    {
        int bitIndex = sampleIndex * bitsPerSample;
        int byteIndex = destinationOffset + bitIndex / 8;
        int bitOffset = bitIndex % 8;
        int shift = 8 - bitsPerSample - bitOffset;
        int sampleMask = ((1 << bitsPerSample) - 1) << shift;
        output[byteIndex] = (byte)(output[byteIndex] & ~sampleMask);
        output[byteIndex] = (byte)(output[byteIndex] | (sample << shift));
    }

    internal static void ValidateBuffer(PngPixelBuffer buffer, int rowBytes)
    {
        if (buffer.Width <= 0)
        {
            throw new ArgumentOutOfRangeException("Width");
        }

        if (buffer.Height <= 0)
        {
            throw new ArgumentOutOfRangeException("Height");
        }

        if (buffer.Stride < rowBytes)
        {
            throw new ArgumentOutOfRangeException("Stride");
        }

        long requiredBytes = CalculateRequiredBytes(buffer.Stride, buffer.Height, rowBytes);
        if (requiredBytes > buffer.Data.Length)
        {
            throw new ArgumentOutOfRangeException("Data");
        }
    }

    internal static void ValidateCrop(PngPixelBuffer buffer, int x, int y, int width, int height)
    {
        if (x < 0)
        {
            throw new ArgumentOutOfRangeException("x");
        }

        if (y < 0)
        {
            throw new ArgumentOutOfRangeException("y");
        }

        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException("width");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException("height");
        }

        if (x > buffer.Width - width)
        {
            throw new ArgumentOutOfRangeException("width");
        }

        if (y > buffer.Height - height)
        {
            throw new ArgumentOutOfRangeException("height");
        }
    }

    internal static void ValidateCopyRectangle(PngPixelBuffer source, PngPixelBuffer destination, int sourceX, int sourceY, int destinationX, int destinationY, int width, int height)
    {
        if (source.Format != destination.Format)
        {
            throw new ArgumentOutOfRangeException("Format");
        }

        ValidateRectangleBounds(source.Width, source.Height, sourceX, sourceY, width, height, "source");
        ValidateRectangleBounds(destination.Width, destination.Height, destinationX, destinationY, width, height, "destination");
    }

    internal static void ValidateResize(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException("width");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException("height");
        }
    }

    internal static void ValidateBilinearFormat(PngPixelFormat format)
    {
        GetBilinearSampleBytes(format);
        GetBilinearSamplesPerPixel(format);
    }

    internal static void ValidateRectangleBounds(int bufferWidth, int bufferHeight, int x, int y, int width, int height, string name)
    {
        if (x < 0)
        {
            throw new ArgumentOutOfRangeException(name + "X");
        }

        if (y < 0)
        {
            throw new ArgumentOutOfRangeException(name + "Y");
        }

        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException("width");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException("height");
        }

        if (x > bufferWidth - width)
        {
            throw new ArgumentOutOfRangeException(name + "Width");
        }

        if (y > bufferHeight - height)
        {
            throw new ArgumentOutOfRangeException(name + "Height");
        }
    }

    internal static void ValidatePadding(int left, int top, int right, int bottom)
    {
        if (left < 0)
        {
            throw new ArgumentOutOfRangeException("left");
        }

        if (top < 0)
        {
            throw new ArgumentOutOfRangeException("top");
        }

        if (right < 0)
        {
            throw new ArgumentOutOfRangeException("right");
        }

        if (bottom < 0)
        {
            throw new ArgumentOutOfRangeException("bottom");
        }
    }

    internal static int CalculatePaddedSize(int size, int before, int after, string name)
    {
        long padded = (long)size + before + after;
        if (padded > int.MaxValue)
        {
            throw new OverflowException();
        }

        if (padded <= 0)
        {
            throw new ArgumentOutOfRangeException(name);
        }

        return (int)padded;
    }

    internal static int CalculateRowBytes(int width, PngPixelFormat format)
    {
        int bitsPerPixel = GetBitsPerPixel(format);
        long rowBits = (long)width * bitsPerPixel;
        long rowBytes = (rowBits + 7) / 8;
        if (rowBytes > int.MaxValue)
        {
            throw new OverflowException();
        }

        return (int)rowBytes;
    }

    internal static int MapNearestCoordinate(int destinationIndex, int sourceSize, int destinationSize)
    {
        long sourceIndex = (long)destinationIndex * sourceSize / destinationSize;
        return (int)sourceIndex;
    }

    internal static long MapBilinearCoordinate(int destinationIndex, int sourceSize, int destinationSize)
    {
        long numerator = ((long)destinationIndex * 2 + 1) * sourceSize - destinationSize;
        if (numerator <= 0)
        {
            return 0;
        }

        long denominator = (long)destinationSize * 2;
        long coordinate = numerator * 65536 / denominator;
        long maximumCoordinate = (long)(sourceSize - 1) * 65536;
        if (coordinate > maximumCoordinate)
        {
            return maximumCoordinate;
        }

        return coordinate;
    }

    internal static int GetFixedWhole(long value)
    {
        return (int)(value / 65536);
    }

    internal static int GetFixedFraction(long value)
    {
        return (int)(value % 65536);
    }

    internal static int GetNextSampleIndex(int index, int size)
    {
        if (index + 1 >= size)
        {
            return index;
        }

        return index + 1;
    }

    internal static int GetBitsPerPixel(PngPixelFormat format)
    {
        switch (format)
        {
            case PngPixelFormat.Grayscale1:
            case PngPixelFormat.Indexed1:
                return 1;
            case PngPixelFormat.Grayscale2:
            case PngPixelFormat.Indexed2:
                return 2;
            case PngPixelFormat.Grayscale4:
            case PngPixelFormat.Indexed4:
                return 4;
            case PngPixelFormat.Grayscale8:
            case PngPixelFormat.Indexed8:
                return 8;
            case PngPixelFormat.Grayscale16:
            case PngPixelFormat.GrayscaleAlpha8:
                return 16;
            case PngPixelFormat.Rgb8:
            case PngPixelFormat.Bgr8:
                return 24;
            case PngPixelFormat.GrayscaleAlpha16:
            case PngPixelFormat.Rgba8:
            case PngPixelFormat.Bgra8:
            case PngPixelFormat.Argb8:
                return 32;
            case PngPixelFormat.Rgb16:
            case PngPixelFormat.Bgr16:
                return 48;
            case PngPixelFormat.Rgba16:
            case PngPixelFormat.Bgra16:
            case PngPixelFormat.Argb16:
                return 64;
            default:
                throw new ArgumentOutOfRangeException("Format");
        }
    }

    internal static int GetBilinearSampleBytes(PngPixelFormat format)
    {
        switch (format)
        {
            case PngPixelFormat.Grayscale8:
            case PngPixelFormat.Rgb8:
            case PngPixelFormat.GrayscaleAlpha8:
            case PngPixelFormat.Rgba8:
            case PngPixelFormat.Bgr8:
            case PngPixelFormat.Bgra8:
            case PngPixelFormat.Argb8:
                return 1;
            case PngPixelFormat.Grayscale16:
            case PngPixelFormat.Rgb16:
            case PngPixelFormat.GrayscaleAlpha16:
            case PngPixelFormat.Rgba16:
            case PngPixelFormat.Bgr16:
            case PngPixelFormat.Bgra16:
            case PngPixelFormat.Argb16:
                return 2;
            default:
                throw new ArgumentOutOfRangeException("Format");
        }
    }

    internal static int GetBilinearSamplesPerPixel(PngPixelFormat format)
    {
        switch (format)
        {
            case PngPixelFormat.Grayscale8:
            case PngPixelFormat.Grayscale16:
                return 1;
            case PngPixelFormat.GrayscaleAlpha8:
            case PngPixelFormat.GrayscaleAlpha16:
                return 2;
            case PngPixelFormat.Rgb8:
            case PngPixelFormat.Rgb16:
            case PngPixelFormat.Bgr8:
            case PngPixelFormat.Bgr16:
                return 3;
            case PngPixelFormat.Rgba8:
            case PngPixelFormat.Rgba16:
            case PngPixelFormat.Bgra8:
            case PngPixelFormat.Bgra16:
            case PngPixelFormat.Argb8:
            case PngPixelFormat.Argb16:
                return 4;
            default:
                throw new ArgumentOutOfRangeException("Format");
        }
    }

    internal static bool IsPackedFormat(PngPixelFormat format)
    {
        return format == PngPixelFormat.Grayscale1
            || format == PngPixelFormat.Grayscale2
            || format == PngPixelFormat.Grayscale4
            || format == PngPixelFormat.Indexed1
            || format == PngPixelFormat.Indexed2
            || format == PngPixelFormat.Indexed4;
    }

    internal static PngBitDepth GetPackedBitDepth(PngPixelFormat format)
    {
        switch (format)
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
            default:
                throw new ArgumentOutOfRangeException("Format");
        }
    }

    internal static int GetPackedBitsPerSample(PngPixelFormat format)
    {
        switch (format)
        {
            case PngPixelFormat.Grayscale1:
            case PngPixelFormat.Indexed1:
                return 1;
            case PngPixelFormat.Grayscale2:
            case PngPixelFormat.Indexed2:
                return 2;
            case PngPixelFormat.Grayscale4:
            case PngPixelFormat.Indexed4:
                return 4;
            default:
                throw new ArgumentOutOfRangeException("Format");
        }
    }

    internal static byte[] CreateOutputData(int stride, int height)
    {
        long byteCount = (long)stride * height;
        if (byteCount > int.MaxValue)
        {
            throw new OverflowException();
        }

        return new byte[(int)byteCount];
    }

    internal static byte[] CreateSampleData(int width, int height)
    {
        long sampleCount = (long)width * height;
        if (sampleCount > int.MaxValue)
        {
            throw new OverflowException();
        }

        return new byte[(int)sampleCount];
    }

    internal static long CalculateRequiredBytes(int stride, int height, int rowBytes)
    {
        if (height == 1)
        {
            return rowBytes;
        }

        long leadingRows = (long)stride * (height - 1);
        return leadingRows + rowBytes;
    }

    internal static PngPixelBuffer CreateBuffer(PngPixelFormat format, int width, int height, int stride, byte[] data)
    {
        PngPixelBuffer buffer = new PngPixelBuffer();
        buffer.Format = format;
        buffer.Width = width;
        buffer.Height = height;
        buffer.Stride = stride;
        buffer.Data = data;
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
