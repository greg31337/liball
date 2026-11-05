namespace png;

internal class PngApngFrameDisposer
{
    internal static PngPixelBuffer Apply(PngPixelBuffer canvas, PngFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        if (frame.DisposeOperation == PngApngDisposeOperation.None)
        {
            return ApplyNone(canvas);
        }

        if (frame.DisposeOperation == PngApngDisposeOperation.Background)
        {
            return ApplyBackground(canvas, frame);
        }

        throw new ArgumentOutOfRangeException("DisposeOperation");
    }

    internal static PngPixelBuffer Apply(PngPixelBuffer canvas, PngFrame frame, PngPixelBuffer previousCanvas)
    {
        ArgumentNullException.ThrowIfNull(frame);

        if (frame.DisposeOperation == PngApngDisposeOperation.None)
        {
            return ApplyNone(canvas);
        }

        if (frame.DisposeOperation == PngApngDisposeOperation.Background)
        {
            return ApplyBackground(canvas, frame);
        }

        if (frame.DisposeOperation == PngApngDisposeOperation.Previous)
        {
            return ApplyPrevious(canvas, previousCanvas);
        }

        throw new ArgumentOutOfRangeException("DisposeOperation");
    }

    internal static PngPixelBuffer Apply(PngPixelBuffer canvas, PngApngDisposeOperation disposeOperation)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(canvas.Data);

        if (disposeOperation == PngApngDisposeOperation.None)
        {
            return ApplyNone(canvas);
        }

        throw new ArgumentOutOfRangeException("disposeOperation");
    }

    internal static PngPixelBuffer ApplyNone(PngPixelBuffer canvas)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(canvas.Data);

        ValidateBuffer(canvas);
        return CopyBuffer(canvas);
    }

    internal static PngPixelBuffer ApplyBackground(PngPixelBuffer canvas, PngFrame frame)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(canvas.Data);
        ArgumentNullException.ThrowIfNull(frame);

        int bytesPerPixel = GetBackgroundBytesPerPixel(canvas.Format);
        ValidateBuffer(canvas);
        ValidateFrameRectangle(canvas, frame);

        PngPixelBuffer result = CopyBuffer(canvas);
        ClearRectangle(result, frame.XOffset, frame.YOffset, frame.Width, frame.Height, bytesPerPixel);
        return result;
    }

    internal static PngPixelBuffer ApplyPrevious(PngPixelBuffer canvas, PngPixelBuffer previousCanvas)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(canvas.Data);
        ArgumentNullException.ThrowIfNull(previousCanvas);
        ArgumentNullException.ThrowIfNull(previousCanvas.Data);

        ValidateBuffer(canvas);
        ValidateBuffer(previousCanvas);
        ValidatePreviousBuffer(canvas, previousCanvas);
        return CopyBuffer(previousCanvas);
    }

    internal static void ClearRectangle(PngPixelBuffer canvas, int xOffset, int yOffset, int width, int height, int bytesPerPixel)
    {
        int rowBytes = width * bytesPerPixel;
        int rowIndex = 0;
        while (rowIndex < height)
        {
            int offset = (yOffset + rowIndex) * canvas.Stride + xOffset * bytesPerPixel;
            ClearBytes(canvas.Data, offset, rowBytes);
            rowIndex += 1;
        }
    }

    internal static void ClearBytes(byte[] data, int offset, int length)
    {
        int index = 0;
        while (index < length)
        {
            data[offset + index] = 0;
            index += 1;
        }
    }

    internal static PngPixelBuffer CopyBuffer(PngPixelBuffer canvas)
    {
        byte[] data = new byte[canvas.Data.Length];
        CopyBytes(canvas.Data, 0, data, 0, canvas.Data.Length);

        PngPixelBuffer result = new PngPixelBuffer();
        result.Format = canvas.Format;
        result.Width = canvas.Width;
        result.Height = canvas.Height;
        result.Stride = canvas.Stride;
        result.Data = data;
        return result;
    }

    internal static void ValidateBuffer(PngPixelBuffer buffer)
    {
        long rowBytes = PngSizeCalculator.CalculateFrameRowBytes(buffer.Width, buffer.Format);
        if (rowBytes > int.MaxValue)
        {
            throw new OverflowException();
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

    internal static void ValidateFrameRectangle(PngPixelBuffer canvas, PngFrame frame)
    {
        if (frame.Width <= 0)
        {
            throw new ArgumentOutOfRangeException("Width");
        }

        if (frame.Height <= 0)
        {
            throw new ArgumentOutOfRangeException("Height");
        }

        if (frame.XOffset < 0)
        {
            throw new ArgumentOutOfRangeException("XOffset");
        }

        if (frame.YOffset < 0)
        {
            throw new ArgumentOutOfRangeException("YOffset");
        }

        if (frame.XOffset > canvas.Width - frame.Width)
        {
            throw new ArgumentOutOfRangeException("XOffset");
        }

        if (frame.YOffset > canvas.Height - frame.Height)
        {
            throw new ArgumentOutOfRangeException("YOffset");
        }
    }

    internal static void ValidatePreviousBuffer(PngPixelBuffer canvas, PngPixelBuffer previousCanvas)
    {
        if (canvas.Format != previousCanvas.Format)
        {
            throw new ArgumentOutOfRangeException("Format");
        }

        if (canvas.Width != previousCanvas.Width)
        {
            throw new ArgumentOutOfRangeException("Width");
        }

        if (canvas.Height != previousCanvas.Height)
        {
            throw new ArgumentOutOfRangeException("Height");
        }
    }

    internal static long CalculateRequiredBytes(int stride, int height, long rowBytes)
    {
        if (height == 1)
        {
            return rowBytes;
        }

        return (long)stride * (height - 1) + rowBytes;
    }

    internal static int GetBackgroundBytesPerPixel(PngPixelFormat format)
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
