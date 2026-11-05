namespace jpeg;

internal class JpegCmykToRgbConverter
{
    internal JpegInterleavedPixelBufferAllocator Allocator = new JpegInterleavedPixelBufferAllocator();

    internal JpegInterleavedPixelBuffer ConvertImageToRgbBuffer(JpegImage source)
    {
        ValidateCmykImage(source);

        JpegInterleavedPixelBuffer destination = Allocator.Allocate(source.Width, source.Height, JpegPixelFormat.Rgb24, 0);
        CopyPackedCmykToRgb(
            source.Pixels,
            source.Width,
            source.Height,
            GetCmykRowByteCount(source.Width),
            destination.Pixels,
            destination.Stride);
        return destination;
    }

    internal JpegImage ConvertImageToRgbImage(JpegImage source)
    {
        JpegInterleavedPixelBuffer buffer = ConvertImageToRgbBuffer(source);

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
        image.PixelFormat = JpegPixelFormat.Rgb24;
        image.ColorSpace = JpegColorSpace.RGB;
        image.Pixels = buffer.Pixels;
        return image;
    }

    internal byte[] ConvertPackedCmykToRgb(byte[] source, int width, int height)
    {
        ValidateDimensions(width, height);

        int sourceStride = GetCmykRowByteCount(width);
        int destinationStride = GetRgbRowByteCount(width);
        byte[] destination = new byte[GetStridedLength(destinationStride, height)];
        CopyPackedCmykToRgb(source, width, height, sourceStride, destination, destinationStride);
        return destination;
    }

    internal void CopyPackedCmykToRgb(
        byte[] source,
        int width,
        int height,
        int sourceStride,
        byte[] destination,
        int destinationStride)
    {
        int sourceRowByteCount = ValidateCmykSource(source, width, height, sourceStride);
        ValidateRgbDestination(destination, width, height, destinationStride);

        int y = 0;
        while (y < height)
        {
            int sourceIndex = y * sourceStride;
            int destinationIndex = y * destinationStride;
            int rowEnd = sourceIndex + sourceRowByteCount;
            while (sourceIndex < rowEnd)
            {
                WriteRgbPixel(
                    source[sourceIndex],
                    source[sourceIndex + 1],
                    source[sourceIndex + 2],
                    source[sourceIndex + 3],
                    destination,
                    destinationIndex);
                sourceIndex = sourceIndex + 4;
                destinationIndex = destinationIndex + 3;
            }

            y = y + 1;
        }
    }

    internal void WriteRgbPixel(int cyan, int magenta, int yellow, int black, byte[] destination, int destinationIndex)
    {
        ValidateCmykSample(cyan, magenta, yellow, black);

        if (destination == null)
        {
            throw new System.ArgumentNullException("destination");
        }

        if (destinationIndex < 0 || destinationIndex + 2 >= destination.Length)
        {
            throw JpegInvalidDataException.Create("RGB destination index is outside the buffer.");
        }

        destination[destinationIndex] = (byte)ConvertSampleToRed(cyan, magenta, yellow, black);
        destination[destinationIndex + 1] = (byte)ConvertSampleToGreen(cyan, magenta, yellow, black);
        destination[destinationIndex + 2] = (byte)ConvertSampleToBlue(cyan, magenta, yellow, black);
    }

    internal int ConvertSampleToRed(int cyan, int magenta, int yellow, int black)
    {
        ValidateCmykSample(cyan, magenta, yellow, black);
        return ScaleSubtractiveChannel(cyan, black);
    }

    internal int ConvertSampleToGreen(int cyan, int magenta, int yellow, int black)
    {
        ValidateCmykSample(cyan, magenta, yellow, black);
        return ScaleSubtractiveChannel(magenta, black);
    }

    internal int ConvertSampleToBlue(int cyan, int magenta, int yellow, int black)
    {
        ValidateCmykSample(cyan, magenta, yellow, black);
        return ScaleSubtractiveChannel(yellow, black);
    }

    internal int ScaleSubtractiveChannel(int ink, int black)
    {
        ValidateSample(ink, "ink");
        ValidateSample(black, "black");

        int lightness = 255 - ink;
        int blackScale = 255 - black;
        return (lightness * blackScale + 127) / 255;
    }

    internal void ValidateCmykImage(JpegImage source)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        ValidateDimensions(source.Width, source.Height);

        if (source.PixelFormat != JpegPixelFormat.Cmyk32)
        {
            throw JpegInvalidDataException.Create("CMYK to RGB fallback requires Cmyk32 input.");
        }

        if (source.ComponentCount != 4)
        {
            throw JpegInvalidDataException.Create("CMYK image must contain four components.");
        }

        ValidateCmykSource(source.Pixels, source.Width, source.Height, GetCmykRowByteCount(source.Width));
    }

    internal int ValidateCmykSource(byte[] source, int width, int height, int sourceStride)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        ValidateDimensions(width, height);

        int rowByteCount = GetCmykRowByteCount(width);
        if (sourceStride < rowByteCount)
        {
            throw JpegInvalidDataException.Create("CMYK source stride cannot be smaller than one packed row.");
        }

        int minimumLength = GetStridedLength(sourceStride, height);
        if (source.Length < minimumLength)
        {
            throw JpegInvalidDataException.Create("CMYK source buffer is too small.");
        }

        return rowByteCount;
    }

    internal void ValidateRgbDestination(byte[] destination, int width, int height, int destinationStride)
    {
        if (destination == null)
        {
            throw new System.ArgumentNullException("destination");
        }

        ValidateDimensions(width, height);

        int rowByteCount = GetRgbRowByteCount(width);
        if (destinationStride < rowByteCount)
        {
            throw JpegInvalidDataException.Create("RGB destination stride cannot be smaller than one packed row.");
        }

        int minimumLength = GetStridedLength(destinationStride, height);
        if (destination.Length < minimumLength)
        {
            throw JpegInvalidDataException.Create("RGB destination buffer is too small.");
        }
    }

    internal void ValidateDimensions(int width, int height)
    {
        if (width <= 0)
        {
            throw JpegInvalidDataException.Create("CMYK to RGB width must be greater than zero.");
        }

        if (height < 0)
        {
            throw JpegInvalidDataException.Create("CMYK to RGB height cannot be negative.");
        }
    }

    internal void ValidateCmykSample(int cyan, int magenta, int yellow, int black)
    {
        ValidateSample(cyan, "cyan");
        ValidateSample(magenta, "magenta");
        ValidateSample(yellow, "yellow");
        ValidateSample(black, "black");
    }

    internal void ValidateSample(int sample, string parameterName)
    {
        if (sample < 0 || sample > 255)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }

    internal int GetCmykRowByteCount(int width)
    {
        if (width <= 0)
        {
            throw JpegInvalidDataException.Create("Packed row width must be greater than zero.");
        }

        long rowByteCount = (long)width * 4L;
        if (rowByteCount > int.MaxValue)
        {
            throw JpegInvalidDataException.Create("CMYK row byte count is too large.");
        }

        return (int)rowByteCount;
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
