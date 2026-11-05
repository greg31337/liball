namespace jpeg;

internal class JpegFancyChromaUpsampler : IJpegChromaUpsampler
{
    /// <summary>Upsamples a component with interpolated sampling.</summary>
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
            int sourceYFixed = MapOutputToSourceFixed(y, sourceHeight, outputHeight);
            int destinationIndex = y * destinationStride;
            int x = 0;
            while (x < outputWidth)
            {
                int sourceXFixed = MapOutputToSourceFixed(x, sourceWidth, outputWidth);
                destination[destinationIndex] = (byte)ReadInterpolatedSample(source, sourceWidth, sourceHeight, sourceStride, sourceXFixed, sourceYFixed);
                destinationIndex = destinationIndex + 1;
                x = x + 1;
            }

            y = y + 1;
        }
    }

    internal int InterpolateSample(
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

        int sourceXFixed = MapOutputToSourceFixed(outputX, sourceWidth, outputWidth);
        int sourceYFixed = MapOutputToSourceFixed(outputY, sourceHeight, outputHeight);
        return ReadInterpolatedSample(source, sourceWidth, sourceHeight, sourceStride, sourceXFixed, sourceYFixed);
    }

    internal int ReadInterpolatedSample(
        byte[] source,
        int sourceWidth,
        int sourceHeight,
        int sourceStride,
        int sourceXFixed,
        int sourceYFixed)
    {
        int x0 = sourceXFixed >> 8;
        int y0 = sourceYFixed >> 8;
        int x1 = x0 + 1;
        int y1 = y0 + 1;
        if (x1 >= sourceWidth)
        {
            x1 = sourceWidth - 1;
        }

        if (y1 >= sourceHeight)
        {
            y1 = sourceHeight - 1;
        }

        int xWeight = sourceXFixed - x0 * 256;
        int yWeight = sourceYFixed - y0 * 256;

        int top = Blend(source[y0 * sourceStride + x0], source[y0 * sourceStride + x1], xWeight);
        int bottom = Blend(source[y1 * sourceStride + x0], source[y1 * sourceStride + x1], xWeight);
        return Blend(top, bottom, yWeight);
    }

    internal int MapOutputToSourceFixed(int outputPosition, int sourceLength, int outputLength)
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

        long fixedPosition = ((long)outputPosition * 2L + 1L) * (long)sourceLength * 256L / ((long)outputLength * 2L) - 128L;
        long maxFixedPosition = (long)(sourceLength - 1) * 256L;
        if (fixedPosition < 0)
        {
            return 0;
        }

        if (fixedPosition > maxFixedPosition)
        {
            return (int)maxFixedPosition;
        }

        return (int)fixedPosition;
    }

    internal int Blend(int first, int second, int weight)
    {
        ValidateSample(first, "first");
        ValidateSample(second, "second");

        if (weight < 0 || weight > 256)
        {
            throw new System.ArgumentOutOfRangeException("weight");
        }

        return ((256 - weight) * first + weight * second + 128) >> 8;
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
            throw JpegInvalidDataException.Create("Fancy chroma upsampling cannot reduce width.");
        }

        if (outputHeight < sourceHeight)
        {
            throw JpegInvalidDataException.Create("Fancy chroma upsampling cannot reduce height.");
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
            throw JpegInvalidDataException.Create("Chroma plane is too large.");
        }

        return (int)length;
    }
}
