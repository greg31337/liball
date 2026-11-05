namespace jpeg;

internal class JpegRgbOutputPacker
{
    internal JpegInterleavedPixelBufferAllocator Allocator = new JpegInterleavedPixelBufferAllocator();

    internal JpegInterleavedPixelBuffer PackImage(JpegImage source, JpegPixelFormat outputPixelFormat)
    {
        ValidateRgbImage(source);
        ValidateOutputPixelFormat(outputPixelFormat);

        JpegInterleavedPixelBuffer destination = Allocator.Allocate(source.Width, source.Height, outputPixelFormat, 0);
        CopyRgbToFormat(
            source.Pixels,
            source.Width,
            source.Height,
            GetRgbRowByteCount(source.Width),
            destination.Pixels,
            destination.Stride,
            outputPixelFormat);
        return destination;
    }

    internal JpegImage PackImageToImage(JpegImage source, JpegPixelFormat outputPixelFormat)
    {
        JpegInterleavedPixelBuffer buffer = PackImage(source, outputPixelFormat);

        int precision = source.Precision;
        if (precision <= 0)
        {
            precision = 8;
        }

        JpegImage image = new JpegImage();
        image.Width = buffer.Width;
        image.Height = buffer.Height;
        image.Precision = precision;
        image.ComponentCount = buffer.ComponentCount;
        image.PixelFormat = outputPixelFormat;
        image.ColorSpace = JpegColorSpace.RGB;
        image.Pixels = buffer.Pixels;
        return image;
    }

    internal byte[] PackRgb(byte[] source, int width, int height, JpegPixelFormat outputPixelFormat)
    {
        ValidateDimensions(width, height);
        ValidateOutputPixelFormat(outputPixelFormat);

        int sourceStride = GetRgbRowByteCount(width);
        int destinationStride = GetDestinationRowByteCount(width, outputPixelFormat);
        byte[] destination = new byte[GetStridedLength(destinationStride, height)];
        CopyRgbToFormat(source, width, height, sourceStride, destination, destinationStride, outputPixelFormat);
        return destination;
    }

    internal void CopyRgbToFormat(
        byte[] source,
        int width,
        int height,
        int sourceStride,
        byte[] destination,
        int destinationStride,
        JpegPixelFormat outputPixelFormat)
    {
        int sourceRowByteCount = ValidateRgbSource(source, width, height, sourceStride);
        ValidateDestination(destination, width, height, destinationStride, outputPixelFormat);

        int y = 0;
        while (y < height)
        {
            int sourceIndex = y * sourceStride;
            int destinationIndex = y * destinationStride;
            int rowEnd = sourceIndex + sourceRowByteCount;
            while (sourceIndex < rowEnd)
            {
                WritePackedPixel(source[sourceIndex], source[sourceIndex + 1], source[sourceIndex + 2], destination, destinationIndex, outputPixelFormat);
                sourceIndex = sourceIndex + 3;
                destinationIndex = destinationIndex + GetOutputBytesPerPixel(outputPixelFormat);
            }

            y = y + 1;
        }
    }

    internal void WritePackedPixel(
        int red,
        int green,
        int blue,
        byte[] destination,
        int destinationIndex,
        JpegPixelFormat outputPixelFormat)
    {
        ValidateRgbSample(red, green, blue);
        ValidateOutputPixelFormat(outputPixelFormat);

        if (destination == null)
        {
            throw new System.ArgumentNullException("destination");
        }

        int bytesPerPixel = GetOutputBytesPerPixel(outputPixelFormat);
        if (destinationIndex < 0 || destinationIndex + bytesPerPixel > destination.Length)
        {
            throw JpegInvalidDataException.Create("RGB output destination index is outside the buffer.");
        }

        if (outputPixelFormat == JpegPixelFormat.Rgb24)
        {
            destination[destinationIndex] = (byte)red;
            destination[destinationIndex + 1] = (byte)green;
            destination[destinationIndex + 2] = (byte)blue;
            return;
        }

        if (outputPixelFormat == JpegPixelFormat.Bgr24)
        {
            destination[destinationIndex] = (byte)blue;
            destination[destinationIndex + 1] = (byte)green;
            destination[destinationIndex + 2] = (byte)red;
            return;
        }

        if (outputPixelFormat == JpegPixelFormat.Rgba32)
        {
            destination[destinationIndex] = (byte)red;
            destination[destinationIndex + 1] = (byte)green;
            destination[destinationIndex + 2] = (byte)blue;
            destination[destinationIndex + 3] = 255;
            return;
        }

        destination[destinationIndex] = (byte)blue;
        destination[destinationIndex + 1] = (byte)green;
        destination[destinationIndex + 2] = (byte)red;
        destination[destinationIndex + 3] = 255;
    }

