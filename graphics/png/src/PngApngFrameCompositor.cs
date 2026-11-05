namespace png;

internal class PngApngFrameCompositor
{
    internal static PngPixelBuffer Compose(PngPixelBuffer canvas, PngFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(frame.Pixels);

        return Compose(canvas, frame.Pixels, frame.XOffset, frame.YOffset, frame.BlendOperation);
    }

    internal static PngPixelBuffer Compose(PngPixelBuffer canvas, PngPixelBuffer framePixels, int xOffset, int yOffset, PngApngBlendOperation blendOperation)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(canvas.Data);
        ArgumentNullException.ThrowIfNull(framePixels);
        ArgumentNullException.ThrowIfNull(framePixels.Data);

        int bytesPerPixel = GetBytesPerPixel(canvas.Format);
        ValidateBuffers(canvas, framePixels, xOffset, yOffset, bytesPerPixel);

        PngPixelBuffer result = CopyCanvas(canvas, bytesPerPixel);
        if (blendOperation == PngApngBlendOperation.Source)
        {
            ComposeSource(framePixels, result, xOffset, yOffset, bytesPerPixel);
            return result;
        }

        if (blendOperation == PngApngBlendOperation.Over)
        {
            ComposeOver(framePixels, result, xOffset, yOffset, bytesPerPixel);
            return result;
        }

