namespace jpeg;

internal class JpegGrayscaleOutputConverter
{
    internal JpegInterleavedPixelBufferAllocator Allocator = new JpegInterleavedPixelBufferAllocator();

    internal JpegInterleavedPixelBuffer ConvertPlanarToL8(JpegPlanarImageBuffer source)
    {
        ValidatePlanarBuffer(source);

        JpegInterleavedPixelBuffer destination = Allocator.Allocate(source.Width, source.Height, JpegPixelFormat.L8, 0);
        CopyComponentToL8(source.Components[0], source.Width, source.Height, destination.Pixels, destination.Stride);
        return destination;
    }

    internal JpegImage ConvertPlanarToImage(JpegPlanarImageBuffer source)
    {
        JpegInterleavedPixelBuffer buffer = ConvertPlanarToL8(source);

        JpegImage image = new JpegImage();
        image.Width = buffer.Width;
        image.Height = buffer.Height;
        image.Precision = 8;
        image.ComponentCount = 1;
        image.PixelFormat = JpegPixelFormat.L8;
        image.ColorSpace = JpegColorSpace.Grayscale;
        image.Subsampling = JpegSubsampling.Grayscale;
        image.Pixels = buffer.Pixels;
        return image;
    }

    internal byte[] ConvertComponentToL8(JpegPlanarComponentBuffer component, int outputWidth, int outputHeight)
    {
        ValidateComponentForOutput(component, outputWidth, outputHeight);

        int length = Allocator.GetBufferLength(outputWidth, outputHeight, 1);
        byte[] destination = new byte[length];
        CopyComponentToL8(component, outputWidth, outputHeight, destination, outputWidth);
        return destination;
    }

    internal void CopyComponentToL8(
        JpegPlanarComponentBuffer component,
        int outputWidth,
        int outputHeight,
        byte[] destination,
        int destinationStride)
    {
        ValidateComponentForOutput(component, outputWidth, outputHeight);
        CopyLuminanceToL8(component.Samples, outputWidth, outputHeight, component.Stride, destination, destinationStride);
    }

    internal void CopyLuminanceToL8(
        byte[] source,
        int width,
        int height,
        int sourceStride,
        byte[] destination,
        int destinationStride)
    {
        ValidateLuminancePlane(source, width, height, sourceStride);
        ValidateDestination(destination, width, height, destinationStride);

        int y = 0;
        while (y < height)
        {
            int sourceIndex = y * sourceStride;
            int destinationIndex = y * destinationStride;
            System.Array.Copy(source, sourceIndex, destination, destinationIndex, width);
            y = y + 1;
        }
    }

    internal void ValidatePlanarBuffer(JpegPlanarImageBuffer source)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        if (source.Width <= 0)
        {
            throw JpegInvalidDataException.Create("Planar image width must be greater than zero.");
        }

        if (source.Height < 0)
        {
            throw JpegInvalidDataException.Create("Planar image height cannot be negative.");
        }

        if (source.ComponentCount <= 0)
        {
            throw JpegInvalidDataException.Create("Planar image must contain at least one component.");
        }

        if (source.Components == null)
        {
            throw JpegInvalidDataException.Create("Planar component list cannot be null.");
        }

        if (source.Components.Count < source.ComponentCount)
        {
            throw JpegInvalidDataException.Create("Planar component count must fit in the component list.");
        }

        ValidateComponentForOutput(source.Components[0], source.Width, source.Height);
    }

    internal void ValidateComponentForOutput(JpegPlanarComponentBuffer component, int outputWidth, int outputHeight)
    {
        if (component == null)
        {
            throw new System.ArgumentNullException("component");
        }

        if (outputWidth <= 0)
        {
            throw JpegInvalidDataException.Create("Output width must be greater than zero.");
        }

        if (outputHeight < 0)
        {
            throw JpegInvalidDataException.Create("Output height cannot be negative.");
        }

        if (component.Width < outputWidth || component.Height < outputHeight)
        {
            throw JpegInvalidDataException.Create("Luminance component is smaller than the requested output.");
        }

        ValidateLuminancePlane(component.Samples, component.Width, component.Height, component.Stride);
    }

    internal void ValidateLuminancePlane(byte[] source, int width, int height, int sourceStride)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        if (width <= 0)
        {
            throw JpegInvalidDataException.Create("Luminance width must be greater than zero.");
        }

        if (height < 0)
        {
            throw JpegInvalidDataException.Create("Luminance height cannot be negative.");
        }

        if (sourceStride < width)
        {
            throw JpegInvalidDataException.Create("Luminance stride cannot be smaller than width.");
        }

        int minimumLength = GetStridedLength(sourceStride, height);
        if (source.Length < minimumLength)
        {
            throw JpegInvalidDataException.Create("Luminance source plane is too small.");
        }
    }

    internal void ValidateDestination(byte[] destination, int width, int height, int destinationStride)
    {
        if (destination == null)
        {
            throw new System.ArgumentNullException("destination");
        }

        if (width <= 0)
        {
            throw JpegInvalidDataException.Create("Destination width must be greater than zero.");
        }

        if (height < 0)
        {
            throw JpegInvalidDataException.Create("Destination height cannot be negative.");
        }

        if (destinationStride < width)
        {
            throw JpegInvalidDataException.Create("Destination stride cannot be smaller than width.");
        }

        int minimumLength = GetStridedLength(destinationStride, height);
        if (destination.Length < minimumLength)
        {
            throw JpegInvalidDataException.Create("Destination L8 buffer is too small.");
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
