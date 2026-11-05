namespace jpeg;

internal class JpegIntegerImageSource
{
    internal JpegPlanarIntImageBuffer Create(JpegImage image, int precision)
    {
        ValidateImage(image, precision);
        if (image.BufferLayout == JpegBufferLayout.Planar && image.PlanarBuffer.Components.Count > 0)
        {
            return CreateFromPlanar(image, precision);
        }

        return CreateFromInterleaved(image, precision);
    }

    internal JpegPlanarIntImageBuffer CreateFromPlanar(JpegImage image, int precision)
    {
        JpegPlanarImageBuffer publicBuffer = image.PlanarBuffer;
        if (publicBuffer == null)
        {
            throw JpegWriteValidationException.Create("Planar image buffer cannot be null.");
        }

        if (publicBuffer.Components == null || publicBuffer.Components.Count != image.ComponentCount)
        {
            throw JpegWriteValidationException.Create("Planar component count must match the image component count.");
        }

        JpegPlanarIntImageBuffer result = CreateEmptyImage(image, precision);
        int componentIndex = 0;
        while (componentIndex < image.ComponentCount)
        {
            JpegPlanarComponentBuffer source = publicBuffer.Components[componentIndex];
            ValidatePlanarComponent(source, image, precision);
            JpegPlanarIntComponentBuffer destination = CreateEmptyComponent(image, precision, componentIndex, componentIndex + 1);
            CopyPlanarSamples(source, destination, precision);
            result.Components.Add(destination);
            componentIndex = componentIndex + 1;
        }

        return result;
    }

    internal JpegPlanarIntImageBuffer CreateFromInterleaved(JpegImage image, int precision)
    {
        JpegPlanarIntImageBuffer result = CreateEmptyImage(image, precision);
        int componentIndex = 0;
        while (componentIndex < image.ComponentCount)
        {
            result.Components.Add(CreateEmptyComponent(image, precision, componentIndex, componentIndex + 1));
            componentIndex = componentIndex + 1;
        }

        int stride = ResolveInterleavedStride(image);
        ValidateInterleavedStorage(image, precision, stride);
        int y = 0;
        while (y < image.Height)
        {
            int x = 0;
            while (x < image.Width)
            {
                componentIndex = 0;
                while (componentIndex < image.ComponentCount)
                {
                    int sourceIndex = y * stride + x * image.ComponentCount + componentIndex;
                    result.Components[componentIndex].Samples[y * image.Width + x] = ReadInterleavedSample(image, precision, sourceIndex);
                    componentIndex = componentIndex + 1;
                }

                x = x + 1;
            }

            y = y + 1;
        }

        return result;
    }

    internal JpegPlanarIntImageBuffer CreateEmptyImage(JpegImage image, int precision)
    {
        JpegPlanarIntImageBuffer result = new JpegPlanarIntImageBuffer();
        result.Width = image.Width;
        result.Height = image.Height;
        result.ComponentCount = image.ComponentCount;
        result.Precision = precision;
        return result;
    }

    internal JpegPlanarIntComponentBuffer CreateEmptyComponent(JpegImage image, int precision, int componentIndex, int identifier)
    {
        JpegPlanarIntComponentBuffer component = new JpegPlanarIntComponentBuffer();
        component.ComponentIndex = componentIndex;
        component.Identifier = identifier;
        component.Width = image.Width;
        component.Height = image.Height;
        component.PaddedWidth = image.Width;
        component.PaddedHeight = image.Height;
        component.Stride = image.Width;
        component.Precision = precision;
        component.Samples = new int[image.Width * image.Height];
        return component;
    }

    internal void CopyPlanarSamples(JpegPlanarComponentBuffer source, JpegPlanarIntComponentBuffer destination, int precision)
    {
        int y = 0;
        while (y < destination.Height)
        {
            int x = 0;
            while (x < destination.Width)
            {
                int sourceIndex = y * source.Stride + x;
                int value;
                if (precision > 8)
                {
                    value = source.IntSamples[sourceIndex];
                }
                else
                {
                    value = source.Samples[sourceIndex];
                }

                destination.Samples[y * destination.Stride + x] = value;
                x = x + 1;
            }

            y = y + 1;
        }
    }

    internal int ReadInterleavedSample(JpegImage image, int precision, int sourceIndex)
    {
        if (precision > 8)
        {
            return image.InterleavedBuffer.IntSamples[sourceIndex];
        }

        if (image.InterleavedBuffer.Pixels.Length > 0)
        {
            return image.InterleavedBuffer.Pixels[sourceIndex];
        }

        return image.Pixels[sourceIndex];
    }

    internal int ResolveInterleavedStride(JpegImage image)
    {
        int stride = image.InterleavedBuffer.Stride;
        if (stride == 0)
        {
            stride = image.RowStride;
        }

        if (stride == 0)
        {
            stride = image.Width * image.ComponentCount;
        }

        if (stride < image.Width * image.ComponentCount)
        {
            throw JpegWriteValidationException.Create("Interleaved sample stride is smaller than one image row.");
        }

        return stride;
    }

    internal void ValidateImage(JpegImage image, int precision)
    {
        if (image == null)
        {
            throw new System.ArgumentNullException("image");
        }

        if (image.Width <= 0 || image.Height <= 0)
        {
            throw JpegWriteValidationException.Create("Image dimensions must be greater than zero.");
        }

        if (image.ComponentCount < 1 || image.ComponentCount > 4)
        {
            throw JpegWriteValidationException.Create("This encoder path requires between one and four image components.");
        }

        if (image.Precision != 0 && image.Precision != precision)
        {
            throw JpegWriteValidationException.Create("Image precision must match the requested encoder precision.");
        }
    }

    internal void ValidatePlanarComponent(JpegPlanarComponentBuffer component, JpegImage image, int precision)
    {
        if (component == null)
        {
            throw JpegWriteValidationException.Create("Planar component cannot be null.");
        }

        if (component.Width != image.Width || component.Height != image.Height)
        {
            throw JpegWriteValidationException.Create("This encoder process requires full-resolution component planes.");
        }

        if (component.Stride < component.Width)
        {
            throw JpegWriteValidationException.Create("Planar component stride is smaller than its width.");
        }

        long required = (long)component.Stride * (long)component.Height;
        if (precision > 8)
        {
            if (component.IntSamples == null || required > component.IntSamples.Length)
            {
                throw JpegWriteValidationException.Create("Planar integer sample buffer is too small.");
            }
        }
        else
        {
            if (component.Samples == null || required > component.Samples.Length)
            {
                throw JpegWriteValidationException.Create("Planar byte sample buffer is too small.");
            }
        }
    }

    internal void ValidateInterleavedStorage(JpegImage image, int precision, int stride)
    {
        long required = (long)stride * (long)image.Height;
        if (precision > 8)
        {
            if (image.InterleavedBuffer == null || image.InterleavedBuffer.IntSamples == null || required > image.InterleavedBuffer.IntSamples.Length)
            {
                throw JpegWriteValidationException.Create("Interleaved integer sample buffer is too small.");
            }

            return;
        }

        int available = image.Pixels.Length;
        if (image.InterleavedBuffer != null && image.InterleavedBuffer.Pixels != null && image.InterleavedBuffer.Pixels.Length > 0)
        {
            available = image.InterleavedBuffer.Pixels.Length;
        }

        if (required > available)
        {
            throw JpegWriteValidationException.Create("Interleaved byte sample buffer is too small.");
        }
    }
}
