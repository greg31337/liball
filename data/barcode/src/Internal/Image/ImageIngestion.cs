namespace barcode;

internal class ManagedGrayImage
{
    internal int Width;
    internal int Height;
    internal int[] Samples;

    internal ManagedGrayImage(int width, int height)
    {
        Width = CheckedMath.ValidateDimension(width);
        Height = CheckedMath.ValidateDimension(height);
        Samples = new int[CheckedMath.CalculateAllocationLength(
            width,
            height,
            1,
            DecodingContext.DefaultMaximumModules)];
    }

    internal int ReadSample(int column, int row)
    {
        CheckedMath.ValidateIndex(column, Width);
        CheckedMath.ValidateIndex(row, Height);
        return Samples[(row * Width) + column];
    }

    internal void WriteSample(int column, int row, int value)
    {
        CheckedMath.ValidateIndex(column, Width);
        CheckedMath.ValidateIndex(row, Height);
        if (value < 0 || value > 255)
        {
            throw new BarcodeValidationException(
                "Managed grayscale sample must be between zero and 255.");
        }

        Samples[(row * Width) + column] = value;
    }

    internal int[] CopySamples()
    {
        int[] copy = new int[Samples.Length];
        int index = 0;
        while (index < Samples.Length)
        {
            copy[index] = Samples[index];
            index = index + 1;
        }

        return copy;
    }
}

internal static class ImageIngestion
{
    internal static ManagedGrayImage ConvertToGray8(BarcodeImage? image)
    {
        if (image == null || image.Pixels == null)
        {
            throw new BarcodeValidationException(
                "The raw image and its pixel buffer must not be null.");
        }

        ValidateResolution(image.DpiX, "horizontal");
        ValidateResolution(image.DpiY, "vertical");
        CheckedMath.ValidateDimension(image.Width);
        CheckedMath.ValidateDimension(image.Height);
        CheckedMath.CalculateAllocationLength(
            image.Width,
            image.Height,
            1,
            DecodingContext.DefaultMaximumModules);
        if (image.Pixels.Length > DecodingContext.DefaultMaximumBytes)
        {
            throw new BarcodeValidationException(
                "The raw image pixel buffer exceeds the supported size.");
        }

        int bytesPerPixel = ResolveBytesPerPixel(image.PixelFormat);
        int rowBytes = CheckedMath.Multiply(image.Width, bytesPerPixel);
        ValidateImageRows(image, rowBytes);

        ManagedGrayImage output = new ManagedGrayImage(
            image.Width,
            image.Height);
        byte[] pixels = image.Pixels;
        int row = 0;
        while (row < image.Height)
        {
            int rowStart = (int)((long)image.Offset
                + ((long)row * image.Stride));
            int column = 0;
            while (column < image.Width)
            {
                int pixelIndex = rowStart + (column * bytesPerPixel);
                output.WriteSample(
                    column,
                    row,
                    ReadLuminance(
                        pixels,
                        pixelIndex,
                        image.PixelFormat));
                column = column + 1;
            }

            row = row + 1;
        }

        return output;
    }

    static int ResolveBytesPerPixel(BarcodePixelFormat format)
    {
        switch (format)
        {
            case BarcodePixelFormat.Gray8:
                return 1;
            case BarcodePixelFormat.Rgb24:
            case BarcodePixelFormat.Bgr24:
                return 3;
            case BarcodePixelFormat.Rgba32:
            case BarcodePixelFormat.Bgra32:
                return 4;
            default:
                throw new BarcodeValidationException(
                    "The raw image pixel format is invalid.");
        }
    }

    static void ValidateImageRows(BarcodeImage image, int rowBytes)
    {
        long absoluteStride = image.Stride < 0
            ? -(long)image.Stride
            : image.Stride;
        if (image.Height > 1 && absoluteStride < rowBytes)
        {
            throw new BarcodeValidationException(
                "The raw image stride is shorter than a pixel row.");
        }

        byte[]? pixels = image.Pixels;
        if (pixels == null)
        {
            throw new BarcodeValidationException(
                "The raw image pixel buffer must not be null.");
        }

        int row = 0;
        while (row < image.Height)
        {
            long start = (long)image.Offset
                + ((long)row * image.Stride);
            long end = start + rowBytes;
            if (start < 0L || end < start || end > pixels.Length)
            {
                throw new BarcodeValidationException(
                    "The raw image rows exceed the pixel buffer.");
            }

            row = row + 1;
        }
    }

    static void ValidateResolution(double value, string axis)
    {
        if (double.IsNaN(value)
            || double.IsInfinity(value)
            || value < 0.0)
        {
            throw new BarcodeValidationException(
                $"The raw image {axis} resolution must be finite and non-negative.");
        }
    }

    static int ReadLuminance(
        byte[] pixels,
        int index,
        BarcodePixelFormat format)
    {
        if (format == BarcodePixelFormat.Gray8)
        {
            return pixels[index];
        }

        int red;
        int green = pixels[index + 1];
        int blue;
        if (format == BarcodePixelFormat.Rgb24
            || format == BarcodePixelFormat.Rgba32)
        {
            red = pixels[index];
            blue = pixels[index + 2];
        }
        else
        {
            blue = pixels[index];
            red = pixels[index + 2];
        }

        return ((77 * red)
            + (150 * green)
            + (29 * blue)
            + 128) >> 8;
    }
}
