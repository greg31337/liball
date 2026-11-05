namespace jpeg;

internal class JpegImageFactoryCore
{
    internal JpegInterleavedPixelBufferAllocator InterleavedAllocator = new JpegInterleavedPixelBufferAllocator();

    internal JpegImage CreatePackedImageFromByteSamples(
        System.ReadOnlySpan<byte> samples,
        int width,
        int height,
        int componentCount,
        int rowStride,
        JpegPixelFormat pixelFormat,
        JpegColorSpace colorSpace)
    {
        ValidateComponentCount(componentCount);
        JpegInterleavedPixelBuffer buffer = InterleavedAllocator.AllocateWithPrecisionAndStride(
            width,
            height,
            pixelFormat,
            componentCount,
            8,
            rowStride);
        ValidateResolvedComponentCount(buffer.ComponentCount, componentCount);
        ValidateAvailableSampleCount(samples.Length, buffer.Pixels.Length);
        CopyByteSamples(samples, 0, buffer.Pixels);
        return CreateImageFromInterleavedBuffer(buffer, colorSpace);
    }

    internal JpegImage CreatePackedImageFromIntSamples(
        System.ReadOnlySpan<int> samples,
        int width,
        int height,
        int componentCount,
        int rowStride,
        int precision,
        JpegColorSpace colorSpace)
    {
        ValidateComponentCount(componentCount);
        ValidateWidePrecision(precision);
        JpegInterleavedPixelBuffer buffer = InterleavedAllocator.AllocateWithPrecisionAndStride(
            width,
            height,
            JpegPixelFormat.RawComponents,
            componentCount,
            precision,
            rowStride);
        ValidateAvailableSampleCount(samples.Length, buffer.IntSamples.Length);
        ValidateIntSampleRange(samples, buffer.IntSamples.Length, precision);
        CopyIntSamples(samples, 0, buffer.IntSamples);
        return CreateImageFromInterleavedBuffer(buffer, colorSpace);
    }

    internal JpegImage CreatePlanarImageFromByteSamples(System.ReadOnlySpan<byte> samples, JpegPlanarImageBuffer layout)
    {
        ValidatePlanarLayout(layout);
        if (layout.Precision != 8)
        {
            throw JpegInvalidDataException.Create("Byte planar samples require 8-bit precision.");
        }

        int required = CalculatePlanarSampleCount(layout);
        ValidateAvailableSampleCount(samples.Length, required);
        JpegPlanarImageBuffer copy = CreatePlanarLayoutCopy(layout, JpegSampleStorage.Byte);
        int sourceOffset = 0;
        int index = 0;
        while (index < copy.ComponentCount)
        {
            JpegPlanarComponentBuffer component = copy.Components[index];
            int count = GetComponentSampleCount(component);
            component.Samples = new byte[count];
            CopyByteSamples(samples, sourceOffset, component.Samples);
            sourceOffset = sourceOffset + count;
            index = index + 1;
        }

        return CreateImageFromPlanarBuffer(copy);
    }

    internal JpegImage CreatePlanarImageFromIntSamples(System.ReadOnlySpan<int> samples, JpegPlanarImageBuffer layout)
    {
        ValidatePlanarLayout(layout);
        ValidateWidePrecision(layout.Precision);
        int required = CalculatePlanarSampleCount(layout);
        ValidateAvailableSampleCount(samples.Length, required);
        ValidateIntSampleRange(samples, required, layout.Precision);
        JpegPlanarImageBuffer copy = CreatePlanarLayoutCopy(layout, JpegSampleStorage.Int32);
        int sourceOffset = 0;
        int index = 0;
        while (index < copy.ComponentCount)
        {
            JpegPlanarComponentBuffer component = copy.Components[index];
            int count = GetComponentSampleCount(component);
            component.IntSamples = new int[count];
            CopyIntSamples(samples, sourceOffset, component.IntSamples);
            sourceOffset = sourceOffset + count;
            index = index + 1;
        }

        return CreateImageFromPlanarBuffer(copy);
    }

