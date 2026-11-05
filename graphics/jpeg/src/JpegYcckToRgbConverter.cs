namespace jpeg;

internal class JpegYcckToRgbConverter
{
    internal JpegInterleavedPixelBufferAllocator Allocator = new JpegInterleavedPixelBufferAllocator();
    internal JpegYcckToCmykConverter YcckToCmykConverter = new JpegYcckToCmykConverter();
    internal JpegCmykToRgbConverter CmykToRgbConverter = new JpegCmykToRgbConverter();

    internal JpegInterleavedPixelBuffer ConvertPlanarToRgb(JpegPlanarImageBuffer source)
    {
        ValidatePlanarBuffer(source);

        JpegInterleavedPixelBuffer destination = Allocator.Allocate(source.Width, source.Height, JpegPixelFormat.Rgb24, 0);
        CopyPlanarToRgb(
            source.Components[0],
            source.Components[1],
            source.Components[2],
            source.Components[3],
            source.Width,
            source.Height,
            destination.Pixels,
            destination.Stride);
        return destination;
    }

    internal JpegImage ConvertPlanarToImage(JpegPlanarImageBuffer source)
    {
        JpegInterleavedPixelBuffer buffer = ConvertPlanarToRgb(source);

        JpegImage image = new JpegImage();
        image.Width = buffer.Width;
        image.Height = buffer.Height;
        image.Precision = 8;
        image.ComponentCount = 3;
        image.PixelFormat = JpegPixelFormat.Rgb24;
        image.ColorSpace = JpegColorSpace.RGB;
        image.Pixels = buffer.Pixels;
        return image;
    }

    internal JpegInterleavedPixelBuffer ConvertImageToRgbBuffer(JpegImage source)
    {
        ValidateYcckImage(source);

        JpegInterleavedPixelBuffer destination = Allocator.Allocate(source.Width, source.Height, JpegPixelFormat.Rgb24, 0);
        CopyPackedYcckToRgb(
            source.Pixels,
            source.Width,
            source.Height,
            YcckToCmykConverter.GetPackedRowByteCount(source.Width),
            destination.Pixels,
            destination.Stride);
        return destination;
    }

    internal JpegImage ConvertImageToRgbImage(JpegImage source)
    {
        JpegInterleavedPixelBuffer buffer = ConvertImageToRgbBuffer(source);

        int precision = source.Precision;
        if (precision <= 0)
        {
            precision = 8;
        }

        JpegImage image = new JpegImage();
        image.Width = buffer.Width;
        image.Height = buffer.Height;
        image.Precision = precision;
        image.ComponentCount = 3;
        image.PixelFormat = JpegPixelFormat.Rgb24;
        image.ColorSpace = JpegColorSpace.RGB;
        image.Pixels = buffer.Pixels;
        return image;
    }

    internal byte[] ConvertPackedYcckToRgb(byte[] source, int width, int height)
    {
        YcckToCmykConverter.ValidateDimensions(width, height);

        int sourceStride = YcckToCmykConverter.GetPackedRowByteCount(width);
        int destinationStride = CmykToRgbConverter.GetRgbRowByteCount(width);
        byte[] destination = new byte[CmykToRgbConverter.GetStridedLength(destinationStride, height)];
        CopyPackedYcckToRgb(source, width, height, sourceStride, destination, destinationStride);
        return destination;
    }

    internal void CopyPackedYcckToRgb(
        byte[] source,
        int width,
        int height,
        int sourceStride,
        byte[] destination,
        int destinationStride)
    {
        int rowByteCount = YcckToCmykConverter.ValidateYcckSource(source, width, height, sourceStride);
        CmykToRgbConverter.ValidateRgbDestination(destination, width, height, destinationStride);

        int y = 0;
        while (y < height)
        {
            int sourceIndex = y * sourceStride;
            int destinationIndex = y * destinationStride;
            int rowEnd = sourceIndex + rowByteCount;
            while (sourceIndex < rowEnd)
            {
                WriteRgbPixel(
                    source[sourceIndex],
                    source[sourceIndex + 1],
                    source[sourceIndex + 2],
                    source[sourceIndex + 3],
                    destination,
                    destinationIndex);
                sourceIndex = sourceIndex + 4;
                destinationIndex = destinationIndex + 3;
            }

            y = y + 1;
        }
    }

