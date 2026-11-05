namespace jpeg;

internal class JpegYCbCrToRgbConverter
{
    internal JpegInterleavedPixelBufferAllocator Allocator = new JpegInterleavedPixelBufferAllocator();

    internal JpegInterleavedPixelBuffer ConvertPlanarToRgb(JpegPlanarImageBuffer source)
    {
        ValidatePlanarBuffer(source);

        JpegInterleavedPixelBuffer destination = Allocator.Allocate(source.Width, source.Height, JpegPixelFormat.Rgb24, 0);
        CopyPlanarToRgb(
            source.Components[0],
            source.Components[1],
            source.Components[2],
            source.Width,
            source.Height,
            destination.Pixels,
            destination.Stride);
        return destination;
    }

    internal JpegImage ConvertPlanarToImage(JpegPlanarImageBuffer source)
    {
        JpegInterleavedPixelBuffer buffer = ConvertPlanarToRgb(source);

        JpegImage image = new JpegImage();
        image.Width = buffer.Width;
        image.Height = buffer.Height;
        image.Precision = 8;
        image.ComponentCount = 3;
        image.PixelFormat = JpegPixelFormat.Rgb24;
        image.ColorSpace = JpegColorSpace.RGB;
        image.Pixels = buffer.Pixels;
        return image;
    }

    internal byte[] ConvertPackedYCbCrToRgb(byte[] source, int width, int height)
    {
        ValidateDimensions(width, height);

        int destinationStride = width * 3;
        byte[] destination = new byte[GetStridedLength(destinationStride, height)];
        CopyPackedYCbCrToRgb(source, width, height, destinationStride, destination, destinationStride);
        return destination;
    }

    internal void CopyPackedYCbCrToRgb(
        byte[] source,
        int width,
        int height,
        int sourceStride,
        byte[] destination,
        int destinationStride)
    {
        int rowByteCount = ValidatePackedSource(source, width, height, sourceStride);
        ValidateRgbDestination(destination, width, height, destinationStride);

        int y = 0;
        while (y < height)
        {
            int sourceIndex = y * sourceStride;
            int destinationIndex = y * destinationStride;
            int rowEnd = sourceIndex + rowByteCount;
            while (sourceIndex < rowEnd)
            {
                WriteRgbPixel(source[sourceIndex], source[sourceIndex + 1], source[sourceIndex + 2], destination, destinationIndex);
                sourceIndex = sourceIndex + 3;
                destinationIndex = destinationIndex + 3;
            }

            y = y + 1;
        }
    }

    internal void CopyPlanarToRgb(
        JpegPlanarComponentBuffer luminance,
        JpegPlanarComponentBuffer cb,
        JpegPlanarComponentBuffer cr,
        int outputWidth,
        int outputHeight,
        byte[] destination,
        int destinationStride)
    {
        ValidateComponentForOutput(luminance, outputWidth, outputHeight, "luminance");
        ValidateComponentForOutput(cb, outputWidth, outputHeight, "cb");
        ValidateComponentForOutput(cr, outputWidth, outputHeight, "cr");
        ValidateRgbDestination(destination, outputWidth, outputHeight, destinationStride);

        int y = 0;
        while (y < outputHeight)
        {
            int luminanceIndex = y * luminance.Stride;
            int cbIndex = y * cb.Stride;
            int crIndex = y * cr.Stride;
            int destinationIndex = y * destinationStride;
            int x = 0;
            while (x < outputWidth)
            {
                WriteRgbPixel(luminance.Samples[luminanceIndex], cb.Samples[cbIndex], cr.Samples[crIndex], destination, destinationIndex);
                luminanceIndex = luminanceIndex + 1;
                cbIndex = cbIndex + 1;
                crIndex = crIndex + 1;
                destinationIndex = destinationIndex + 3;
                x = x + 1;
            }

            y = y + 1;
        }
    }

    internal void WriteRgbPixel(int y, int cb, int cr, byte[] destination, int destinationIndex)
    {
        ValidateSample(y, "y");
        ValidateSample(cb, "cb");
        ValidateSample(cr, "cr");

        if (destination == null)
        {
            throw new System.ArgumentNullException("destination");
        }

        if (destinationIndex < 0 || destinationIndex + 2 >= destination.Length)
        {
            throw JpegInvalidDataException.Create("RGB destination index is outside the buffer.");
        }

        destination[destinationIndex] = (byte)ConvertSampleToRed(y, cb, cr);
        destination[destinationIndex + 1] = (byte)ConvertSampleToGreen(y, cb, cr);
        destination[destinationIndex + 2] = (byte)ConvertSampleToBlue(y, cb, cr);
    }

