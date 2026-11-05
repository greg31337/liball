namespace jpeg;

internal class JpegEdgePadding
{
    internal byte[] PadBytePlane(byte[] source, int width, int height, int paddedWidth, int paddedHeight)
    {
        ValidateBytePlane(source, width, height, paddedWidth, paddedHeight);
        byte[] destination = new byte[GetBufferLength(paddedWidth, paddedHeight)];
        CopyPaddedBytePlane(source, width, height, paddedWidth, paddedHeight, destination);
        return destination;
    }

    internal int[] PadIntPlane(int[] source, int width, int height, int paddedWidth, int paddedHeight)
    {
        ValidateIntPlane(source, width, height, paddedWidth, paddedHeight);
        int[] destination = new int[GetBufferLength(paddedWidth, paddedHeight)];
        CopyPaddedIntPlane(source, width, height, paddedWidth, paddedHeight, destination);
        return destination;
    }

    internal byte[] PadByteComponent(byte[] source, JpegMcuComponentLayout layout)
    {
        ValidateLayout(layout);
        return PadBytePlane(source, layout.SampleWidth, layout.SampleHeight, layout.PaddedSampleWidth, layout.PaddedSampleHeight);
    }

    internal int[] PadIntComponent(int[] source, JpegMcuComponentLayout layout)
    {
        ValidateLayout(layout);
        return PadIntPlane(source, layout.SampleWidth, layout.SampleHeight, layout.PaddedSampleWidth, layout.PaddedSampleHeight);
    }

    internal void CopyPaddedBytePlane(byte[] source, int width, int height, int paddedWidth, int paddedHeight, byte[] destination)
    {
        ValidateBytePlane(source, width, height, paddedWidth, paddedHeight);
        ValidateDestination(destination, paddedWidth, paddedHeight);

        int y = 0;
        while (y < paddedHeight)
        {
            int x = 0;
            while (x < paddedWidth)
            {
                int sourceIndex = GetClampedSourceIndex(x, y, width, height);
                int destinationIndex = y * paddedWidth + x;
                destination[destinationIndex] = source[sourceIndex];
                x = x + 1;
            }

            y = y + 1;
        }
    }

    internal void CopyPaddedIntPlane(int[] source, int width, int height, int paddedWidth, int paddedHeight, int[] destination)
    {
        ValidateIntPlane(source, width, height, paddedWidth, paddedHeight);
        ValidateIntDestination(destination, paddedWidth, paddedHeight);

        int y = 0;
        while (y < paddedHeight)
        {
            int x = 0;
            while (x < paddedWidth)
            {
                int sourceIndex = GetClampedSourceIndex(x, y, width, height);
                int destinationIndex = y * paddedWidth + x;
                destination[destinationIndex] = source[sourceIndex];
                x = x + 1;
            }

            y = y + 1;
        }
    }

    internal int GetClampedSourceIndex(int x, int y, int width, int height)
    {
        ValidateSourceCoordinates(x, y, width, height);
        int sourceX = x;
        if (sourceX >= width)
        {
            sourceX = width - 1;
        }

        int sourceY = y;
        if (sourceY >= height)
        {
            sourceY = height - 1;
        }

        return sourceY * width + sourceX;
    }

    internal int GetBufferLength(int width, int height)
    {
        if (width < 0)
        {
            throw new System.ArgumentOutOfRangeException("width");
        }

        if (height < 0)
        {
            throw new System.ArgumentOutOfRangeException("height");
        }

        long length = (long)width * (long)height;
        if (length > int.MaxValue)
        {
            throw JpegInvalidDataException.Create("Plane buffer is too large.");
        }

        return (int)length;
    }

    internal void ValidateBytePlane(byte[] source, int width, int height, int paddedWidth, int paddedHeight)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        ValidatePlaneDimensions(width, height, paddedWidth, paddedHeight);
        int sourceLength = GetBufferLength(width, height);
        if (source.Length < sourceLength)
        {
            throw JpegInvalidDataException.Create("Source byte plane is too small.");
        }
    }

    internal void ValidateIntPlane(int[] source, int width, int height, int paddedWidth, int paddedHeight)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        ValidatePlaneDimensions(width, height, paddedWidth, paddedHeight);
        int sourceLength = GetBufferLength(width, height);
        if (source.Length < sourceLength)
        {
            throw JpegInvalidDataException.Create("Source int plane is too small.");
        }
    }

    internal void ValidatePlaneDimensions(int width, int height, int paddedWidth, int paddedHeight)
    {
        if (width < 0)
        {
            throw new System.ArgumentOutOfRangeException("width");
        }

        if (height < 0)
        {
            throw new System.ArgumentOutOfRangeException("height");
        }

        if (paddedWidth < width)
        {
            throw new System.ArgumentOutOfRangeException("paddedWidth");
        }

        if (paddedHeight < height)
        {
            throw new System.ArgumentOutOfRangeException("paddedHeight");
        }

        if (paddedWidth > 0 && paddedHeight > 0 && (width == 0 || height == 0))
        {
            throw JpegInvalidDataException.Create("Cannot pad a non-empty plane from an empty edge.");
        }

        GetBufferLength(paddedWidth, paddedHeight);
    }

    internal void ValidateDestination(byte[] destination, int paddedWidth, int paddedHeight)
    {
        if (destination == null)
        {
            throw new System.ArgumentNullException("destination");
        }

        int destinationLength = GetBufferLength(paddedWidth, paddedHeight);
        if (destination.Length < destinationLength)
        {
            throw JpegInvalidDataException.Create("Destination byte plane is too small.");
        }
    }

    internal void ValidateIntDestination(int[] destination, int paddedWidth, int paddedHeight)
    {
        if (destination == null)
        {
            throw new System.ArgumentNullException("destination");
        }

        int destinationLength = GetBufferLength(paddedWidth, paddedHeight);
        if (destination.Length < destinationLength)
        {
            throw JpegInvalidDataException.Create("Destination int plane is too small.");
        }
    }

    internal void ValidateSourceCoordinates(int x, int y, int width, int height)
    {
        if (x < 0)
        {
            throw new System.ArgumentOutOfRangeException("x");
        }

        if (y < 0)
        {
            throw new System.ArgumentOutOfRangeException("y");
        }

        if (width <= 0)
        {
            throw new System.ArgumentOutOfRangeException("width");
        }

        if (height <= 0)
        {
            throw new System.ArgumentOutOfRangeException("height");
        }
    }

    internal void ValidateLayout(JpegMcuComponentLayout layout)
    {
        if (layout == null)
        {
            throw new System.ArgumentNullException("layout");
        }

        ValidatePlaneDimensions(layout.SampleWidth, layout.SampleHeight, layout.PaddedSampleWidth, layout.PaddedSampleHeight);
    }
}
