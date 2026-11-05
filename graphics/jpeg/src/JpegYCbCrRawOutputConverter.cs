namespace jpeg;

internal class JpegYCbCrRawOutputConverter
{
    internal JpegInterleavedPixelBufferAllocator Allocator = new JpegInterleavedPixelBufferAllocator();

    internal JpegInterleavedPixelBuffer ConvertPlanarToYCbCr(JpegPlanarImageBuffer source)
    {
        ValidatePlanarBuffer(source);

        JpegInterleavedPixelBuffer destination = Allocator.Allocate(source.Width, source.Height, JpegPixelFormat.YCbCr24, 0);
        CopyPlanarToYCbCr(
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
        JpegInterleavedPixelBuffer buffer = ConvertPlanarToYCbCr(source);

        JpegImage image = new JpegImage();
        image.Width = buffer.Width;
        image.Height = buffer.Height;
        image.Precision = 8;
        image.ComponentCount = 3;
        image.PixelFormat = JpegPixelFormat.YCbCr24;
        image.ColorSpace = JpegColorSpace.YCbCr;
        image.Subsampling = JpegSubsampling.Ratio444;
        image.Pixels = buffer.Pixels;
        return image;
    }

    internal byte[] ConvertComponentsToYCbCr(
        JpegPlanarComponentBuffer luminance,
        JpegPlanarComponentBuffer cb,
        JpegPlanarComponentBuffer cr,
        int outputWidth,
        int outputHeight)
    {
        ValidateComponentForOutput(luminance, outputWidth, outputHeight, "luminance");
        ValidateComponentForOutput(cb, outputWidth, outputHeight, "cb");
        ValidateComponentForOutput(cr, outputWidth, outputHeight, "cr");

        int destinationStride = GetPackedRowByteCount(outputWidth);
        byte[] destination = new byte[GetStridedLength(destinationStride, outputHeight)];
        CopyPlanarToYCbCr(luminance, cb, cr, outputWidth, outputHeight, destination, destinationStride);
        return destination;
    }

    internal void CopyPlanarToYCbCr(
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
        ValidateYCbCrDestination(destination, outputWidth, outputHeight, destinationStride);

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
                WriteYCbCrPixel(luminance.Samples[luminanceIndex], cb.Samples[cbIndex], cr.Samples[crIndex], destination, destinationIndex);
                luminanceIndex = luminanceIndex + 1;
                cbIndex = cbIndex + 1;
                crIndex = crIndex + 1;
                destinationIndex = destinationIndex + 3;
                x = x + 1;
            }

            y = y + 1;
        }
    }

    internal void WriteYCbCrPixel(int luminance, int cb, int cr, byte[] destination, int destinationIndex)
    {
        ValidateYCbCrSample(luminance, cb, cr);

        if (destination == null)
        {
            throw new System.ArgumentNullException("destination");
        }

        if (destinationIndex < 0 || destinationIndex + 2 >= destination.Length)
        {
            throw JpegInvalidDataException.Create("YCbCr destination index is outside the buffer.");
        }

        destination[destinationIndex] = (byte)luminance;
        destination[destinationIndex + 1] = (byte)cb;
        destination[destinationIndex + 2] = (byte)cr;
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
            throw JpegInvalidDataException.Create("YCbCr raw output requires three components.");
        }

        if (source.Components == null)
        {
            throw JpegInvalidDataException.Create("Planar component list cannot be null.");
        }

        if (source.Components.Count < 3)
        {
            throw JpegInvalidDataException.Create("YCbCr raw output requires three component buffers.");
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
            throw JpegInvalidDataException.Create("YCbCr raw output width must be greater than zero.");
        }

        if (height < 0)
        {
            throw JpegInvalidDataException.Create("YCbCr raw output height cannot be negative.");
        }
    }

    internal void ValidateYCbCrSample(int luminance, int cb, int cr)
    {
        ValidateSample(luminance, "luminance");
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