    internal JpegImage CreateImageFromInterleavedBuffer(JpegInterleavedPixelBuffer buffer, JpegColorSpace colorSpace)
    {
        JpegImage image = new JpegImage();
        image.Width = buffer.Width;
        image.Height = buffer.Height;
        image.Precision = buffer.Precision;
        image.ComponentCount = buffer.ComponentCount;
        image.PixelFormat = buffer.PixelFormat;
        image.ColorSpace = colorSpace;
        image.Subsampling = ResolvePackedSubsampling(buffer.ComponentCount);
        image.BufferLayout = JpegBufferLayout.Interleaved;
        image.BufferOwnership = JpegBufferOwnership.LibraryOwned;
        image.RowStride = buffer.Stride;
        image.Pixels = buffer.Pixels;
        image.InterleavedBuffer = buffer;
        return image;
    }

    internal JpegImage CreateImageFromPlanarBuffer(JpegPlanarImageBuffer buffer)
    {
        JpegImage image = new JpegImage();
        image.Width = buffer.Width;
        image.Height = buffer.Height;
        image.Precision = buffer.Precision;
        image.ComponentCount = buffer.ComponentCount;
        image.PixelFormat = JpegPixelFormat.RawComponents;
        image.ColorSpace = buffer.ColorSpace;
        image.Subsampling = buffer.Subsampling;
        image.BufferLayout = JpegBufferLayout.Planar;
        image.BufferOwnership = JpegBufferOwnership.LibraryOwned;
        image.RowStride = 0;
        image.PlanarBuffer = buffer;
        return image;
    }

    internal JpegPlanarImageBuffer CreatePlanarLayoutCopy(JpegPlanarImageBuffer layout, JpegSampleStorage storage)
    {
        JpegPlanarImageBuffer copy = new JpegPlanarImageBuffer();
        copy.Width = layout.Width;
        copy.Height = layout.Height;
        copy.Precision = layout.Precision;
        copy.ComponentCount = layout.ComponentCount;
        copy.ColorSpace = layout.ColorSpace;
        copy.Subsampling = layout.Subsampling;
        copy.Ownership = JpegBufferOwnership.LibraryOwned;

        int index = 0;
        while (index < layout.ComponentCount)
        {
            JpegPlanarComponentBuffer source = layout.Components[index];
            JpegPlanarComponentBuffer component = new JpegPlanarComponentBuffer();
            component.ComponentIndex = source.ComponentIndex;
            component.Identifier = source.Identifier;
            component.Precision = layout.Precision;
            component.HorizontalSamplingFactor = source.HorizontalSamplingFactor;
            component.VerticalSamplingFactor = source.VerticalSamplingFactor;
            component.Width = source.Width;
            component.Height = source.Height;
            component.PaddedWidth = source.PaddedWidth;
            component.PaddedHeight = source.PaddedHeight;
            component.Stride = source.Stride;
            component.SampleStorage = storage;
            component.Ownership = JpegBufferOwnership.LibraryOwned;
            copy.Components.Add(component);
            index = index + 1;
        }

        return copy;
    }

    internal void ValidatePlanarLayout(JpegPlanarImageBuffer layout)
    {
        if (layout == null)
        {
            throw new System.ArgumentNullException("layout");
        }

        if (layout.Width <= 0 || layout.Height <= 0)
        {
            throw JpegInvalidDataException.Create("Planar image dimensions must be greater than zero.");
        }

        ValidateComponentCount(layout.ComponentCount);
        if (layout.Components == null || layout.Components.Count != layout.ComponentCount)
        {
            throw JpegInvalidDataException.Create("Planar component count must match the component layout list.");
        }

        int index = 0;
        while (index < layout.ComponentCount)
        {
            ValidatePlanarComponent(layout.Components[index], layout.Precision);
            index = index + 1;
        }
    }

