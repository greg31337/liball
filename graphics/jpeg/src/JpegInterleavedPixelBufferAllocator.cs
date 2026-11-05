namespace jpeg;

internal class JpegInterleavedPixelBufferAllocator
{
    internal JpegInterleavedPixelBuffer Allocate(int width, int height, JpegPixelFormat pixelFormat, int componentCount)
    {
        int samplesPerPixel = ResolveComponentCount(pixelFormat, componentCount);
        int stride = GetStride(width, samplesPerPixel);
        return AllocateWithPrecisionAndStride(width, height, pixelFormat, componentCount, 8, stride);
    }

    internal JpegInterleavedPixelBuffer AllocateWithPrecisionAndStride(
        int width,
        int height,
        JpegPixelFormat pixelFormat,
        int componentCount,
        int precision,
        int stride)
    {
        ValidateDimensions(width, height);
        ValidatePrecision(precision);

        int samplesPerPixel = ResolveComponentCount(pixelFormat, componentCount);
        int minimumStride = GetStride(width, samplesPerPixel);
        ValidateStride(stride, minimumStride);
        int length = GetStridedBufferLength(stride, height);
        JpegInterleavedPixelBuffer buffer = new JpegInterleavedPixelBuffer();
        buffer.Width = width;
        buffer.Height = height;
        buffer.Precision = precision;
        buffer.ComponentCount = samplesPerPixel;
        buffer.PixelFormat = pixelFormat;
        buffer.SamplesPerPixel = samplesPerPixel;
        buffer.Stride = stride;
        buffer.Ownership = JpegBufferOwnership.LibraryOwned;
        if (precision == 8)
        {
            buffer.BytesPerPixel = GetBytesPerPixel(pixelFormat, componentCount);
            buffer.SampleStorage = JpegSampleStorage.Byte;
            buffer.Pixels = new byte[length];
        }
        else
        {
            buffer.BytesPerPixel = 0;
            buffer.SampleStorage = JpegSampleStorage.Int32;
            buffer.IntSamples = new int[length];
        }

        return buffer;
    }

    internal JpegInterleavedPixelBuffer AllocateForImage(JpegImage image)
    {
        if (image == null)
        {
            throw new System.ArgumentNullException("image");
        }

        int precision = image.Precision;
        if (precision == 0)
        {
            precision = 8;
        }

        int samplesPerPixel = ResolveComponentCount(image.PixelFormat, image.ComponentCount);
        int stride = image.RowStride;
        if (stride == 0)
        {
            stride = GetStride(image.Width, samplesPerPixel);
        }

        return AllocateWithPrecisionAndStride(image.Width, image.Height, image.PixelFormat, image.ComponentCount, precision, stride);
    }

    internal JpegImage CreateImage(int width, int height, JpegPixelFormat pixelFormat, JpegColorSpace colorSpace, int componentCount)
    {
        JpegInterleavedPixelBuffer buffer = Allocate(width, height, pixelFormat, componentCount);
        JpegImage image = new JpegImage();
        image.Width = buffer.Width;
        image.Height = buffer.Height;
        image.Precision = buffer.Precision;
        image.ComponentCount = buffer.ComponentCount;
        image.PixelFormat = buffer.PixelFormat;
        image.ColorSpace = colorSpace;
        image.BufferLayout = JpegBufferLayout.Interleaved;
        image.BufferOwnership = buffer.Ownership;
        image.RowStride = buffer.Stride;
        image.Pixels = buffer.Pixels;
        image.InterleavedBuffer = buffer;
        return image;
    }

    internal void ValidatePrecision(int precision)
    {
        if (precision != 8 && precision != 12 && precision != 16)
        {
            throw JpegInvalidDataException.Create("Pixel buffer precision must be 8, 12, or 16 bits.");
        }
    }

    internal void ValidateStride(int stride, int minimumStride)
    {
        if (stride < minimumStride)
        {
            throw JpegInvalidDataException.Create("Pixel buffer stride is smaller than one packed sample row.");
        }
    }

    internal int GetStridedBufferLength(int stride, int height)
    {
        if (stride < 0)
        {
            throw new System.ArgumentOutOfRangeException("stride");
        }

        if (height < 0)
        {
            throw JpegInvalidDataException.Create("Pixel buffer height cannot be negative.");
        }

        long length = (long)stride * (long)height;
        if (length > int.MaxValue)
        {
            throw JpegInvalidDataException.Create("Pixel buffer is too large.");
        }

        return (int)length;
    }

    internal int GetBytesPerPixel(JpegPixelFormat pixelFormat, int componentCount)
    {
        if (pixelFormat == JpegPixelFormat.L8)
        {
            return 1;
        }

        if (pixelFormat == JpegPixelFormat.Rgb24 || pixelFormat == JpegPixelFormat.Bgr24 || pixelFormat == JpegPixelFormat.YCbCr24)
        {
            return 3;
        }

        if (pixelFormat == JpegPixelFormat.Rgba32
            || pixelFormat == JpegPixelFormat.Bgra32
            || pixelFormat == JpegPixelFormat.Cmyk32
            || pixelFormat == JpegPixelFormat.Ycck32)
        {
            return 4;
        }

        if (pixelFormat == JpegPixelFormat.RawComponents)
        {
            if (componentCount <= 0)
            {
                throw JpegInvalidDataException.Create("Raw component pixel buffers require a positive component count.");
            }

            return componentCount;
        }

        throw JpegInvalidDataException.Create("Pixel format has an unknown pixel size.");
    }

    internal int ResolveComponentCount(JpegPixelFormat pixelFormat, int componentCount)
    {
        if (pixelFormat == JpegPixelFormat.L8)
        {
            return 1;
        }

        if (pixelFormat == JpegPixelFormat.Rgb24 || pixelFormat == JpegPixelFormat.Bgr24 || pixelFormat == JpegPixelFormat.YCbCr24)
        {
            return 3;
        }

        if (pixelFormat == JpegPixelFormat.Rgba32
            || pixelFormat == JpegPixelFormat.Bgra32
            || pixelFormat == JpegPixelFormat.Cmyk32
            || pixelFormat == JpegPixelFormat.Ycck32)
        {
            return 4;
        }

        if (pixelFormat == JpegPixelFormat.RawComponents && componentCount > 0)
        {
            return componentCount;
        }

        throw JpegInvalidDataException.Create("Pixel format has an unknown component count.");
    }

    internal int GetStride(int width, int bytesPerPixel)
    {
        ValidateDimensions(width, 0);
        if (bytesPerPixel <= 0)
        {
            throw new System.ArgumentOutOfRangeException("bytesPerPixel");
        }

        long stride = (long)width * (long)bytesPerPixel;
        if (stride > int.MaxValue)
        {
            throw JpegInvalidDataException.Create("Pixel buffer stride is too large.");
        }

        return (int)stride;
    }

    internal int GetBufferLength(int width, int height, int bytesPerPixel)
    {
        if (height < 0)
        {
            throw JpegInvalidDataException.Create("Pixel buffer height cannot be negative.");
        }

        int stride = GetStride(width, bytesPerPixel);
        long length = (long)stride * (long)height;
        if (length > int.MaxValue)
        {
            throw JpegInvalidDataException.Create("Pixel buffer is too large.");
        }

        return (int)length;
    }

    internal void ValidateDimensions(int width, int height)
    {
        if (width <= 0)
        {
            throw JpegInvalidDataException.Create("Pixel buffer width must be greater than zero.");
        }

        if (height < 0)
        {
            throw JpegInvalidDataException.Create("Pixel buffer height cannot be negative.");
        }
    }
}