    internal int ConvertSampleToRed(int y, int cb, int cr)
    {
        ValidateYCbCrSample(y, cb, cr);
        int centeredCr = cr - 128;
        int value = y + ((91881 * centeredCr + 32768) >> 16);
        return ClampToByte(value);
    }

    internal int ConvertSampleToGreen(int y, int cb, int cr)
    {
        ValidateYCbCrSample(y, cb, cr);
        int centeredCb = cb - 128;
        int centeredCr = cr - 128;
        int value = y - ((22554 * centeredCb + 46802 * centeredCr + 32768) >> 16);
        return ClampToByte(value);
    }

    internal int ConvertSampleToBlue(int y, int cb, int cr)
    {
        ValidateYCbCrSample(y, cb, cr);
        int centeredCb = cb - 128;
        int value = y + ((116130 * centeredCb + 32768) >> 16);
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

    internal void ValidatePlanarBuffer(JpegPlanarImageBuffer source)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        ValidateDimensions(source.Width, source.Height);

        if (source.ComponentCount < 3)
        {
            throw JpegInvalidDataException.Create("YCbCr planar image must contain three components.");
        }

        if (source.Components == null)
        {
            throw JpegInvalidDataException.Create("Planar component list cannot be null.");
        }

        if (source.Components.Count < 3)
        {
            throw JpegInvalidDataException.Create("YCbCr planar image must contain three component buffers.");
        }

        ValidateComponentForOutput(source.Components[0], source.Width, source.Height, "luminance");
        ValidateComponentForOutput(source.Components[1], source.Width, source.Height, "cb");
        ValidateComponentForOutput(source.Components[2], source.Width, source.Height, "cr");
    }

    internal void ValidateComponentForOutput(JpegPlanarComponentBuffer component, int outputWidth, int outputHeight, string componentName)
    {
        if (component == null)
        {
            throw new System.ArgumentNullException(componentName);
        }

        ValidateDimensions(outputWidth, outputHeight);

        if (component.Width < outputWidth || component.Height < outputHeight)
        {
            throw JpegInvalidDataException.Create("YCbCr component is smaller than the requested output.");
        }

        ValidatePlane(component.Samples, component.Width, component.Height, component.Stride, componentName);
    }

    internal int ValidatePackedSource(byte[] source, int width, int height, int sourceStride)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        ValidateDimensions(width, height);

        int rowByteCount = width * 3;
        if (sourceStride < rowByteCount)
        {
            throw JpegInvalidDataException.Create("YCbCr source stride cannot be smaller than one packed row.");
        }

        int minimumLength = GetStridedLength(sourceStride, height);
        if (source.Length < minimumLength)
        {
            throw JpegInvalidDataException.Create("YCbCr source buffer is too small.");
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

        int rowByteCount = width * 3;
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

    internal void ValidatePlane(byte[] source, int width, int height, int stride, string componentName)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException(componentName);
        }

        ValidateDimensions(width, height);

        if (stride < width)
        {
            throw JpegInvalidDataException.Create("YCbCr component stride cannot be smaller than width.");
        }

        int minimumLength = GetStridedLength(stride, height);
        if (source.Length < minimumLength)
        {
            throw JpegInvalidDataException.Create("YCbCr component plane is too small.");
        }
    }

    internal void ValidateDimensions(int width, int height)
    {
        if (width <= 0)
        {
            throw JpegInvalidDataException.Create("YCbCr conversion width must be greater than zero.");
        }

        if (height < 0)
        {
            throw JpegInvalidDataException.Create("YCbCr conversion height cannot be negative.");
        }
    }

    internal void ValidateYCbCrSample(int y, int cb, int cr)
    {
        ValidateSample(y, "y");
        ValidateSample(cb, "cb");
        ValidateSample(cr, "cr");
    }

    internal void ValidateSample(int sample, string parameterName)
    {
        if (sample < 0 || sample > 255)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
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
