using System.Collections.Generic;

namespace jpeg;

internal class JpegEncoderComponentConfigurator
{
    internal JpegChromaDownsampler ByteDownsampler = new JpegChromaDownsampler();
    internal JpegMcuLayoutCalculator LayoutCalculator = new JpegMcuLayoutCalculator();

    internal List<JpegEncoderComponent> ResolveComponents(int componentCount, JpegWriteOptions options, bool lossless)
    {
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        List<JpegEncoderComponent> components;
        if (options.Components.Count == 0)
        {
            components = CreateDefaultComponents(componentCount, options.Subsampling);
        }
        else
        {
            components = CopyExplicitComponents(options.Components);
        }

        ValidateComponents(components, componentCount, lossless);
        if (!lossless && options.ScanLayout != JpegScanLayout.NonInterleaved)
        {
            ValidateInterleavedDataUnitCount(components);
        }

        return components;
    }

    internal List<JpegEncoderComponent> CreateDefaultComponents(int componentCount, JpegSubsampling subsampling)
    {
        if (componentCount < 1 || componentCount > 4)
        {
            throw JpegWriteValidationException.Create("Encoder component count must be between one and four.");
        }

        List<JpegEncoderComponent> components = new List<JpegEncoderComponent>();
        int index = 0;
        while (index < componentCount)
        {
            JpegEncoderComponent component = new JpegEncoderComponent();
            component.SourceComponentIndex = index;
            component.Identifier = index + 1;
            components.Add(component);
            index = index + 1;
        }

        ApplySubsamplingPreset(components, componentCount, subsampling);
        return components;
    }

