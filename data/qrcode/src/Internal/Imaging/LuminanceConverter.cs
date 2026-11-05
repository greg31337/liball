namespace qrcode;

internal class LuminanceConverter
{
    internal static QrImage ConvertToGray8(
        QrImage image,
        QrDetectionOptions options)
    {
        if (image == null)
        {
            throw new QrInputException("The source image is required.", "image");
        }

        if (options == null)
        {
            throw new QrInputException("The detection options are required.", "options");
        }

        if (image.Pixels == null)
        {
            throw new QrInputException("The source pixel buffer is required.", "Pixels");
        }

        int bytesPerPixel = GetBytesPerPixel(image.PixelFormat);
        CheckedDimensions.ValidateDimensionBounds(
            image.Width,
            image.Height,
            options.MaximumImageWidth,
            options.MaximumImageHeight,
            options.MaximumImagePixels);
        int minimumStride = CheckedDimensions.CalculateMinimumStride(
            image.Width,
            bytesPerPixel);
        CheckedDimensions.ValidateStride(image.Stride, minimumStride);
        int requiredLength = CheckedDimensions.CalculateRequiredBufferLength(
            image.Height,
            image.Stride,
            minimumStride);
        CheckedDimensions.ValidateBufferLength(requiredLength, image.Pixels.Length);
        int pixelCount = CheckedDimensions.CalculateArea(
            image.Width,
            image.Height,
            options.MaximumImagePixels);
        CheckedDimensions.CalculateAllocationBytes(
            pixelCount,
            1,
            QrLimits.DefaultMaximumAllocationBytes);

        QrImage result = new QrImage
        {
            Pixels = new byte[pixelCount],
            Width = image.Width,
            Height = image.Height,
            Stride = image.Width,
            PixelFormat = QrPixelFormat.Gray8
        };
        for (int row = 0; row < image.Height; row++)
        {
            int sourceRow = row * image.Stride;
            int destinationRow = row * result.Stride;
            for (int column = 0; column < image.Width; column++)
            {
                int sourceIndex = sourceRow + column * bytesPerPixel;
                result.Pixels[destinationRow + column] = ConvertPixel(
                    image.Pixels,
                    sourceIndex,
                    image.PixelFormat);
            }
        }

        return result;
    }

    static int GetBytesPerPixel(QrPixelFormat pixelFormat)
    {
        if (pixelFormat == QrPixelFormat.Gray8)
        {
            return 1;
        }

        if (pixelFormat == QrPixelFormat.Rgb24)
        {
            return 3;
        }

        if (pixelFormat == QrPixelFormat.Rgba32
            || pixelFormat == QrPixelFormat.Bgra32)
        {
            return 4;
        }

        throw new QrInputException("The pixel format is not defined.", "PixelFormat");
    }

    static byte ConvertPixel(
        byte[] pixels,
        int index,
        QrPixelFormat pixelFormat)
    {
        if (pixelFormat == QrPixelFormat.Gray8)
        {
            return pixels[index];
        }

        int red;
        int green = pixels[index + 1];
        int blue;
        if (pixelFormat == QrPixelFormat.Bgra32)
        {
            blue = pixels[index];
            red = pixels[index + 2];
        }
        else
        {
            red = pixels[index];
            blue = pixels[index + 2];
        }

        int luminance = (red * 299 + green * 587 + blue * 114 + 500) / 1000;
        if (pixelFormat == QrPixelFormat.Rgba32
            || pixelFormat == QrPixelFormat.Bgra32)
        {
            int alpha = pixels[index + 3];
            luminance = (luminance * alpha + 255 * (255 - alpha) + 127) / 255;
        }

        return (byte)luminance;
    }
}