    internal void CopyPlanarToRgb(
        JpegPlanarComponentBuffer luminance,
        JpegPlanarComponentBuffer cb,
        JpegPlanarComponentBuffer cr,
        JpegPlanarComponentBuffer black,
        int outputWidth,
        int outputHeight,
        byte[] destination,
        int destinationStride)
    {
        YcckToCmykConverter.ValidateComponentForOutput(luminance, outputWidth, outputHeight, "luminance");
        YcckToCmykConverter.ValidateComponentForOutput(cb, outputWidth, outputHeight, "cb");
        YcckToCmykConverter.ValidateComponentForOutput(cr, outputWidth, outputHeight, "cr");
        YcckToCmykConverter.ValidateComponentForOutput(black, outputWidth, outputHeight, "black");
        CmykToRgbConverter.ValidateRgbDestination(destination, outputWidth, outputHeight, destinationStride);

        int y = 0;
        while (y < outputHeight)
        {
            int luminanceIndex = y * luminance.Stride;
            int cbIndex = y * cb.Stride;
            int crIndex = y * cr.Stride;
            int blackIndex = y * black.Stride;
            int destinationIndex = y * destinationStride;
            int x = 0;
            while (x < outputWidth)
            {
                WriteRgbPixel(
                    luminance.Samples[luminanceIndex],
                    cb.Samples[cbIndex],
                    cr.Samples[crIndex],
                    black.Samples[blackIndex],
                    destination,
                    destinationIndex);
                luminanceIndex = luminanceIndex + 1;
                cbIndex = cbIndex + 1;
                crIndex = crIndex + 1;
                blackIndex = blackIndex + 1;
                destinationIndex = destinationIndex + 3;
                x = x + 1;
            }

            y = y + 1;
        }
    }

    internal void WriteRgbPixel(int luminance, int cb, int cr, int black, byte[] destination, int destinationIndex)
    {
        YcckToCmykConverter.ValidateYcckSample(luminance, cb, cr, black);

        if (destination == null)
        {
            throw new System.ArgumentNullException("destination");
        }

        if (destinationIndex < 0 || destinationIndex + 2 >= destination.Length)
        {
            throw JpegInvalidDataException.Create("RGB destination index is outside the buffer.");
        }

        destination[destinationIndex] = (byte)ConvertSampleToRed(luminance, cb, cr, black);
        destination[destinationIndex + 1] = (byte)ConvertSampleToGreen(luminance, cb, cr, black);
        destination[destinationIndex + 2] = (byte)ConvertSampleToBlue(luminance, cb, cr, black);
    }

    internal int ConvertSampleToRed(int luminance, int cb, int cr, int black)
    {
        int cyan = YcckToCmykConverter.ConvertSampleToCyan(luminance, cb, cr, black);
        int magenta = YcckToCmykConverter.ConvertSampleToMagenta(luminance, cb, cr, black);
        int yellow = YcckToCmykConverter.ConvertSampleToYellow(luminance, cb, cr, black);
        int decodedBlack = YcckToCmykConverter.ConvertSampleToBlack(luminance, cb, cr, black);
        return CmykToRgbConverter.ConvertSampleToRed(cyan, magenta, yellow, decodedBlack);
    }

    internal int ConvertSampleToGreen(int luminance, int cb, int cr, int black)
    {
        int cyan = YcckToCmykConverter.ConvertSampleToCyan(luminance, cb, cr, black);
        int magenta = YcckToCmykConverter.ConvertSampleToMagenta(luminance, cb, cr, black);
        int yellow = YcckToCmykConverter.ConvertSampleToYellow(luminance, cb, cr, black);
        int decodedBlack = YcckToCmykConverter.ConvertSampleToBlack(luminance, cb, cr, black);
        return CmykToRgbConverter.ConvertSampleToGreen(cyan, magenta, yellow, decodedBlack);
    }

    internal int ConvertSampleToBlue(int luminance, int cb, int cr, int black)
    {
        int cyan = YcckToCmykConverter.ConvertSampleToCyan(luminance, cb, cr, black);
        int magenta = YcckToCmykConverter.ConvertSampleToMagenta(luminance, cb, cr, black);
        int yellow = YcckToCmykConverter.ConvertSampleToYellow(luminance, cb, cr, black);
        int decodedBlack = YcckToCmykConverter.ConvertSampleToBlack(luminance, cb, cr, black);
        return CmykToRgbConverter.ConvertSampleToBlue(cyan, magenta, yellow, decodedBlack);
    }

    internal void ValidatePlanarBuffer(JpegPlanarImageBuffer source)
    {
        YcckToCmykConverter.ValidatePlanarBuffer(source);
    }

    internal void ValidateYcckImage(JpegImage source)
    {
        YcckToCmykConverter.ValidateYcckImage(source);
    }
}
