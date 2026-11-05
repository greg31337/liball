namespace jpeg;

internal class JpegExifOrientationTransformer
{
    internal JpegImage Apply(JpegImage image)
    {
        if (image == null)
        {
            throw new System.ArgumentNullException("image");
        }

        return ApplyExifOrientation(image, image.Metadata.ExifMetadata);
    }

    internal JpegImage ApplyExifOrientation(JpegImage image, JpegExifMetadata exif)
    {
        if (image == null)
        {
            throw new System.ArgumentNullException("image");
        }

        if (exif == null)
        {
            throw new System.ArgumentNullException("exif");
        }

        if (!exif.HasOrientation)
        {
            return ApplyOrientation(image, 1);
        }

        return ApplyOrientation(image, exif.Orientation);
    }

    internal JpegImage ApplyOrientation(JpegImage image, int orientation)
    {
        if (image == null)
        {
            throw new System.ArgumentNullException("image");
        }

        ValidateOrientation(orientation);
        int bytesPerPixel = GetBytesPerPixel(image);
        ValidateImageBuffer(image, bytesPerPixel);
        int outputWidth = GetOutputWidth(image, orientation);
        int outputHeight = GetOutputHeight(image, orientation);
        byte[] pixels = new byte[outputWidth * outputHeight * bytesPerPixel];
        int y = 0;
        while (y < outputHeight)
        {
            int x = 0;
            while (x < outputWidth)
            {
                int sourceX = GetSourceX(image.Width, image.Height, x, y, orientation);
                int sourceY = GetSourceY(image.Width, image.Height, x, y, orientation);
                CopyPixel(image.Pixels, pixels, image.Width, outputWidth, bytesPerPixel, sourceX, sourceY, x, y);
                x = x + 1;
            }

            y = y + 1;
        }

        return CopyImageWithPixels(image, outputWidth, outputHeight, pixels);
    }

    internal int GetBytesPerPixel(JpegImage image)
    {
        if (image.PixelFormat == JpegPixelFormat.L8)
        {
            return 1;
        }

        if (image.PixelFormat == JpegPixelFormat.Rgb24 || image.PixelFormat == JpegPixelFormat.Bgr24 || image.PixelFormat == JpegPixelFormat.YCbCr24)
        {
            return 3;
        }

        if (image.PixelFormat == JpegPixelFormat.Rgba32
            || image.PixelFormat == JpegPixelFormat.Bgra32
            || image.PixelFormat == JpegPixelFormat.Cmyk32
            || image.PixelFormat == JpegPixelFormat.Ycck32)
        {
            return 4;
        }

        if (image.PixelFormat == JpegPixelFormat.RawComponents && image.ComponentCount > 0)
        {
            return image.ComponentCount;
        }

        throw JpegInvalidDataException.Create("Cannot auto-orient an image with an unknown pixel size.");
    }

    internal void ValidateImageBuffer(JpegImage image, int bytesPerPixel)
    {
        if (image.Width < 0 || image.Height < 0)
        {
            throw JpegInvalidDataException.Create("Cannot auto-orient an image with negative dimensions.");
        }

        if (image.Pixels == null)
        {
            throw new System.ArgumentNullException("Pixels");
        }

        long expected = (long)image.Width * (long)image.Height * (long)bytesPerPixel;
        if (expected > 2147483647L || image.Pixels.Length != expected)
        {
            throw JpegInvalidDataException.Create("Image pixel buffer length does not match dimensions.");
        }
    }

    internal void ValidateOrientation(int orientation)
    {
        if (orientation < 1 || orientation > 8)
        {
            throw JpegInvalidDataException.Create("Exif orientation must be between 1 and 8.");
        }
    }

    internal int GetOutputWidth(JpegImage image, int orientation)
    {
        if (OrientationSwapsDimensions(orientation))
        {
            return image.Height;
        }

        return image.Width;
    }

    internal int GetOutputHeight(JpegImage image, int orientation)
    {
        if (OrientationSwapsDimensions(orientation))
        {
            return image.Width;
        }

        return image.Height;
    }

    internal bool OrientationSwapsDimensions(int orientation)
    {
        return orientation == 5 || orientation == 6 || orientation == 7 || orientation == 8;
    }

    internal int GetSourceX(int width, int height, int x, int y, int orientation)
    {
        if (orientation == 2)
        {
            return width - 1 - x;
        }

        if (orientation == 3)
        {
            return width - 1 - x;
        }

        if (orientation == 4)
        {
            return x;
        }

        if (orientation == 5)
        {
            return y;
        }

        if (orientation == 6)
        {
            return y;
        }

        if (orientation == 7)
        {
            return width - 1 - y;
        }

        if (orientation == 8)
        {
            return width - 1 - y;
        }

        return x;
    }

    internal int GetSourceY(int width, int height, int x, int y, int orientation)
    {
        if (orientation == 2)
        {
            return y;
        }

        if (orientation == 3)
        {
            return height - 1 - y;
        }

        if (orientation == 4)
        {
            return height - 1 - y;
        }

        if (orientation == 5)
        {
            return x;
        }

        if (orientation == 6)
        {
            return height - 1 - x;
        }

        if (orientation == 7)
        {
            return height - 1 - x;
        }

        if (orientation == 8)
        {
            return x;
        }

        return y;
    }

    internal void CopyPixel(byte[] source, byte[] destination, int sourceWidth, int destinationWidth, int bytesPerPixel, int sourceX, int sourceY, int destinationX, int destinationY)
    {
        int sourceOffset = (sourceY * sourceWidth + sourceX) * bytesPerPixel;
        int destinationOffset = (destinationY * destinationWidth + destinationX) * bytesPerPixel;
        int index = 0;
        while (index < bytesPerPixel)
        {
            destination[destinationOffset + index] = source[sourceOffset + index];
            index = index + 1;
        }
    }

    internal JpegImage CopyImageWithPixels(JpegImage image, int width, int height, byte[] pixels)
    {
        JpegInterleavedPixelBufferAllocator allocator = new JpegInterleavedPixelBufferAllocator();
        JpegImage result = allocator.CreateImage(width, height, image.PixelFormat, image.ColorSpace, image.ComponentCount);
        result.Precision = image.Precision;
        result.SourceWidth = image.SourceWidth;
        result.SourceHeight = image.SourceHeight;
        result.Subsampling = image.Subsampling;
        result.Pixels = pixels;
        result.InterleavedBuffer.Precision = image.Precision;
        result.InterleavedBuffer.Pixels = pixels;
        result.AppliedIdctScaleRatio = image.AppliedIdctScaleRatio;
        result.AppliedIdctScaleNumerator = image.AppliedIdctScaleNumerator;
        result.AppliedIdctScaleDenominator = image.AppliedIdctScaleDenominator;
        result.DecodedRegion = image.DecodedRegion;
        result.OutputRectangle = new JpegRectangle();
        result.OutputRectangle.Width = width;
        result.OutputRectangle.Height = height;
        result.FastMcuCropApplied = image.FastMcuCropApplied;
        result.QuantizationTables = image.QuantizationTables;
        result.ComponentQuantizationTableIdentifiers = image.ComponentQuantizationTableIdentifiers;
        result.Metadata = image.Metadata;
        result.Diagnostics = image.Diagnostics;
        result.DiagnosticCollection = image.DiagnosticCollection;
        return result;
    }
}
