namespace jpeg;

internal class JpegYCbCrRangeConverter
{
    internal JpegPlanarImageBuffer ExpandLimitedRange(JpegPlanarImageBuffer source)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        if (source.ComponentCount < 3 || source.Components.Count < source.ComponentCount)
        {
            throw JpegInvalidDataException.Create("Limited-range YCbCr conversion requires at least three component planes.");
        }

        JpegPlanarImageBuffer result = new JpegPlanarImageBuffer();
        result.Width = source.Width;
        result.Height = source.Height;
        result.Precision = source.Precision;
        result.ComponentCount = source.ComponentCount;
        result.ColorSpace = source.ColorSpace;
        result.Subsampling = source.Subsampling;
        result.Ownership = JpegBufferOwnership.LibraryOwned;

        int index = 0;
        while (index < source.ComponentCount)
        {
            JpegPlanarComponentBuffer component = CopyComponent(source.Components[index]);
            if (index == 0)
            {
                ExpandLuminance(component);
            }
            else if (index < 3)
            {
                ExpandChroma(component);
            }

            result.Components.Add(component);
            index = index + 1;
        }

        return result;
    }

    internal JpegPlanarComponentBuffer CopyComponent(JpegPlanarComponentBuffer source)
    {
        JpegPlanarComponentBuffer result = new JpegPlanarComponentBuffer();
        result.ComponentIndex = source.ComponentIndex;
        result.Identifier = source.Identifier;
        result.Precision = source.Precision;
        result.HorizontalSamplingFactor = source.HorizontalSamplingFactor;
        result.VerticalSamplingFactor = source.VerticalSamplingFactor;
        result.Width = source.Width;
        result.Height = source.Height;
        result.PaddedWidth = source.PaddedWidth;
        result.PaddedHeight = source.PaddedHeight;
        result.Stride = source.Stride;
        result.SampleStorage = source.SampleStorage;
        result.Ownership = JpegBufferOwnership.LibraryOwned;
        result.Samples = new byte[source.Samples.Length];
        System.Array.Copy(source.Samples, result.Samples, source.Samples.Length);
        result.IntSamples = new int[source.IntSamples.Length];
        System.Array.Copy(source.IntSamples, result.IntSamples, source.IntSamples.Length);
        return result;
    }

    internal void ExpandLuminance(JpegPlanarComponentBuffer component)
    {
        if (component.SampleStorage != JpegSampleStorage.Byte)
        {
            throw JpegUnsupportedException.Create("Limited-range YCbCr conversion currently supports eight-bit component samples.");
        }

        int y = 0;
        while (y < component.Height)
        {
            int index = y * component.Stride;
            int end = index + component.Width;
            while (index < end)
            {
                component.Samples[index] = (byte)ExpandSample(component.Samples[index], 16, 219);
                index = index + 1;
            }

            y = y + 1;
        }
    }

    internal void ExpandChroma(JpegPlanarComponentBuffer component)
    {
        if (component.SampleStorage != JpegSampleStorage.Byte)
        {
            throw JpegUnsupportedException.Create("Limited-range YCbCr conversion currently supports eight-bit component samples.");
        }

        int y = 0;
        while (y < component.Height)
        {
            int index = y * component.Stride;
            int end = index + component.Width;
            while (index < end)
            {
                component.Samples[index] = (byte)ExpandSample(component.Samples[index], 16, 224);
                index = index + 1;
            }

            y = y + 1;
        }
    }

    internal int ExpandSample(int sample, int offset, int range)
    {
        int result = ((sample - offset) * 255 + range / 2) / range;
        if (result < 0)
        {
            return 0;
        }

        if (result > 255)
        {
            return 255;
        }

        return result;
    }
}