    internal List<JpegEncoderComponent> CopyExplicitComponents(List<JpegEncoderComponent> source)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("components");
        }

        List<JpegEncoderComponent> copy = new List<JpegEncoderComponent>();
        foreach (JpegEncoderComponent item in source)
        {
            if (item == null)
            {
                throw JpegWriteValidationException.Create("Encoder component mapping cannot be null.");
            }

            JpegEncoderComponent component = new JpegEncoderComponent();
            component.SourceComponentIndex = item.SourceComponentIndex;
            component.Identifier = item.Identifier;
            component.HorizontalSamplingFactor = item.HorizontalSamplingFactor;
            component.VerticalSamplingFactor = item.VerticalSamplingFactor;
            component.QuantizationTableIdentifier = item.QuantizationTableIdentifier;
            copy.Add(component);
        }

        return copy;
    }

    internal void ApplySubsamplingPreset(List<JpegEncoderComponent> components, int componentCount, JpegSubsampling subsampling)
    {
        if (componentCount == 1)
        {
            if (subsampling != JpegSubsampling.Unknown && subsampling != JpegSubsampling.Grayscale)
            {
                throw JpegWriteValidationException.Create("One-component encoding requires grayscale sampling.");
            }

            return;
        }

        if (subsampling == JpegSubsampling.Unknown || subsampling == JpegSubsampling.Ratio444)
        {
            return;
        }

        if (componentCount != 3)
        {
            throw JpegWriteValidationException.Create("Named chroma subsampling presets require three components.");
        }

        if (subsampling == JpegSubsampling.Ratio422)
        {
            components[0].HorizontalSamplingFactor = 2;
            return;
        }

        if (subsampling == JpegSubsampling.Ratio420)
        {
            components[0].HorizontalSamplingFactor = 2;
            components[0].VerticalSamplingFactor = 2;
            return;
        }

        if (subsampling == JpegSubsampling.Ratio440)
        {
            components[0].VerticalSamplingFactor = 2;
            return;
        }

        if (subsampling == JpegSubsampling.Ratio411)
        {
            components[0].HorizontalSamplingFactor = 4;
            return;
        }

        throw JpegWriteValidationException.Create("Custom subsampling requires explicit encoder component mappings.");
    }

    internal void ValidateComponents(List<JpegEncoderComponent> components, int sourceComponentCount, bool lossless)
    {
        if (components == null)
        {
            throw new System.ArgumentNullException("components");
        }

        if (components.Count != sourceComponentCount)
        {
            throw JpegWriteValidationException.Create("Encoder component mappings must cover every source component exactly once.");
        }

        int index = 0;
        while (index < components.Count)
        {
            JpegEncoderComponent component = components[index];
            ValidateComponent(component, components, index, sourceComponentCount, lossless);
            index = index + 1;
        }
    }

    internal void ValidateInterleavedDataUnitCount(List<JpegEncoderComponent> components)
    {
        int dataUnitCount = 0;
        int index = 0;
        while (index < components.Count)
        {
            JpegEncoderComponent component = components[index];
            dataUnitCount = dataUnitCount + component.HorizontalSamplingFactor * component.VerticalSamplingFactor;
            index = index + 1;
        }

        if (dataUnitCount > 10)
        {
            throw JpegWriteValidationException.Create("An interleaved DCT scan cannot contain more than ten data units per MCU.");
        }
    }

    internal void ValidateComponent(JpegEncoderComponent component, List<JpegEncoderComponent> components, int componentIndex, int sourceComponentCount, bool lossless)
    {
        if (component.SourceComponentIndex < 0 || component.SourceComponentIndex >= sourceComponentCount)
        {
            throw JpegWriteValidationException.Create("Source component index is outside the image component range.");
        }

        if (component.Identifier < 0 || component.Identifier > 255)
        {
            throw JpegWriteValidationException.Create("Encoded component identifier must fit in one byte.");
        }

        if (component.HorizontalSamplingFactor < 1 || component.HorizontalSamplingFactor > 4
            || component.VerticalSamplingFactor < 1 || component.VerticalSamplingFactor > 4)
        {
            throw JpegWriteValidationException.Create("Component sampling factors must be between one and four.");
        }

        if (lossless && (component.HorizontalSamplingFactor != 1 || component.VerticalSamplingFactor != 1))
        {
            throw JpegWriteValidationException.Create("Lossless JPEG encoding requires unit component sampling factors.");
        }

        if (component.QuantizationTableIdentifier < -1 || component.QuantizationTableIdentifier > 3)
        {
            throw JpegWriteValidationException.Create("Component quantization-table identifiers must be automatic or between zero and three.");
        }

        if (lossless && component.QuantizationTableIdentifier > 0)
        {
            throw JpegWriteValidationException.Create("Lossless JPEG components cannot select a quantization table.");
        }

        int index = 0;
        while (index < componentIndex)
        {
            if (components[index].SourceComponentIndex == component.SourceComponentIndex)
            {
                throw JpegWriteValidationException.Create("Source component indexes must be unique.");
            }

            if (components[index].Identifier == component.Identifier)
            {
                throw JpegWriteValidationException.Create("Encoded component identifiers must be unique.");
            }

            index = index + 1;
        }
    }

    internal JpegPlanarImageBuffer ConfigureByteSource(
        JpegPlanarImageBuffer source,
        JpegFrameHeader frame,
        List<JpegEncoderComponent> components,
        JpegChromaDownsamplingFilter filter)
    {
        JpegMcuLayout layout = LayoutCalculator.Calculate(frame);
        JpegPlanarImageBuffer result = new JpegPlanarImageBuffer();
        result.Width = source.Width;
        result.Height = source.Height;
        result.Precision = source.Precision;
        result.ComponentCount = components.Count;
        result.ColorSpace = source.ColorSpace;
        result.Subsampling = JpegSubsampling.Custom;
        result.Ownership = JpegBufferOwnership.LibraryOwned;

        int index = 0;
        while (index < components.Count)
        {
            JpegEncoderComponent mapping = components[index];
            JpegPlanarComponentBuffer sourceComponent = source.Components[mapping.SourceComponentIndex];
            JpegMcuComponentLayout componentLayout = layout.Components[index];
            JpegPlanarComponentBuffer destination = ByteDownsampler.DownsampleComponent(
                sourceComponent,
                componentLayout.SampleWidth,
                componentLayout.SampleHeight,
                filter);
            destination.ComponentIndex = index;
            destination.Identifier = mapping.Identifier;
            destination.HorizontalSamplingFactor = mapping.HorizontalSamplingFactor;
            destination.VerticalSamplingFactor = mapping.VerticalSamplingFactor;
            result.Components.Add(destination);
            index = index + 1;
        }

        return result;
    }

    internal JpegPlanarIntImageBuffer ConfigureIntegerSource(
        JpegPlanarIntImageBuffer source,
        JpegFrameHeader frame,
        List<JpegEncoderComponent> components,
        JpegChromaDownsamplingFilter filter)
    {
        JpegMcuLayout layout = LayoutCalculator.Calculate(frame);
        JpegPlanarIntImageBuffer result = new JpegPlanarIntImageBuffer();
        result.Width = source.Width;
        result.Height = source.Height;
        result.Precision = source.Precision;
        result.ComponentCount = components.Count;
        int index = 0;
        while (index < components.Count)
        {
            JpegEncoderComponent mapping = components[index];
            JpegMcuComponentLayout componentLayout = layout.Components[index];
            result.Components.Add(DownsampleIntegerComponent(
                source.Components[mapping.SourceComponentIndex],
                componentLayout,
                index,
                mapping.Identifier,
                filter));
            index = index + 1;
        }

        return result;
    }

    internal JpegPlanarIntComponentBuffer DownsampleIntegerComponent(
        JpegPlanarIntComponentBuffer source,
        JpegMcuComponentLayout layout,
        int componentIndex,
        int identifier,
        JpegChromaDownsamplingFilter filter)
    {
        JpegPlanarIntComponentBuffer destination = new JpegPlanarIntComponentBuffer();
        destination.ComponentIndex = componentIndex;
        destination.Identifier = identifier;
        destination.Width = layout.SampleWidth;
        destination.Height = layout.SampleHeight;
        destination.PaddedWidth = layout.SampleWidth;
        destination.PaddedHeight = layout.SampleHeight;
        destination.Stride = layout.SampleWidth;
        destination.Precision = source.Precision;
        destination.Samples = new int[layout.SampleWidth * layout.SampleHeight];
        int y = 0;
        while (y < destination.Height)
        {
            int x = 0;
            while (x < destination.Width)
            {
                destination.Samples[y * destination.Stride + x] = ReadDownsampledInteger(
                    source,
                    x,
                    y,
                    destination.Width,
                    destination.Height,
                    filter);
                x = x + 1;
            }

            y = y + 1;
        }

        return destination;
    }

    internal int ReadDownsampledInteger(
        JpegPlanarIntComponentBuffer source,
        int outputX,
        int outputY,
        int outputWidth,
        int outputHeight,
        JpegChromaDownsamplingFilter filter)
    {
        if (filter == JpegChromaDownsamplingFilter.Center)
        {
            int sourceX = ByteDownsampler.MapOutputCenterToSource(outputX, source.Width, outputWidth);
            int sourceY = ByteDownsampler.MapOutputCenterToSource(outputY, source.Height, outputHeight);
            return source.Samples[sourceY * source.Stride + sourceX];
        }

        int startX = ByteDownsampler.MapOutputStartToSource(outputX, source.Width, outputWidth);
        int endX = ByteDownsampler.MapOutputEndToSource(outputX, source.Width, outputWidth);
        int startY = ByteDownsampler.MapOutputStartToSource(outputY, source.Height, outputHeight);
        int endY = ByteDownsampler.MapOutputEndToSource(outputY, source.Height, outputHeight);
        long sum = 0;
        long count = 0;
        int y = startY;
        while (y < endY)
        {
            int x = startX;
            while (x < endX)
            {
                sum = sum + source.Samples[y * source.Stride + x];
                count = count + 1;
                x = x + 1;
            }

            y = y + 1;
        }

        return (int)((sum + count / 2L) / count);
    }
}
