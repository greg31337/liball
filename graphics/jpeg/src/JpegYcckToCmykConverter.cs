namespace jpeg;

internal class JpegYcckToCmykConverter
{
    internal JpegInterleavedPixelBufferAllocator Allocator = new JpegInterleavedPixelBufferAllocator();
    internal JpegYCbCrToRgbConverter YCbCrConverter = new JpegYCbCrToRgbConverter();

    internal JpegInterleavedPixelBuffer ConvertPlanarToCmyk(JpegPlanarImageBuffer source)
    {
        ValidatePlanarBuffer(source);

        JpegInterleavedPixelBuffer destination = Allocator.Allocate(source.Width, source.Height, JpegPixelFormat.Cmyk32, 0);
        CopyPlanarToCmyk(
            source.Components[0],
            source.Components[1],
            source.Components[2],
            source.Components[3],
            source.Width,
            source.Height,
            destination.Pixels,
            destination.Stride);
        return destination;
    }

    internal JpegImage ConvertPlanarToImage(JpegPlanarImageBuffer source)
    {
        JpegInterleavedPixelBuffer buffer = ConvertPlanarToCmyk(source);

        JpegImage image = new JpegImage();
        image.Width = buffer.Width;
        image.Height = buffer.Height;
        image.Precision = 8;
        image.ComponentCount = 4;
        image.PixelFormat = JpegPixelFormat.Cmyk32;
        image.ColorSpace = JpegColorSpace.CMYK;
        image.Subsampling = JpegSubsampling.Ratio444;
        image.Pixels = buffer.Pixels;
        return image;
    }

    internal JpegInterleavedPixelBuffer ConvertImageToCmykBuffer(JpegImage source)
    {
        ValidateYcckImage(source);

        JpegInterleavedPixelBuffer destination = Allocator.Allocate(source.Width, source.Height, JpegPixelFormat.Cmyk32, 0);
        CopyPackedYcckToCmyk(
            source.Pixels,
            source.Width,
            source.Height,
            GetPackedRowByteCount(source.Width),
            destination.Pixels,
            destination.Stride);
        return destination;
    }

    internal JpegImage ConvertImageToCmykImage(JpegImage source)
    {
        JpegInterleavedPixelBuffer buffer = ConvertImageToCmykBuffer(source);

        int precision = source.Precision;
        if (precision <= 0)
        {
            precision = 8;
        }

        JpegImage image = new JpegImage();
        image.Width = buffer.Width;
        image.Height = buffer.Height;
        image.Precision = precision;
        image.ComponentCount = 4;
        image.PixelFormat = JpegPixelFormat.Cmyk32;
        image.ColorSpace = JpegColorSpace.CMYK;
        image.Subsampling = JpegSubsampling.Ratio444;
        image.Pixels = buffer.Pixels;
        return image;
    }

    internal byte[] ConvertPackedYcckToCmyk(byte[] source, int width, int height)
    {
        ValidateDimensions(width, height);

        int stride = GetPackedRowByteCount(width);
        byte[] destination = new byte[GetStridedLength(stride, height)];
        CopyPackedYcckToCmyk(source, width, height, stride, destination, stride);
        return destination;
    }

    internal void CopyPackedYcckToCmyk(
        byte[] source,
        int width,
        int height,
        int sourceStride,
        byte[] destination,
        int destinationStride)
    {
        int rowByteCount = ValidateYcckSource(source, width, height, sourceStride);
        ValidateCmykDestination(destination, width, height, destinationStride);

        int y = 0;
        while (y < height)
        {
            int sourceIndex = y * sourceStride;
            int destinationIndex = y * destinationStride;
            int rowEnd = sourceIndex + rowByteCount;
            while (sourceIndex < rowEnd)
            {
                WriteCmykPixel(
                    source[sourceIndex],
                    source[sourceIndex + 1],
                    source[sourceIndex + 2],
                    source[sourceIndex + 3],
                    destination,
                    destinationIndex);
                sourceIndex = sourceIndex + 4;
                destinationIndex = destinationIndex + 4;
            }

            y = y + 1;
        }
    }

    internal void CopyPlanarToCmyk(
        JpegPlanarComponentBuffer luminance,
        JpegPlanarComponentBuffer cb,
        JpegPlanarComponentBuffer cr,
        JpegPlanarComponentBuffer black,
        int outputWidth,
        int outputHeight,
        byte[] destination,
        int destinationStride)
    {
        ValidateComponentForOutput(luminance, outputWidth, outputHeight, "luminance");
        ValidateComponentForOutput(cb, outputWidth, outputHeight, "cb");
        ValidateComponentForOutput(cr, outputWidth, outputHeight, "cr");
        ValidateComponentForOutput(black, outputWidth, outputHeight, "black");
        ValidateCmykDestination(destination, outputWidth, outputHeight, destinationStride);

        int y = 0;
        while (y < outputHeight)
        {
            int luminanceIndex = y * luminance.Stride;
            int cbIndex = y * cb.Stride;
            int crIndex = y * cr.Stride;
            int blackIndex = y * black.Stride;
            int destinationIndex = y * destinationStride;
            int x = 0;
            while (x < outputWidth)
            {
                WriteCmykPixel(
                    luminance.Samples[luminanceIndex],
                    cb.Samples[cbIndex],
                    cr.Samples[crIndex],
                    black.Samples[blackIndex],
                    destination,
                    destinationIndex);
                luminanceIndex = luminanceIndex + 1;
                cbIndex = cbIndex + 1;
                crIndex = crIndex + 1;
                blackIndex = blackIndex + 1;
                destinationIndex = destinationIndex + 4;
                x = x + 1;
            }

            y = y + 1;
        }
    }