        throw new ArgumentOutOfRangeException("blendOperation");
    }

    internal static void ComposeSource(PngPixelBuffer framePixels, PngPixelBuffer canvas, int xOffset, int yOffset, int bytesPerPixel)
    {
        int frameRowBytes = CalculateRowBytes(framePixels.Width, bytesPerPixel);
        int rowIndex = 0;
        while (rowIndex < framePixels.Height)
        {
            int sourceOffset = rowIndex * framePixels.Stride;
            int destinationOffset = (yOffset + rowIndex) * canvas.Stride + xOffset * bytesPerPixel;
            CopyBytes(framePixels.Data, sourceOffset, canvas.Data, destinationOffset, frameRowBytes);
            rowIndex += 1;
        }
    }

    internal static void ComposeOver(PngPixelBuffer framePixels, PngPixelBuffer canvas, int xOffset, int yOffset, int bytesPerPixel)
    {
        int maxSample = GetMaxSample(framePixels.Format);
        int alphaOffset = GetAlphaOffset(framePixels.Format);
        int colorSamples = GetColorSamples(framePixels.Format);

        int rowIndex = 0;
        while (rowIndex < framePixels.Height)
        {
            ComposeOverRow(framePixels, canvas, xOffset, yOffset, bytesPerPixel, maxSample, alphaOffset, colorSamples, rowIndex);
            rowIndex += 1;
        }
    }

    internal static void ComposeOverRow(PngPixelBuffer framePixels, PngPixelBuffer canvas, int xOffset, int yOffset, int bytesPerPixel, int maxSample, int alphaOffset, int colorSamples, int rowIndex)
    {
        int columnIndex = 0;
        while (columnIndex < framePixels.Width)
        {
            int sourceOffset = rowIndex * framePixels.Stride + columnIndex * bytesPerPixel;
            int destinationOffset = (yOffset + rowIndex) * canvas.Stride + (xOffset + columnIndex) * bytesPerPixel;
            ComposeOverPixel(framePixels.Data, sourceOffset, canvas.Data, destinationOffset, maxSample, alphaOffset, colorSamples);
            columnIndex += 1;
        }
    }

    internal static void ComposeOverPixel(byte[] source, int sourceOffset, byte[] destination, int destinationOffset, int maxSample, int alphaOffset, int colorSamples)
    {
        int sourceAlpha = ReadSample(source, sourceOffset + alphaOffset, maxSample);
        if (sourceAlpha == maxSample)
        {
            CopyBytes(source, sourceOffset, destination, destinationOffset, GetBytesPerPixelFromMaxSample(maxSample, colorSamples));
            return;
        }

        if (sourceAlpha == 0)
        {
            return;
        }

        int destinationAlpha = ReadSample(destination, destinationOffset + alphaOffset, maxSample);
        int outputAlpha = sourceAlpha + DivideRounded((long)destinationAlpha * (maxSample - sourceAlpha), maxSample);
        int sampleBytes = GetSampleBytes(maxSample);

        int sampleIndex = 0;
        while (sampleIndex < colorSamples)
        {
            int componentOffset = sampleIndex * sampleBytes;
            int sourceSample = ReadSample(source, sourceOffset + componentOffset, maxSample);
            int destinationSample = ReadSample(destination, destinationOffset + componentOffset, maxSample);
            int outputSample = BlendColorSample(sourceSample, sourceAlpha, destinationSample, destinationAlpha, outputAlpha, maxSample);
            WriteSample(destination, destinationOffset + componentOffset, outputSample, maxSample);
            sampleIndex += 1;
        }

        WriteSample(destination, destinationOffset + alphaOffset, outputAlpha, maxSample);
    }

    internal static int BlendColorSample(int sourceSample, int sourceAlpha, int destinationSample, int destinationAlpha, int outputAlpha, int maxSample)
    {
        if (outputAlpha == 0)
        {
            return 0;
        }

        long sourceContribution = (long)sourceSample * sourceAlpha * maxSample;
        long destinationContribution = (long)destinationSample * destinationAlpha * (maxSample - sourceAlpha);
        return DivideRounded(sourceContribution + destinationContribution, (long)outputAlpha * maxSample);
    }

    internal static PngPixelBuffer CopyCanvas(PngPixelBuffer canvas, int bytesPerPixel)
    {
        int rowBytes = CalculateRowBytes(canvas.Width, bytesPerPixel);
        byte[] data = CreateOutputData(rowBytes, canvas.Height);

        int rowIndex = 0;
        while (rowIndex < canvas.Height)
        {
            CopyBytes(canvas.Data, rowIndex * canvas.Stride, data, rowIndex * rowBytes, rowBytes);
            rowIndex += 1;
        }

        PngPixelBuffer result = new PngPixelBuffer();
        result.Format = canvas.Format;
        result.Width = canvas.Width;
        result.Height = canvas.Height;
        result.Stride = rowBytes;
        result.Data = data;
        return result;
    }

    internal static void ValidateBuffers(PngPixelBuffer canvas, PngPixelBuffer framePixels, int xOffset, int yOffset, int bytesPerPixel)
    {
        if (canvas.Format != framePixels.Format)
        {
            throw new ArgumentOutOfRangeException("Format");
        }

        ValidateBuffer(canvas, bytesPerPixel, "canvas");
        ValidateBuffer(framePixels, bytesPerPixel, "framePixels");
        ValidateRectangle(canvas, framePixels, xOffset, yOffset);
    }

    internal static void ValidateBuffer(PngPixelBuffer buffer, int bytesPerPixel, string name)
    {
        if (buffer.Width <= 0)
        {
            throw new ArgumentOutOfRangeException(name + "Width");
        }

        if (buffer.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(name + "Height");
        }

        int rowBytes = CalculateRowBytes(buffer.Width, bytesPerPixel);
        if (buffer.Stride < rowBytes)
        {
            throw new ArgumentOutOfRangeException(name + "Stride");
        }

        long requiredBytes = CalculateRequiredBytes(buffer.Stride, buffer.Height, rowBytes);
        if (requiredBytes > buffer.Data.Length)
        {
            throw new ArgumentOutOfRangeException(name + "Data");
        }
    }

    internal static void ValidateRectangle(PngPixelBuffer canvas, PngPixelBuffer framePixels, int xOffset, int yOffset)
    {
        if (xOffset < 0)
        {
            throw new ArgumentOutOfRangeException("xOffset");
        }

        if (yOffset < 0)
        {
            throw new ArgumentOutOfRangeException("yOffset");
        }

        if (xOffset > canvas.Width - framePixels.Width)
        {
            throw new ArgumentOutOfRangeException("xOffset");
        }

        if (yOffset > canvas.Height - framePixels.Height)
        {
            throw new ArgumentOutOfRangeException("yOffset");
        }
    }

    internal static long CalculateRequiredBytes(int stride, int height, int rowBytes)
    {
        if (height == 1)
        {
            return rowBytes;
        }

        return (long)stride * (height - 1) + rowBytes;
    }

    internal static int CalculateRowBytes(int width, int bytesPerPixel)
    {
        if (width > int.MaxValue / bytesPerPixel)
        {
            throw new OverflowException();
        }

        return width * bytesPerPixel;
    }

    internal static byte[] CreateOutputData(int rowBytes, int height)
    {
        long byteCount = (long)rowBytes * height;
        if (byteCount > int.MaxValue)
        {
            throw new OverflowException();
        }

        return new byte[(int)byteCount];
    }

    internal static int GetBytesPerPixel(PngPixelFormat format)
    {
        if (format == PngPixelFormat.GrayscaleAlpha8)
        {
            return 2;
        }

        if (format == PngPixelFormat.GrayscaleAlpha16)
        {
            return 4;
        }

        if (format == PngPixelFormat.Rgba8)
        {
            return 4;
        }

        if (format == PngPixelFormat.Rgba16)
        {
            return 8;
        }

        throw new ArgumentOutOfRangeException("Format");
    }

    internal static int GetMaxSample(PngPixelFormat format)
    {
        if (format == PngPixelFormat.GrayscaleAlpha16 || format == PngPixelFormat.Rgba16)
        {
            return 65535;
        }

        return 255;
    }

    internal static int GetAlphaOffset(PngPixelFormat format)
    {
        if (format == PngPixelFormat.GrayscaleAlpha8)
        {
            return 1;
        }

        if (format == PngPixelFormat.GrayscaleAlpha16)
        {
            return 2;
        }

        if (format == PngPixelFormat.Rgba8)
        {
            return 3;
        }

        if (format == PngPixelFormat.Rgba16)
        {
            return 6;
        }

        throw new ArgumentOutOfRangeException("Format");
    }

    internal static int GetColorSamples(PngPixelFormat format)
    {
        if (format == PngPixelFormat.GrayscaleAlpha8 || format == PngPixelFormat.GrayscaleAlpha16)
        {
            return 1;
        }

        return 3;
    }

    internal static int GetSampleBytes(int maxSample)
    {
        if (maxSample == 65535)
        {
            return 2;
        }

        return 1;
    }

    internal static int GetBytesPerPixelFromMaxSample(int maxSample, int colorSamples)
    {
        return (colorSamples + 1) * GetSampleBytes(maxSample);
    }

    internal static int ReadSample(byte[] data, int offset, int maxSample)
    {
        if (maxSample == 65535)
        {
            return data[offset] * 256 + data[offset + 1];
        }

        return data[offset];
    }

    internal static void WriteSample(byte[] data, int offset, int value, int maxSample)
    {
        if (maxSample == 65535)
        {
            data[offset] = (byte)(value / 256);
            data[offset + 1] = (byte)(value % 256);
            return;
        }

        data[offset] = (byte)value;
    }

    internal static int DivideRounded(long numerator, long denominator)
    {
        return (int)((numerator + denominator / 2) / denominator);
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
