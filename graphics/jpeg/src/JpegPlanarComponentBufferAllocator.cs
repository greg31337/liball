namespace jpeg;

internal class JpegPlanarComponentBufferAllocator
{
    internal JpegMcuLayoutCalculator LayoutCalculator = new JpegMcuLayoutCalculator();
    internal JpegEdgePadding EdgePadding = new JpegEdgePadding();

    internal JpegPlanarImageBuffer Allocate(JpegMcuLayout layout)
    {
        return AllocateWithPrecision(layout, 8);
    }

    internal JpegPlanarImageBuffer AllocateWithPrecision(JpegMcuLayout layout, int precision)
    {
        ValidateLayout(layout);
        ValidatePrecision(precision);

        JpegPlanarImageBuffer buffer = new JpegPlanarImageBuffer();
        buffer.Width = layout.Width;
        buffer.Height = layout.Height;
        buffer.Precision = precision;
        buffer.ComponentCount = layout.ComponentCount;
        buffer.Subsampling = JpegSubsampling.Custom;
        buffer.Ownership = JpegBufferOwnership.LibraryOwned;

        int index = 0;
        while (index < layout.ComponentCount)
        {
            JpegMcuComponentLayout componentLayout = layout.Components[index];
            buffer.Components.Add(AllocateComponentWithPrecisionAndStride(componentLayout, precision, componentLayout.PaddedSampleWidth));
            index = index + 1;
        }

        return buffer;
    }

    internal JpegPlanarImageBuffer AllocateForFrame(JpegFrameHeader frame)
    {
        JpegMcuLayout layout = LayoutCalculator.Calculate(frame);
        int precision = frame.Precision;
        if (precision == 0)
        {
            precision = 8;
        }

        return AllocateWithPrecision(layout, precision);
    }

    internal JpegPlanarComponentBuffer AllocateComponent(JpegMcuComponentLayout layout)
    {
        ValidateComponentLayout(layout);
        return AllocateComponentWithPrecisionAndStride(layout, 8, layout.PaddedSampleWidth);
    }

    internal JpegPlanarComponentBuffer AllocateComponentWithPrecisionAndStride(JpegMcuComponentLayout layout, int precision, int stride)
    {
        ValidateComponentLayout(layout);
        ValidatePrecision(precision);
        if (stride < layout.PaddedSampleWidth)
        {
            throw JpegInvalidDataException.Create("Planar component stride is smaller than its padded sample width.");
        }

        JpegPlanarComponentBuffer buffer = new JpegPlanarComponentBuffer();
        buffer.ComponentIndex = layout.ComponentIndex;
        buffer.Identifier = layout.Identifier;
        buffer.Precision = precision;
        buffer.HorizontalSamplingFactor = layout.HorizontalSamplingFactor;
        buffer.VerticalSamplingFactor = layout.VerticalSamplingFactor;
        buffer.Width = layout.SampleWidth;
        buffer.Height = layout.SampleHeight;
        buffer.PaddedWidth = layout.PaddedSampleWidth;
        buffer.PaddedHeight = layout.PaddedSampleHeight;
        buffer.Stride = stride;
        buffer.Ownership = JpegBufferOwnership.LibraryOwned;
        int length = GetStridedSampleLength(stride, buffer.PaddedHeight);
        if (precision == 8)
        {
            buffer.SampleStorage = JpegSampleStorage.Byte;
            buffer.Samples = new byte[length];
        }
        else
        {
            buffer.SampleStorage = JpegSampleStorage.Int32;
            buffer.IntSamples = new int[length];
        }

        return buffer;
    }

    internal void ValidatePrecision(int precision)
    {
        if (precision != 8 && precision != 12 && precision != 16)
        {
            throw JpegInvalidDataException.Create("Planar sample precision must be 8, 12, or 16 bits.");
        }
    }

    internal int GetStridedSampleLength(int stride, int height)
    {
        if (stride < 0)
        {
            throw new System.ArgumentOutOfRangeException("stride");
        }

        if (height < 0)
        {
            throw JpegInvalidDataException.Create("Planar sample height cannot be negative.");
        }

        long length = (long)stride * (long)height;
        if (length > int.MaxValue)
        {
            throw JpegInvalidDataException.Create("Planar sample buffer is too large.");
        }

        return (int)length;
    }

    internal JpegPlanarComponentBuffer FindComponentBuffer(JpegPlanarImageBuffer buffer, int identifier)
    {
        if (buffer == null)
        {
            throw new System.ArgumentNullException("buffer");
        }

        if (buffer.Components == null)
        {
            throw JpegInvalidDataException.Create("Planar component list cannot be null.");
        }

        foreach (JpegPlanarComponentBuffer component in buffer.Components)
        {
            if (component.Identifier == identifier)
            {
                return component;
            }
        }

        throw JpegInvalidDataException.Create("Planar component buffer was not found.");
    }

    internal void ValidateLayout(JpegMcuLayout layout)
    {
        if (layout == null)
        {
            throw new System.ArgumentNullException("layout");
        }

        if (layout.Width <= 0)
        {
            throw JpegInvalidDataException.Create("Layout width must be greater than zero.");
        }

        if (layout.Height < 0)
        {
            throw JpegInvalidDataException.Create("Layout height cannot be negative.");
        }

        if (layout.Components == null)
        {
            throw JpegInvalidDataException.Create("Layout component list cannot be null.");
        }

        if (layout.ComponentCount != layout.Components.Count)
        {
            throw JpegInvalidDataException.Create("Layout component count must match the component list.");
        }

        int index = 0;
        while (index < layout.ComponentCount)
        {
            ValidateComponentLayout(layout.Components[index]);
            ValidateComponentLayoutForImage(layout.Components[index], layout.Height);
            index = index + 1;
        }
    }

    internal void ValidateComponentLayout(JpegMcuComponentLayout layout)
    {
        if (layout == null)
        {
            throw JpegInvalidDataException.Create("Component layout cannot be null.");
        }

        EdgePadding.ValidatePlaneDimensions(layout.SampleWidth, layout.SampleHeight, layout.PaddedSampleWidth, layout.PaddedSampleHeight);
    }

    internal void ValidateComponentLayoutForImage(JpegMcuComponentLayout layout, int imageHeight)
    {
        if (imageHeight > 0)
        {
            if (layout.SampleWidth <= 0 || layout.SampleHeight <= 0)
            {
                throw JpegInvalidDataException.Create("Component layout dimensions must be non-empty.");
            }

            if (layout.PaddedSampleWidth <= 0 || layout.PaddedSampleHeight <= 0)
            {
                throw JpegInvalidDataException.Create("Component padded dimensions must be non-empty.");
            }
        }
    }
}
