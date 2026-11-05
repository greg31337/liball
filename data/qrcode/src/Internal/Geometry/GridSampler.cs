namespace qrcode;

internal class GridSampler
{
    // Module cells occupy [column, column + 1) by [row, row + 1).
    // The transform maps this grid into an already polarity-normalized binary raster.
    internal static QrMatrix Sample(
        QrRaster raster,
        PerspectiveTransform transform,
        int width,
        int height,
        QrDetectionOptions options)
    {
        BinaryMorphology.ValidateRaster(raster, options);
        if (transform == null)
        {
            throw new QrInputException("The grid transform is required.", "transform");
        }

        // Reject invalid configuration once; individual projective horizons and
        // cropped samples are ordinary missing observations inside the loop.
        transform.Invert();
        CheckedDimensions.ValidateDimensionBounds(
            width,
            height,
            options.MaximumImageWidth,
            options.MaximumImageHeight,
            options.MaximumImagePixels);
        int count = CheckedDimensions.CalculateArea(width, height, options.MaximumImagePixels);
        CheckedDimensions.CalculateAllocationBytes(count, 5, QrLimits.DefaultMaximumAllocationBytes);
        QrModuleValue[] values = new QrModuleValue[count];
        byte[] confidence = new byte[count];
        QrMatrix result = new QrMatrix
        {
            Width = width,
            Height = height,
            Values = values,
            Confidence = confidence
        };
        int sampleCount = options.UseMultiPointSampling ? 5 : 1;
        QrPoint mapped = new QrPoint();
        for (int row = 0; row < height; row++)
        {
            for (int column = 0; column < width; column++)
            {
                int darkCount = 0;
                int lightCount = 0;
                for (int sample = 0; sample < sampleCount; sample++)
                {
                    double x = column + 0.5;
                    double y = row + 0.5;
                    if (sample == 1)
                    {
                        x -= 0.25;
                    }
                    else if (sample == 2)
                    {
                        x += 0.25;
                    }
                    else if (sample == 3)
                    {
                        y -= 0.25;
                    }
                    else if (sample == 4)
                    {
                        y += 0.25;
                    }

                    if (!transform.TryMapPoint(x, y, mapped)
                        || mapped.X < 0.0 || mapped.Y < 0.0
                        || mapped.X >= raster.Width || mapped.Y >= raster.Height)
                    {
                        continue;
                    }

                    // Bounds are checked before the integer conversion. For
                    // nonnegative coordinates truncation selects the containing pixel.
                    int offset = (int)mapped.Y * raster.Stride + (int)mapped.X;
                    if (raster.Pixels[offset] == 0)
                    {
                        darkCount++;
                    }
                    else
                    {
                        lightCount++;
                    }
                }

                int index = row * width + column;
                if (darkCount > lightCount)
                {
                    values[index] = QrModuleValue.Dark;
                }
                else if (lightCount > darkCount)
                {
                    values[index] = QrModuleValue.Light;
                }

                // The vote margin measures agreement and includes missing samples
                // in the denominator. Ties and wholly missing modules stay unknown.
                confidence[index] = (byte)(global::System.Math.Abs(darkCount - lightCount)
                    * 255 / sampleCount);
            }
        }

        return result;
    }
}
