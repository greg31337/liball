namespace jpeg;

internal class JpegRgbToYCbCrConverter
{
    internal JpegInterleavedPixelBufferAllocator Allocator = new JpegInterleavedPixelBufferAllocator();

    internal JpegInterleavedPixelBuffer ConvertImageToYCbCrBuffer(JpegImage source)
    {
        ValidateRgbImage(source);

        JpegInterleavedPixelBuffer destination = Allocator.Allocate(source.Width, source.Height, JpegPixelFormat.YCbCr24, 0);
        CopyPackedRgbToYCbCr(source.Pixels, source.Width, source.Height, destination.Stride, destination.Pixels, destination.Stride);
        return destination;
    }

    internal JpegImage ConvertRgbImageToYCbCrImage(JpegImage source)
    {
        JpegInterleavedPixelBuffer buffer = ConvertImageToYCbCrBuffer(source);

        int precision = source.Precision;
        if (precision <= 0)
        {
            precision = 8;
        }

        JpegImage image = new JpegImage();
        image.Width = buffer.Width;
        image.Height = buffer.Height;
        image.Precision = precision;
        image.ComponentCount = 3;
        image.PixelFormat = JpegPixelFormat.YCbCr24;
        image.ColorSpace = JpegColorSpace.YCbCr;
        image.Subsampling = JpegSubsampling.Ratio444;
        image.Pixels = buffer.Pixels;
        return image;
    }

    internal byte[] ConvertPackedRgbToYCbCr(byte[] source, int width, int height)
    {
        ValidateDimensions(width, height);

        int destinationStride = GetPackedRowByteCount(width);
        byte[] destination = new byte[GetStridedLength(destinationStride, height)];
        CopyPackedRgbToYCbCr(source, width, height, destinationStride, destination, destinationStride);
        return destination;
    }

    internal void CopyPackedRgbToYCbCr(
        byte[] source,
        int width,
        int height,
        int sourceStride,
        byte[] destination,
        int destinationStride)
    {
        int rowByteCount = ValidateRgbSource(source, width, height, sourceStride);
        ValidateYCbCrDestination(destination, width, height, destinationStride);

        int y = 0;
        while (y < height)
        {
            int sourceIndex = y * sourceStride;
            int destinationIndex = y * destinationStride;
            int rowEnd = sourceIndex + rowByteCount;
            while (sourceIndex < rowEnd)
            {
                WriteYCbCrPixel(source[sourceIndex], source[sourceIndex + 1], source[sourceIndex + 2], destination, destinationIndex);
                sourceIndex = sourceIndex + 3;
                destinationIndex = destinationIndex + 3;
            }

            y = y + 1;
        }
    }

    internal void WriteYCbCrPixel(int red, int green, int blue, byte[] destination, int destinationIndex)
    {
        ValidateRgbSample(red, green, blue);

        if (destination == null)
        {
            throw new System.ArgumentNullException("destination");
        }

        if (destinationIndex < 0 || destinationIndex + 2 >= destination.Length)
        {
            throw JpegInvalidDataException.Create("YCbCr destination index is outside the buffer.");
        }

        destination[destinationIndex] = (byte)ConvertSampleToY(red, green, blue);
        destination[destinationIndex + 1] = (byte)ConvertSampleToCb(red, green, blue);
        destination[destinationIndex + 2] = (byte)ConvertSampleToCr(red, green, blue);
    }

    internal int ConvertSampleToY(int red, int green, int blue)
    {
        ValidateRgbSample(red, green, blue);
        int value = (19595 * red + 38470 * green + 7471 * blue + 32768) >> 16;
        return ClampToByte(value);
    }

    internal int ConvertSampleToCb(int red, int green, int blue)
    {
        ValidateRgbSample(red, green, blue);
        int value = 128 + ((-11059 * red - 21709 * green + 32768 * blue + 32768) >> 16);
        return ClampToByte(value);
    }

    internal int ConvertSampleToCr(int red, int green, int blue)
    {
        ValidateRgbSample(red, green, blue);
        int value = 128 + ((32768 * red - 27439 * green - 5329 * blue + 32768) >> 16);
        return ClampToByte(value);
    }

    internal int ClampToByte(int value)
    {
        if (value < 0)
        {
            return 0;
        }

        if (value > 255)
        {
            return 255;
        }

        return value;
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
            throw JpegInvalidDataException.Create("RGB to YCbCr conversion requires Rgb24 input.");
        }

        if (source.ComponentCount != 3)
        {
            throw JpegInvalidDataException.Create("RGB image must contain three components.");
        }

        ValidateRgbSource(source.Pixels, source.Width, source.Height, GetPackedRowByteCount(source.Width));
    }

    internal int ValidateRgbSource(byte[] source, int width, int height, int sourceStride)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        ValidateDimensions(width, height);

        int rowByteCount = GetPackedRowByteCount(width);
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

    internal void ValidateYCbCrDestination(byte[] destination, int width, int height, int destinationStride)
    {
        if (destination == null)
        {
            throw new System.ArgumentNullException("destination");
        }

        ValidateDimensions(width, height);

        int rowByteCount = GetPackedRowByteCount(width);
        if (destinationStride < rowByteCount)
        {
            throw JpegInvalidDataException.Create("YCbCr destination stride cannot be smaller than one packed row.");
        }

        int minimumLength = GetStridedLength(destinationStride, height);
        if (destination.Length < minimumLength)
        {
            throw JpegInvalidDataException.Create("YCbCr destination buffer is too small.");
        }
    }

    internal void ValidateDimensions(int width, int height)
    {
        if (width <= 0)
        {
            throw JpegInvalidDataException.Create("RGB conversion width must be greater than zero.");
        }

        if (height < 0)
        {
            throw JpegInvalidDataException.Create("RGB conversion height cannot be negative.");
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

    internal int GetPackedRowByteCount(int width)
    {
        if (width <= 0)
        {
            throw JpegInvalidDataException.Create("Packed row width must be greater than zero.");
        }

        long rowByteCount = (long)width * 3L;
        if (rowByteCount > int.MaxValue)
        {
            throw JpegInvalidDataException.Create("Packed row byte count is too large.");
        }

        return (int)rowByteCount;
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
