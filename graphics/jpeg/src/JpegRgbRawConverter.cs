namespace jpeg;

internal class JpegRgbRawConverter
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
        image.Subsampling = JpegSubsampling.Ratio444;
        image.Pixels = buffer.Pixels;
        return image;
    }

    internal byte[] ConvertComponentsToRgb(
        JpegPlanarComponentBuffer red,
        JpegPlanarComponentBuffer green,
        JpegPlanarComponentBuffer blue,
        int outputWidth,
        int outputHeight)
    {
        ValidateComponentForOutput(red, outputWidth, outputHeight, "red");
        ValidateComponentForOutput(green, outputWidth, outputHeight, "green");
        ValidateComponentForOutput(blue, outputWidth, outputHeight, "blue");

        int destinationStride = GetPackedRowByteCount(outputWidth);
        byte[] destination = new byte[GetStridedLength(destinationStride, outputHeight)];
        CopyPlanarToRgb(red, green, blue, outputWidth, outputHeight, destination, destinationStride);
        return destination;
    }

    internal void CopyPlanarToRgb(
        JpegPlanarComponentBuffer red,
        JpegPlanarComponentBuffer green,
        JpegPlanarComponentBuffer blue,
        int outputWidth,
        int outputHeight,
        byte[] destination,
        int destinationStride)
    {
        ValidateComponentForOutput(red, outputWidth, outputHeight, "red");
        ValidateComponentForOutput(green, outputWidth, outputHeight, "green");
        ValidateComponentForOutput(blue, outputWidth, outputHeight, "blue");
        ValidateRgbDestination(destination, outputWidth, outputHeight, destinationStride);

        int y = 0;
        while (y < outputHeight)
        {
            int redIndex = y * red.Stride;
            int greenIndex = y * green.Stride;
            int blueIndex = y * blue.Stride;
            int destinationIndex = y * destinationStride;
            int x = 0;
            while (x < outputWidth)
            {
                WriteRgbPixel(red.Samples[redIndex], green.Samples[greenIndex], blue.Samples[blueIndex], destination, destinationIndex);
                redIndex = redIndex + 1;
                greenIndex = greenIndex + 1;
                blueIndex = blueIndex + 1;
                destinationIndex = destinationIndex + 3;
                x = x + 1;
            }

            y = y + 1;
        }
    }

    internal JpegPlanarImageBuffer ConvertRgbImageToPlanar(JpegImage source)
    {
        ValidateRgbImage(source);

        JpegPlanarImageBuffer destination = new JpegPlanarImageBuffer();
        destination.Width = source.Width;
        destination.Height = source.Height;
        destination.Precision = 8;
        destination.ComponentCount = 3;
        destination.ColorSpace = JpegColorSpace.RGB;
        destination.Subsampling = JpegSubsampling.Ratio444;
        destination.Ownership = JpegBufferOwnership.LibraryOwned;
        destination.Components.Add(CreateComponentBuffer(0, 1, source.Width, source.Height));
        destination.Components.Add(CreateComponentBuffer(1, 2, source.Width, source.Height));
        destination.Components.Add(CreateComponentBuffer(2, 3, source.Width, source.Height));
        SplitPackedRgbToPlanar(
            source.Pixels,
            source.Width,
            source.Height,
            GetPackedRowByteCount(source.Width),
            destination.Components[0],
            destination.Components[1],
            destination.Components[2]);
        return destination;
    }

    internal void SplitPackedRgbToPlanar(
        byte[] source,
        int width,
        int height,
        int sourceStride,
        JpegPlanarComponentBuffer red,
        JpegPlanarComponentBuffer green,
        JpegPlanarComponentBuffer blue)
    {
        int rowByteCount = ValidateRgbSource(source, width, height, sourceStride);
        ValidateComponentForOutput(red, width, height, "red");
        ValidateComponentForOutput(green, width, height, "green");
        ValidateComponentForOutput(blue, width, height, "blue");

        int y = 0;
        while (y < height)
        {
            int sourceIndex = y * sourceStride;
            int rowEnd = sourceIndex + rowByteCount;
            int redIndex = y * red.Stride;
            int greenIndex = y * green.Stride;
            int blueIndex = y * blue.Stride;
            while (sourceIndex < rowEnd)
            {
                red.Samples[redIndex] = source[sourceIndex];
                green.Samples[greenIndex] = source[sourceIndex + 1];
                blue.Samples[blueIndex] = source[sourceIndex + 2];
                sourceIndex = sourceIndex + 3;
                redIndex = redIndex + 1;
                greenIndex = greenIndex + 1;
                blueIndex = blueIndex + 1;
            }

            y = y + 1;
        }
    }

    internal JpegPlanarComponentBuffer CreateComponentBuffer(int componentIndex, int identifier, int width, int height)
    {
        ValidateDimensions(width, height);

        JpegPlanarComponentBuffer component = new JpegPlanarComponentBuffer();
        component.ComponentIndex = componentIndex;
        component.Identifier = identifier;
        component.Precision = 8;
        component.HorizontalSamplingFactor = 1;
        component.VerticalSamplingFactor = 1;
        component.Width = width;
        component.Height = height;
        component.PaddedWidth = width;
        component.PaddedHeight = height;
        component.Stride = width;
        component.SampleStorage = JpegSampleStorage.Byte;
        component.Ownership = JpegBufferOwnership.LibraryOwned;
        component.Samples = new byte[GetStridedLength(width, height)];
        return component;
    }

    internal void WriteRgbPixel(int red, int green, int blue, byte[] destination, int destinationIndex)
    {
        ValidateRgbSample(red, green, blue);

        if (destination == null)
        {
            throw new System.ArgumentNullException("destination");
        }

        if (destinationIndex < 0 || destinationIndex + 2 >= destination.Length)
        {
            throw JpegInvalidDataException.Create("RGB destination index is outside the buffer.");
        }

        destination[destinationIndex] = (byte)red;
        destination[destinationIndex + 1] = (byte)green;
        destination[destinationIndex + 2] = (byte)blue;
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
            throw JpegInvalidDataException.Create("RGB raw image requires three components.");
        }

        if (source.Components == null)
        {
            throw JpegInvalidDataException.Create("Planar component list cannot be null.");
        }

        if (source.Components.Count < 3)
        {
            throw JpegInvalidDataException.Create("RGB raw image requires three component buffers.");
        }

        ValidateComponentForOutput(source.Components[0], source.Width, source.Height, "red");
        ValidateComponentForOutput(source.Components[1], source.Width, source.Height, "green");
        ValidateComponentForOutput(source.Components[2], source.Width, source.Height, "blue");
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
            throw JpegInvalidDataException.Create("RGB raw encode requires Rgb24 input.");
        }

        if (source.ComponentCount != 3)
        {
            throw JpegInvalidDataException.Create("RGB image must contain three components.");
        }

        ValidateRgbSource(source.Pixels, source.Width, source.Height, GetPackedRowByteCount(source.Width));
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
            throw JpegInvalidDataException.Create("RGB component is smaller than the requested output.");
        }

        ValidatePlane(component.Samples, component.Width, component.Height, component.Stride, componentName);
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

    internal void ValidateRgbDestination(byte[] destination, int width, int height, int destinationStride)
    {
        if (destination == null)
        {
            throw new System.ArgumentNullException("destination");
        }

        ValidateDimensions(width, height);

        int rowByteCount = GetPackedRowByteCount(width);
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
            throw JpegInvalidDataException.Create("RGB component stride cannot be smaller than width.");
        }

        int minimumLength = GetStridedLength(stride, height);
        if (source.Length < minimumLength)
        {
            throw JpegInvalidDataException.Create("RGB component plane is too small.");
        }
    }

    internal void ValidateDimensions(int width, int height)
    {
        if (width <= 0)
        {
            throw JpegInvalidDataException.Create("RGB raw mode width must be greater than zero.");
        }

        if (height < 0)
        {
            throw JpegInvalidDataException.Create("RGB raw mode height cannot be negative.");
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
