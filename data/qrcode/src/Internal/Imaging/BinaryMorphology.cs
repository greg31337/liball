namespace qrcode;

internal class BinaryMorphology
{
    internal static QrRaster Clean(
        QrRaster raster,
        QrDetectionOptions options)
    {
        int pixelCount = ValidateRaster(raster, options);
        CheckedDimensions.CalculateAllocationBytes(
            pixelCount,
            1,
            QrLimits.DefaultMaximumAllocationBytes);
        QrRaster current = CopyRaster(raster, pixelCount);
        if (options.EnableDenoise)
        {
            current = RemoveIsolatedPixels(current, pixelCount);
        }

        if (options.EnableGapClosing)
        {
            current = CloseSinglePixelGaps(current, pixelCount);
        }

        return current;
    }

    internal static int ValidateRaster(QrRaster raster, QrDetectionOptions options)
    {
        if (raster == null)
        {
            throw new QrInputException("The binary raster is required.", "raster");
        }

        if (options == null)
        {
            throw new QrInputException("The detection options are required.", "options");
        }

        if (raster.Pixels == null)
        {
            throw new QrInputException("The binary pixel buffer is required.", "Pixels");
        }

        CheckedDimensions.ValidateDimensionBounds(
            raster.Width,
            raster.Height,
            options.MaximumImageWidth,
            options.MaximumImageHeight,
            options.MaximumImagePixels);
        int minimumStride = CheckedDimensions.CalculateMinimumStride(raster.Width, 1);
        CheckedDimensions.ValidateStride(raster.Stride, minimumStride);
        int requiredLength = CheckedDimensions.CalculateRequiredBufferLength(
            raster.Height,
            raster.Stride,
            minimumStride);
        CheckedDimensions.ValidateBufferLength(requiredLength, raster.Pixels.Length);
        for (int row = 0; row < raster.Height; row++)
        {
            int rowOffset = row * raster.Stride;
            for (int column = 0; column < raster.Width; column++)
            {
                byte value = raster.Pixels[rowOffset + column];
                if (value != 0 && value != 255)
                {
                    throw new QrInputException(
                        "Binary raster pixels must be either 0 or 255.",
                        "Pixels");
                }
            }
        }

        return CheckedDimensions.CalculateArea(
            raster.Width,
            raster.Height,
            options.MaximumImagePixels);
    }

    static QrRaster CopyRaster(QrRaster source, int pixelCount)
    {
        QrRaster result = new QrRaster
        {
            Pixels = new byte[pixelCount],
            Width = source.Width,
            Height = source.Height,
            Stride = source.Width
        };
        for (int row = 0; row < source.Height; row++)
        {
            int sourceRow = row * source.Stride;
            int destinationRow = row * result.Stride;
            for (int column = 0; column < source.Width; column++)
            {
                result.Pixels[destinationRow + column] = source.Pixels[sourceRow + column];
            }
        }

        return result;
    }

    static QrRaster RemoveIsolatedPixels(QrRaster source, int pixelCount)
    {
        QrRaster result = CopyRaster(source, pixelCount);
        for (int row = 1; row < source.Height - 1; row++)
        {
            for (int column = 1; column < source.Width - 1; column++)
            {
                int index = row * source.Stride + column;
                byte center = source.Pixels[index];
                byte opposite = center == 0 ? (byte)255 : (byte)0;
                bool isolated = true;
                for (int rowOffset = -1; rowOffset <= 1; rowOffset++)
                {
                    for (int columnOffset = -1; columnOffset <= 1; columnOffset++)
                    {
                        if (rowOffset == 0 && columnOffset == 0)
                        {
                            continue;
                        }

                        int neighborIndex = (row + rowOffset) * source.Stride
                            + column
                            + columnOffset;
                        if (source.Pixels[neighborIndex] != opposite)
                        {
                            isolated = false;
                        }
                    }
                }

                if (isolated)
                {
                    result.Pixels[row * result.Stride + column] = opposite;
                }
            }
        }

        return result;
    }

    static QrRaster CloseSinglePixelGaps(QrRaster source, int pixelCount)
    {
        QrRaster result = CopyRaster(source, pixelCount);
        for (int row = 1; row < source.Height - 1; row++)
        {
            for (int column = 1; column < source.Width - 1; column++)
            {
                int index = row * source.Stride + column;
                if (source.Pixels[index] == 0)
                {
                    continue;
                }

                bool horizontalGap = source.Pixels[index - 1] == 0
                    && source.Pixels[index + 1] == 0;
                bool verticalGap = source.Pixels[index - source.Stride] == 0
                    && source.Pixels[index + source.Stride] == 0;
                if (horizontalGap || verticalGap)
                {
                    result.Pixels[row * result.Stride + column] = 0;
                }
            }
        }

        return result;
    }
}
