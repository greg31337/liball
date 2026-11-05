namespace jpeg;

internal class JpegFastBoxChromaUpsampler : IJpegChromaUpsampler
{
    /// <summary>Upsamples a component with nearest-neighbor box sampling.</summary>
    /// <param name="source">The source component.</param>
    /// <param name="outputWidth">The output width.</param>
    /// <param name="outputHeight">The output height.</param>
    /// <returns>The upsampled component.</returns>
    public JpegPlanarComponentBuffer UpsampleComponent(JpegPlanarComponentBuffer source, int outputWidth, int outputHeight)
    {
        ValidateComponentForOutput(source, outputWidth, outputHeight);

        JpegPlanarComponentBuffer destination = new JpegPlanarComponentBuffer();
        destination.ComponentIndex = source.ComponentIndex;
        destination.Identifier = source.Identifier;
        destination.Precision = source.Precision;
        destination.HorizontalSamplingFactor = source.HorizontalSamplingFactor;
        destination.VerticalSamplingFactor = source.VerticalSamplingFactor;
        destination.Width = outputWidth;
        destination.Height = outputHeight;
        destination.PaddedWidth = outputWidth;
        destination.PaddedHeight = outputHeight;
        destination.Stride = outputWidth;
        destination.SampleStorage = JpegSampleStorage.Byte;
        destination.Ownership = JpegBufferOwnership.LibraryOwned;
        destination.Samples = Upsample(source.Samples, source.Width, source.Height, source.Stride, outputWidth, outputHeight);
        return destination;
    }

    internal byte[] Upsample(byte[] source, int sourceWidth, int sourceHeight, int sourceStride, int outputWidth, int outputHeight)
    {
        ValidateSource(source, sourceWidth, sourceHeight, sourceStride);
        ValidateOutputDimensions(sourceWidth, sourceHeight, outputWidth, outputHeight);

        byte[] destination = new byte[GetStridedLength(outputWidth, outputHeight)];
        UpsampleTo(source, sourceWidth, sourceHeight, sourceStride, outputWidth, outputHeight, destination, outputWidth);
        return destination;
    }

    internal void UpsampleTo(
        byte[] source,
        int sourceWidth,
        int sourceHeight,
        int sourceStride,
        int outputWidth,
        int outputHeight,
        byte[] destination,
        int destinationStride)
    {
        ValidateSource(source, sourceWidth, sourceHeight, sourceStride);
        ValidateOutputDimensions(sourceWidth, sourceHeight, outputWidth, outputHeight);
        ValidateDestination(destination, outputWidth, outputHeight, destinationStride);

        int y = 0;
        while (y < outputHeight)
        {
            int sourceY = MapOutputToSourceIndex(y, sourceHeight, outputHeight);
            int destinationIndex = y * destinationStride;
            int x = 0;
            while (x < outputWidth)
            {
                int sourceX = MapOutputToSourceIndex(x, sourceWidth, outputWidth);
                destination[destinationIndex] = source[sourceY * sourceStride + sourceX];
                destinationIndex = destinationIndex + 1;
                x = x + 1;
            }

            y = y + 1;
        }
    }

    internal int ReadBoxSample(
        byte[] source,
        int sourceWidth,
        int sourceHeight,
        int sourceStride,
        int outputX,
        int outputY,
        int outputWidth,
        int outputHeight)
    {
        ValidateSource(source, sourceWidth, sourceHeight, sourceStride);
        ValidateOutputDimensions(sourceWidth, sourceHeight, outputWidth, outputHeight);

        if (outputX < 0 || outputX >= outputWidth)
        {
            throw new System.ArgumentOutOfRangeException("outputX");
        }

        if (outputY < 0 || outputY >= outputHeight)
        {
            throw new System.ArgumentOutOfRangeException("outputY");
        }

        int sourceX = MapOutputToSourceIndex(outputX, sourceWidth, outputWidth);
        int sourceY = MapOutputToSourceIndex(outputY, sourceHeight, outputHeight);
        return source[sourceY * sourceStride + sourceX];
    }

    internal int MapOutputToSourceIndex(int outputPosition, int sourceLength, int outputLength)
    {
        if (outputPosition < 0 || outputPosition >= outputLength)
        {
            throw new System.ArgumentOutOfRangeException("outputPosition");
        }

        if (sourceLength <= 0)
        {
            throw new System.ArgumentOutOfRangeException("sourceLength");
        }

        if (outputLength <= 0)
        {
            throw new System.ArgumentOutOfRangeException("outputLength");
        }

        long index = (long)outputPosition * (long)sourceLength / (long)outputLength;
        if (index >= sourceLength)
        {
            return sourceLength - 1;
        }

        return (int)index;
    }

    internal void ValidateComponentForOutput(JpegPlanarComponentBuffer source, int outputWidth, int outputHeight)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        ValidateSource(source.Samples, source.Width, source.Height, source.Stride);
        ValidateOutputDimensions(source.Width, source.Height, outputWidth, outputHeight);
    }

    internal void ValidateSource(byte[] source, int sourceWidth, int sourceHeight, int sourceStride)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        if (sourceWidth <= 0)
        {
            throw JpegInvalidDataException.Create("Chroma source width must be greater than zero.");
        }

        if (sourceHeight < 0)
        {
            throw JpegInvalidDataException.Create("Chroma source height cannot be negative.");
        }

        if (sourceStride < sourceWidth)
        {
            throw JpegInvalidDataException.Create("Chroma source stride cannot be smaller than width.");
        }

        int minimumLength = GetStridedLength(sourceStride, sourceHeight);
        if (source.Length < minimumLength)
        {
            throw JpegInvalidDataException.Create("Chroma source plane is too small.");
        }
    }

    internal void ValidateOutputDimensions(int sourceWidth, int sourceHeight, int outputWidth, int outputHeight)
    {
        if (outputWidth <= 0)
        {
            throw JpegInvalidDataException.Create("Chroma output width must be greater than zero.");
        }

        if (outputHeight < 0)
        {
            throw JpegInvalidDataException.Create("Chroma output height cannot be negative.");
        }

        if (outputWidth < sourceWidth)
        {
            throw JpegInvalidDataException.Create("Fast box chroma upsampling cannot reduce width.");
        }

        if (outputHeight < sourceHeight)
        {
            throw JpegInvalidDataException.Create("Fast box chroma upsampling cannot reduce height.");
        }

        if (outputHeight > 0 && sourceHeight == 0)
        {
            throw JpegInvalidDataException.Create("Cannot upsample non-empty output from an empty source.");
        }
    }

    internal void ValidateDestination(byte[] destination, int outputWidth, int outputHeight, int destinationStride)
    {
        if (destination == null)
        {
            throw new System.ArgumentNullException("destination");
        }

        if (destinationStride < outputWidth)
        {
            throw JpegInvalidDataException.Create("Chroma destination stride cannot be smaller than width.");
        }

        int minimumLength = GetStridedLength(destinationStride, outputHeight);
        if (destination.Length < minimumLength)
        {
            throw JpegInvalidDataException.Create("Chroma destination plane is too small.");
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
            throw JpegInvalidDataException.Create("Chroma plane is too large.");
        }

        return (int)length;
    }
}
