namespace jpeg;

internal class JpegCmykOutputConverter
{
    internal JpegInterleavedPixelBufferAllocator Allocator = new JpegInterleavedPixelBufferAllocator();

    internal JpegInterleavedPixelBuffer ConvertPlanarToCmyk(JpegPlanarImageBuffer source)
    {
        return ConvertPlanarToCmykWithInversion(source, false);
    }

    internal JpegInterleavedPixelBuffer ConvertPlanarToCmykWithAdobeMetadata(JpegPlanarImageBuffer source, JpegAdobeMetadata adobe)
    {
        return ConvertPlanarToCmykWithInversion(source, ShouldInvertAdobeCmyk(adobe));
    }

    internal JpegInterleavedPixelBuffer ConvertPlanarToCmykWithInversion(JpegPlanarImageBuffer source, bool invertAdobeCmyk)
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
            destination.Stride,
            invertAdobeCmyk);
        return destination;
    }

    internal JpegImage ConvertPlanarToImage(JpegPlanarImageBuffer source, JpegAdobeMetadata adobe)
    {
        return ConvertPlanarToImageWithInversion(source, ShouldInvertAdobeCmyk(adobe));
    }

    internal JpegImage ConvertPlanarToImageWithInversion(JpegPlanarImageBuffer source, bool invertAdobeCmyk)
    {
        JpegInterleavedPixelBuffer buffer = ConvertPlanarToCmykWithInversion(source, invertAdobeCmyk);

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

    internal byte[] ConvertComponentsToCmyk(
        JpegPlanarComponentBuffer cyan,
        JpegPlanarComponentBuffer magenta,
        JpegPlanarComponentBuffer yellow,
        JpegPlanarComponentBuffer black,
        int outputWidth,
        int outputHeight,
        bool invertAdobeCmyk)
    {
        ValidateComponentForOutput(cyan, outputWidth, outputHeight, "cyan");
        ValidateComponentForOutput(magenta, outputWidth, outputHeight, "magenta");
        ValidateComponentForOutput(yellow, outputWidth, outputHeight, "yellow");
        ValidateComponentForOutput(black, outputWidth, outputHeight, "black");

        int destinationStride = GetPackedRowByteCount(outputWidth);
        byte[] destination = new byte[GetStridedLength(destinationStride, outputHeight)];
        CopyPlanarToCmyk(cyan, magenta, yellow, black, outputWidth, outputHeight, destination, destinationStride, invertAdobeCmyk);
        return destination;
    }

    internal void CopyPlanarToCmyk(
        JpegPlanarComponentBuffer cyan,
        JpegPlanarComponentBuffer magenta,
        JpegPlanarComponentBuffer yellow,
        JpegPlanarComponentBuffer black,
        int outputWidth,
        int outputHeight,
        byte[] destination,
        int destinationStride,
        bool invertAdobeCmyk)
    {
        ValidateComponentForOutput(cyan, outputWidth, outputHeight, "cyan");
        ValidateComponentForOutput(magenta, outputWidth, outputHeight, "magenta");
        ValidateComponentForOutput(yellow, outputWidth, outputHeight, "yellow");
        ValidateComponentForOutput(black, outputWidth, outputHeight, "black");
        ValidateCmykDestination(destination, outputWidth, outputHeight, destinationStride);

        int y = 0;
        while (y < outputHeight)
        {
            int cyanIndex = y * cyan.Stride;
            int magentaIndex = y * magenta.Stride;
            int yellowIndex = y * yellow.Stride;
            int blackIndex = y * black.Stride;
            int destinationIndex = y * destinationStride;
            int x = 0;
            while (x < outputWidth)
            {
                WriteCmykPixel(
                    cyan.Samples[cyanIndex],
                    magenta.Samples[magentaIndex],
                    yellow.Samples[yellowIndex],
                    black.Samples[blackIndex],
                    destination,
                    destinationIndex,
                    invertAdobeCmyk);
                cyanIndex = cyanIndex + 1;
                magentaIndex = magentaIndex + 1;
                yellowIndex = yellowIndex + 1;
                blackIndex = blackIndex + 1;
                destinationIndex = destinationIndex + 4;
                x = x + 1;
            }

            y = y + 1;
        }
    }

    internal void WriteCmykPixel(
        int cyan,
        int magenta,
        int yellow,
        int black,
        byte[] destination,
        int destinationIndex,
        bool invertAdobeCmyk)
    {
        ValidateCmykSample(cyan, magenta, yellow, black);

        if (destination == null)
        {
            throw new System.ArgumentNullException("destination");
        }

        if (destinationIndex < 0 || destinationIndex + 3 >= destination.Length)
        {
            throw JpegInvalidDataException.Create("CMYK destination index is outside the buffer.");
        }

        destination[destinationIndex] = (byte)ResolveDecodedSample(cyan, invertAdobeCmyk);
        destination[destinationIndex + 1] = (byte)ResolveDecodedSample(magenta, invertAdobeCmyk);
        destination[destinationIndex + 2] = (byte)ResolveDecodedSample(yellow, invertAdobeCmyk);
        destination[destinationIndex + 3] = (byte)ResolveDecodedSample(black, invertAdobeCmyk);
    }

    internal int ResolveDecodedSample(int sample, bool invertAdobeCmyk)
    {
        ValidateSample(sample, "sample");
        if (invertAdobeCmyk)
        {
            return 255 - sample;
        }

        return sample;
    }

    internal bool ShouldInvertAdobeCmyk(JpegAdobeMetadata? adobe)
    {
        if (adobe == null)
        {
            return false;
        }

        return adobe.InvertsCmyk;
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
            throw JpegInvalidDataException.Create("CMYK decode requires four components.");
        }

        if (source.Components == null)
        {
            throw JpegInvalidDataException.Create("Planar component list cannot be null.");
        }

        if (source.Components.Count < 4)
        {
            throw JpegInvalidDataException.Create("CMYK decode requires four component buffers.");
        }

        ValidateComponentForOutput(source.Components[0], source.Width, source.Height, "cyan");
        ValidateComponentForOutput(source.Components[1], source.Width, source.Height, "magenta");
        ValidateComponentForOutput(source.Components[2], source.Width, source.Height, "yellow");
        ValidateComponentForOutput(source.Components[3], source.Width, source.Height, "black");
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
            throw JpegInvalidDataException.Create("CMYK component is smaller than the requested output.");
        }

        ValidatePlane(component.Samples, component.Width, component.Height, component.Stride, componentName);
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
            throw JpegInvalidDataException.Create("CMYK component stride cannot be smaller than width.");
        }

        int minimumLength = GetStridedLength(stride, height);
        if (source.Length < minimumLength)
        {
            throw JpegInvalidDataException.Create("CMYK component plane is too small.");
        }
    }

    internal void ValidateDimensions(int width, int height)
    {
        if (width <= 0)
        {
            throw JpegInvalidDataException.Create("CMYK output width must be greater than zero.");
        }

        if (height < 0)
        {
            throw JpegInvalidDataException.Create("CMYK output height cannot be negative.");
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