    internal void WriteCmykPixel(int luminance, int cb, int cr, int black, byte[] destination, int destinationIndex)
    {
        ValidateYcckSample(luminance, cb, cr, black);

        if (destination == null)
        {
            throw new System.ArgumentNullException("destination");
        }

        if (destinationIndex < 0 || destinationIndex + 3 >= destination.Length)
        {
            throw JpegInvalidDataException.Create("CMYK destination index is outside the buffer.");
        }

        destination[destinationIndex] = (byte)ConvertSampleToCyan(luminance, cb, cr, black);
        destination[destinationIndex + 1] = (byte)ConvertSampleToMagenta(luminance, cb, cr, black);
        destination[destinationIndex + 2] = (byte)ConvertSampleToYellow(luminance, cb, cr, black);
        destination[destinationIndex + 3] = (byte)ConvertSampleToBlack(luminance, cb, cr, black);
    }

    internal int ConvertSampleToCyan(int luminance, int cb, int cr, int black)
    {
        ValidateYcckSample(luminance, cb, cr, black);
        return 255 - YCbCrConverter.ConvertSampleToRed(luminance, cb, cr);
    }

    internal int ConvertSampleToMagenta(int luminance, int cb, int cr, int black)
    {
        ValidateYcckSample(luminance, cb, cr, black);
        return 255 - YCbCrConverter.ConvertSampleToGreen(luminance, cb, cr);
    }

    internal int ConvertSampleToYellow(int luminance, int cb, int cr, int black)
    {
        ValidateYcckSample(luminance, cb, cr, black);
        return 255 - YCbCrConverter.ConvertSampleToBlue(luminance, cb, cr);
    }

    internal int ConvertSampleToBlack(int luminance, int cb, int cr, int black)
    {
        ValidateYcckSample(luminance, cb, cr, black);
        return black;
    }

    internal void ValidatePlanarBuffer(JpegPlanarImageBuffer source)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        ValidateDimensions(source.Width, source.Height);

        if (source.ComponentCount < 4)
        {
            throw JpegInvalidDataException.Create("YCCK conversion requires four components.");
        }

        if (source.Components == null)
        {
            throw JpegInvalidDataException.Create("Planar component list cannot be null.");
        }

        if (source.Components.Count < 4)
        {
            throw JpegInvalidDataException.Create("YCCK conversion requires four component buffers.");
        }

        ValidateComponentForOutput(source.Components[0], source.Width, source.Height, "luminance");
        ValidateComponentForOutput(source.Components[1], source.Width, source.Height, "cb");
        ValidateComponentForOutput(source.Components[2], source.Width, source.Height, "cr");
        ValidateComponentForOutput(source.Components[3], source.Width, source.Height, "black");
    }

    internal void ValidateYcckImage(JpegImage source)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        ValidateDimensions(source.Width, source.Height);

        if (source.PixelFormat != JpegPixelFormat.Ycck32)
        {
            throw JpegInvalidDataException.Create("YCCK to CMYK conversion requires Ycck32 input.");
        }

        if (source.ComponentCount != 4)
        {
            throw JpegInvalidDataException.Create("YCCK image must contain four components.");
        }

        ValidateYcckSource(source.Pixels, source.Width, source.Height, GetPackedRowByteCount(source.Width));
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
            throw JpegInvalidDataException.Create("YCCK component is smaller than the requested output.");
        }

        ValidatePlane(component.Samples, component.Width, component.Height, component.Stride, componentName);
    }

    internal int ValidateYcckSource(byte[] source, int width, int height, int sourceStride)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        ValidateDimensions(width, height);

        int rowByteCount = GetPackedRowByteCount(width);
        if (sourceStride < rowByteCount)
        {
            throw JpegInvalidDataException.Create("YCCK source stride cannot be smaller than one packed row.");
        }

        int minimumLength = GetStridedLength(sourceStride, height);
        if (source.Length < minimumLength)
        {
            throw JpegInvalidDataException.Create("YCCK source buffer is too small.");
        }

        return rowByteCount;
    }

    internal void ValidateCmykDestination(byte[] destination, int width, int height, int destinationStride)
    {
        if (destination == null)
        {
            throw new System.ArgumentNullException("destination");
        }

        ValidateDimensions(width, height);

        int rowByteCount = GetPackedRowByteCount(width);
        if (destinationStride < rowByteCount)
        {
            throw JpegInvalidDataException.Create("CMYK destination stride cannot be smaller than one packed row.");
        }

        int minimumLength = GetStridedLength(destinationStride, height);
        if (destination.Length < minimumLength)
        {
            throw JpegInvalidDataException.Create("CMYK destination buffer is too small.");
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
            throw JpegInvalidDataException.Create("YCCK component stride cannot be smaller than width.");
        }

        int minimumLength = GetStridedLength(stride, height);
        if (source.Length < minimumLength)
        {
            throw JpegInvalidDataException.Create("YCCK component plane is too small.");
        }
    }

    internal void ValidateDimensions(int width, int height)
    {
        if (width <= 0)
        {
            throw JpegInvalidDataException.Create("YCCK conversion width must be greater than zero.");
        }

        if (height < 0)
        {
            throw JpegInvalidDataException.Create("YCCK conversion height cannot be negative.");
        }
    }

    internal void ValidateYcckSample(int luminance, int cb, int cr, int black)
    {
        ValidateSample(luminance, "luminance");
        ValidateSample(cb, "cb");
        ValidateSample(cr, "cr");
        ValidateSample(black, "black");
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

        long rowByteCount = (long)width * 4L;
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