    internal void ValidatePlanarComponent(JpegPlanarComponentBuffer component, int precision)
    {
        if (component == null)
        {
            throw JpegInvalidDataException.Create("Planar component layout cannot be null.");
        }

        if (component.Precision != 0 && component.Precision != precision)
        {
            throw JpegInvalidDataException.Create("Planar component precision must match the image precision.");
        }

        if (component.HorizontalSamplingFactor < 1 || component.HorizontalSamplingFactor > 4
            || component.VerticalSamplingFactor < 1 || component.VerticalSamplingFactor > 4)
        {
            throw JpegInvalidDataException.Create("Planar component sampling factors must be between one and four.");
        }

        if (component.Width <= 0 || component.Height <= 0
            || component.PaddedWidth < component.Width || component.PaddedHeight < component.Height)
        {
            throw JpegInvalidDataException.Create("Planar component dimensions or padding are invalid.");
        }

        if (component.Stride < component.PaddedWidth)
        {
            throw JpegInvalidDataException.Create("Planar component stride is smaller than its padded width.");
        }

        GetComponentSampleCount(component);
    }

    internal int CalculatePlanarSampleCount(JpegPlanarImageBuffer layout)
    {
        long total = 0;
        int index = 0;
        while (index < layout.ComponentCount)
        {
            total = total + GetComponentSampleCount(layout.Components[index]);
            if (total > int.MaxValue)
            {
                throw JpegInvalidDataException.Create("Combined planar sample data is too large.");
            }

            index = index + 1;
        }

        return (int)total;
    }

    internal int GetComponentSampleCount(JpegPlanarComponentBuffer component)
    {
        long count = (long)component.Stride * (long)component.PaddedHeight;
        if (count < 0 || count > int.MaxValue)
        {
            throw JpegInvalidDataException.Create("Planar component sample data is too large.");
        }

        return (int)count;
    }

    internal void ValidateComponentCount(int componentCount)
    {
        if (componentCount <= 0)
        {
            throw JpegInvalidDataException.Create("Image component count must be greater than zero.");
        }
    }

    internal void ValidateResolvedComponentCount(int resolvedCount, int requestedCount)
    {
        if (resolvedCount != requestedCount)
        {
            throw JpegInvalidDataException.Create("Component count does not match the selected pixel format.");
        }
    }

    internal void ValidateWidePrecision(int precision)
    {
        if (precision != 12 && precision != 16)
        {
            throw JpegInvalidDataException.Create("Integer sample data requires 12-bit or 16-bit precision.");
        }
    }

    internal void ValidateAvailableSampleCount(int available, int required)
    {
        if (available < required)
        {
            throw JpegInvalidDataException.Create("Sample span is too small for the declared dimensions and strides.");
        }
    }

    internal void ValidateIntSampleRange(System.ReadOnlySpan<int> samples, int count, int precision)
    {
        int maximum = 65535;
        if (precision == 12)
        {
            maximum = 4095;
        }

        int index = 0;
        while (index < count)
        {
            int sample = samples[index];
            if (sample < 0 || sample > maximum)
            {
                throw JpegInvalidDataException.Create("Integer sample is outside the declared precision range.");
            }

            index = index + 1;
        }
    }

    internal void CopyByteSamples(System.ReadOnlySpan<byte> source, int sourceOffset, byte[] destination)
    {
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = source[sourceOffset + index];
            index = index + 1;
        }
    }

    internal void CopyIntSamples(System.ReadOnlySpan<int> source, int sourceOffset, int[] destination)
    {
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = source[sourceOffset + index];
            index = index + 1;
        }
    }

    internal JpegSubsampling ResolvePackedSubsampling(int componentCount)
    {
        if (componentCount == 1)
        {
            return JpegSubsampling.Grayscale;
        }

        if (componentCount == 3 || componentCount == 4)
        {
            return JpegSubsampling.Ratio444;
        }

        return JpegSubsampling.Custom;
    }
}
