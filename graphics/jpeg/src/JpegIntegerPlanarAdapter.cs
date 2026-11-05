namespace jpeg;

internal class JpegIntegerPlanarAdapter
{
    internal JpegPlanarImageBuffer Convert(
        JpegPlanarIntImageBuffer source,
        JpegFrameHeader frame,
        JpegColorSpace colorSpace,
        JpegSubsampling subsampling)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        if (frame == null)
        {
            throw new System.ArgumentNullException("frame");
        }

        if (source.Components.Count != frame.Components.Count)
        {
            throw JpegInvalidDataException.Create("Decoded integer component count does not match the JPEG frame.");
        }

        JpegPlanarImageBuffer result = new JpegPlanarImageBuffer();
        result.Width = source.Width;
        result.Height = source.Height;
        result.Precision = source.Precision;
        result.ComponentCount = source.ComponentCount;
        result.ColorSpace = colorSpace;
        result.Subsampling = subsampling;
        result.Ownership = JpegBufferOwnership.LibraryOwned;

        int index = 0;
        while (index < source.Components.Count)
        {
            result.Components.Add(ConvertComponent(source.Components[index], frame.Components[index], source.Precision));
            index = index + 1;
        }

        return result;
    }

    internal JpegPlanarComponentBuffer ConvertComponent(
        JpegPlanarIntComponentBuffer source,
        JpegFrameComponent frameComponent,
        int precision)
    {
        JpegPlanarComponentBuffer result = new JpegPlanarComponentBuffer();
        result.ComponentIndex = source.ComponentIndex;
        result.Identifier = source.Identifier;
        result.Precision = precision;
        result.HorizontalSamplingFactor = frameComponent.HorizontalSamplingFactor;
        result.VerticalSamplingFactor = frameComponent.VerticalSamplingFactor;
        result.Width = source.Width;
        result.Height = source.Height;
        result.PaddedWidth = source.PaddedWidth;
        result.PaddedHeight = source.PaddedHeight;
        result.Stride = source.Stride;
        result.Ownership = JpegBufferOwnership.LibraryOwned;
        if (precision <= 8)
        {
            result.SampleStorage = JpegSampleStorage.Byte;
            result.Samples = ConvertSamplesToBytes(source.Samples);
        }
        else
        {
            result.SampleStorage = JpegSampleStorage.Int32;
            result.IntSamples = source.Samples;
        }

        return result;
    }

    internal byte[] ConvertSamplesToBytes(int[] samples)
    {
        if (samples == null)
        {
            throw new System.ArgumentNullException("samples");
        }

        byte[] result = new byte[samples.Length];
        int index = 0;
        while (index < samples.Length)
        {
            int sample = samples[index];
            if (sample < 0 || sample > 255)
            {
                throw JpegInvalidDataException.Create("Decoded sample is outside the eight-bit range.");
            }

            result[index] = (byte)sample;
            index = index + 1;
        }

        return result;
    }
}