    internal void ValidateRgbImage(JpegImage source)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        ValidateDimensions(source.Width, source.Height);

        if (source.PixelFormat != JpegPixelFormat.Rgb24)
        {
            throw JpegInvalidDataException.Create("RGB output packing requires Rgb24 input.");
        }

        if (source.ComponentCount != 3)
        {
            throw JpegInvalidDataException.Create("RGB image must contain three components.");
        }

        ValidateRgbSource(source.Pixels, source.Width, source.Height, GetRgbRowByteCount(source.Width));
    }

    internal int ValidateRgbSource(byte[] source, int width, int height, int sourceStride)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        ValidateDimensions(width, height);

        int rowByteCount = GetRgbRowByteCount(width);
        if (sourceStride < rowByteCount)
        {
            throw JpegInvalidDataException.Create("RGB source stride cannot be smaller than one packed row.");
        }

        int minimumLength = GetStridedLength(sourceStride, height);
        if (source.Length < minimumLength)
        {
            throw JpegInvalidDataException.Create("RGB source buffer is too small.");
        }

        return rowByteCount;
    }

    internal void ValidateDestination(byte[] destination, int width, int height, int destinationStride, JpegPixelFormat outputPixelFormat)
    {
        if (destination == null)
        {
            throw new System.ArgumentNullException("destination");
        }

        ValidateDimensions(width, height);
        ValidateOutputPixelFormat(outputPixelFormat);

        int rowByteCount = GetDestinationRowByteCount(width, outputPixelFormat);
        if (destinationStride < rowByteCount)
        {
            throw JpegInvalidDataException.Create("RGB output destination stride cannot be smaller than one packed row.");
        }

        int minimumLength = GetStridedLength(destinationStride, height);
        if (destination.Length < minimumLength)
        {
            throw JpegInvalidDataException.Create("RGB output destination buffer is too small.");
        }
    }

    internal void ValidateOutputPixelFormat(JpegPixelFormat outputPixelFormat)
    {
        if (outputPixelFormat == JpegPixelFormat.Rgb24)
        {
            return;
        }

        if (outputPixelFormat == JpegPixelFormat.Bgr24)
        {
            return;
        }

        if (outputPixelFormat == JpegPixelFormat.Rgba32)
        {
            return;
        }

        if (outputPixelFormat == JpegPixelFormat.Bgra32)
        {
            return;
        }

        throw JpegInvalidDataException.Create("RGB output packing supports Rgb24, Bgr24, Rgba32, and Bgra32.");
    }

    internal void ValidateDimensions(int width, int height)
    {
        if (width <= 0)
        {
            throw JpegInvalidDataException.Create("RGB output width must be greater than zero.");
        }

        if (height < 0)
        {
            throw JpegInvalidDataException.Create("RGB output height cannot be negative.");
        }
    }

    internal void ValidateRgbSample(int red, int green, int blue)
    {
        ValidateSample(red, "red");
        ValidateSample(green, "green");
        ValidateSample(blue, "blue");
    }

    internal void ValidateSample(int sample, string parameterName)
    {
        if (sample < 0 || sample > 255)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }

    internal int GetRgbRowByteCount(int width)
    {
        if (width <= 0)
        {
            throw JpegInvalidDataException.Create("Packed row width must be greater than zero.");
        }

        long rowByteCount = (long)width * 3L;
        if (rowByteCount > int.MaxValue)
        {
            throw JpegInvalidDataException.Create("RGB row byte count is too large.");
        }

        return (int)rowByteCount;
    }

    internal int GetDestinationRowByteCount(int width, JpegPixelFormat outputPixelFormat)
    {
        int bytesPerPixel = GetOutputBytesPerPixel(outputPixelFormat);
        long rowByteCount = (long)width * (long)bytesPerPixel;
        if (rowByteCount > int.MaxValue)
        {
            throw JpegInvalidDataException.Create("RGB output row byte count is too large.");
        }

        return (int)rowByteCount;
    }

    internal int GetOutputBytesPerPixel(JpegPixelFormat outputPixelFormat)
    {
        ValidateOutputPixelFormat(outputPixelFormat);
        return Allocator.GetBytesPerPixel(outputPixelFormat, 0);
    }

    internal int GetStridedLength(int stride, int height)
    {
        if (stride < 0)
        {
            throw new System.ArgumentOutOfRangeException("stride");
        }

        if (height < 0)
        {
            throw new System.ArgumentOutOfRangeException("height");
        }

        long length = (long)stride * (long)height;
        if (length > int.MaxValue)
        {
            throw JpegInvalidDataException.Create("Strided buffer is too large.");
        }

        return (int)length;
    }
}
