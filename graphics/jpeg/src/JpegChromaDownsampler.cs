namespace jpeg;

internal class JpegChromaDownsampler
{
    internal JpegPlanarComponentBuffer DownsampleComponent(
        JpegPlanarComponentBuffer source,
        int outputWidth,
        int outputHeight,
        JpegChromaDownsamplingFilter filter)
    {
        ValidateComponentForOutput(source, outputWidth, outputHeight);
        ValidateFilter(filter);

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
        destination.Samples = Downsample(source.Samples, source.Width, source.Height, source.Stride, outputWidth, outputHeight, filter);
        return destination;
    }

    internal byte[] Downsample(
        byte[] source,
        int sourceWidth,
        int sourceHeight,
        int sourceStride,
        int outputWidth,
        int outputHeight,
        JpegChromaDownsamplingFilter filter)
    {
        ValidateSource(source, sourceWidth, sourceHeight, sourceStride);
        ValidateOutputDimensions(sourceWidth, sourceHeight, outputWidth, outputHeight);
        ValidateFilter(filter);

        byte[] destination = new byte[GetStridedLength(outputWidth, outputHeight)];
        DownsampleTo(source, sourceWidth, sourceHeight, sourceStride, outputWidth, outputHeight, destination, outputWidth, filter);
        return destination;
    }

    internal void DownsampleTo(
        byte[] source,
        int sourceWidth,
        int sourceHeight,
        int sourceStride,
        int outputWidth,
        int outputHeight,
        byte[] destination,
        int destinationStride,
        JpegChromaDownsamplingFilter filter)
    {
        ValidateSource(source, sourceWidth, sourceHeight, sourceStride);
        ValidateOutputDimensions(sourceWidth, sourceHeight, outputWidth, outputHeight);
        ValidateDestination(destination, outputWidth, outputHeight, destinationStride);
        ValidateFilter(filter);

        int y = 0;
        while (y < outputHeight)
        {
            int destinationIndex = y * destinationStride;
            int x = 0;
            while (x < outputWidth)
            {
                destination[destinationIndex] = (byte)ReadDownsampledSample(source, sourceWidth, sourceHeight, sourceStride, x, y, outputWidth, outputHeight, filter);
                destinationIndex = destinationIndex + 1;
                x = x + 1;
            }

            y = y + 1;
        }
    }

    internal int ReadDownsampledSample(
        byte[] source,
        int sourceWidth,
        int sourceHeight,
        int sourceStride,
        int outputX,
        int outputY,
        int outputWidth,
        int outputHeight,
        JpegChromaDownsamplingFilter filter)
    {
        ValidateSource(source, sourceWidth, sourceHeight, sourceStride);
        ValidateOutputDimensions(sourceWidth, sourceHeight, outputWidth, outputHeight);
        ValidateOutputCoordinate(outputX, outputY, outputWidth, outputHeight);
        ValidateFilter(filter);

        if (filter == JpegChromaDownsamplingFilter.Center)
        {
            return ReadCenterSample(source, sourceWidth, sourceHeight, sourceStride, outputX, outputY, outputWidth, outputHeight);
        }

        return ReadAverageBoxSample(source, sourceWidth, sourceHeight, sourceStride, outputX, outputY, outputWidth, outputHeight);
    }

    internal int ReadAverageBoxSample(
        byte[] source,
        int sourceWidth,
        int sourceHeight,
        int sourceStride,
        int outputX,
        int outputY,
        int outputWidth,
        int outputHeight)
    {
        int startX = MapOutputStartToSource(outputX, sourceWidth, outputWidth);
        int endX = MapOutputEndToSource(outputX, sourceWidth, outputWidth);
        int startY = MapOutputStartToSource(outputY, sourceHeight, outputHeight);
        int endY = MapOutputEndToSource(outputY, sourceHeight, outputHeight);

        long sum = 0;
        long count = 0;
        int y = startY;
        while (y < endY)
        {
            int sourceIndex = y * sourceStride + startX;
            int x = startX;
            while (x < endX)
            {
                sum = sum + source[sourceIndex];
                count = count + 1;
                sourceIndex = sourceIndex + 1;
                x = x + 1;
            }

            y = y + 1;
        }

        return (int)((sum + count / 2L) / count);
    }

    internal int ReadCenterSample(
        byte[] source,
        int sourceWidth,
        int sourceHeight,
        int sourceStride,
        int outputX,
        int outputY,
        int outputWidth,
        int outputHeight)
    {
        int sourceX = MapOutputCenterToSource(outputX, sourceWidth, outputWidth);
        int sourceY = MapOutputCenterToSource(outputY, sourceHeight, outputHeight);
        return source[sourceY * sourceStride + sourceX];
    }

    internal int MapOutputStartToSource(int outputPosition, int sourceLength, int outputLength)
    {
        ValidateMapInput(outputPosition, sourceLength, outputLength);
        return (int)((long)outputPosition * (long)sourceLength / (long)outputLength);
    }

    internal int MapOutputEndToSource(int outputPosition, int sourceLength, int outputLength)
    {
        ValidateMapInput(outputPosition, sourceLength, outputLength);

        long end = (long)(outputPosition + 1) * (long)sourceLength / (long)outputLength;
        int start = MapOutputStartToSource(outputPosition, sourceLength, outputLength);
        if (end <= start)
        {
            end = start + 1;
        }

        if (end > sourceLength)
        {
            end = sourceLength;
        }

        return (int)end;
    }

    internal int MapOutputCenterToSource(int outputPosition, int sourceLength, int outputLength)
    {
        ValidateMapInput(outputPosition, sourceLength, outputLength);

        long index = ((long)outputPosition * 2L + 1L) * (long)sourceLength / ((long)outputLength * 2L);
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

        if (outputWidth > sourceWidth)
        {
            throw JpegInvalidDataException.Create("Chroma downsampling cannot increase width.");
        }

        if (outputHeight > sourceHeight)
        {
            throw JpegInvalidDataException.Create("Chroma downsampling cannot increase height.");
        }

        if (outputHeight > 0 && sourceHeight == 0)
        {
            throw JpegInvalidDataException.Create("Cannot downsample non-empty output from an empty source.");
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

    internal void ValidateOutputCoordinate(int outputX, int outputY, int outputWidth, int outputHeight)
    {
        if (outputX < 0 || outputX >= outputWidth)
        {
            throw new System.ArgumentOutOfRangeException("outputX");
        }

        if (outputY < 0 || outputY >= outputHeight)
        {
            throw new System.ArgumentOutOfRangeException("outputY");
        }
    }

    internal void ValidateMapInput(int outputPosition, int sourceLength, int outputLength)
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
    }

    internal void ValidateFilter(JpegChromaDownsamplingFilter filter)
    {
        if (filter == JpegChromaDownsamplingFilter.Box)
        {
            return;
        }

        if (filter == JpegChromaDownsamplingFilter.Center)
        {
            return;
        }

        throw JpegInvalidDataException.Create("Unsupported chroma downsampling filter.");
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
