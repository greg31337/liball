namespace qrcode;

internal class ImagePyramid
{
    internal const int MinimumLevelDimension = 7;

    internal static ImagePyramidLevel[] Build(
        QrRaster source,
        QrDetectionOptions options)
    {
        int pixelCount = BinaryMorphology.ValidateRaster(source, options);
        if (options.MaximumPyramidLevels <= 0)
        {
            throw new QrInputException(
                "The maximum pyramid level count must be positive.",
                "MaximumPyramidLevels");
        }

        int levelCount = CountLevels(
            source.Width,
            source.Height,
            options.MaximumPyramidLevels);
        ImagePyramidLevel[] levels = new ImagePyramidLevel[levelCount];
        CheckedDimensions.CalculateAllocationBytes(
            pixelCount,
            1,
            QrLimits.DefaultMaximumAllocationBytes);
        QrRaster firstRaster = CopyRaster(source, pixelCount);
        levels[0] = new ImagePyramidLevel
        {
            Raster = firstRaster
        };
        for (int index = 1; index < levels.Length; index++)
        {
            ImagePyramidLevel previous = levels[index - 1];
            QrRaster reduced = Reduce(previous.Raster);
            int scale = CheckedDimensions.CalculateProduct(
                previous.ScaleToSource,
                2,
                int.MaxValue,
                "PyramidScale");
            levels[index] = new ImagePyramidLevel
            {
                Raster = reduced,
                ScaleToSource = scale
            };
        }

        return levels;
    }

    static int CountLevels(int width, int height, int maximumLevels)
    {
        int count = 1;
        while (count < maximumLevels)
        {
            int nextWidth = width / 2 + width % 2;
            int nextHeight = height / 2 + height % 2;
            if (nextWidth < MinimumLevelDimension
                || nextHeight < MinimumLevelDimension)
            {
                break;
            }

            count++;
            width = nextWidth;
            height = nextHeight;
        }

        return count;
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

    static QrRaster Reduce(QrRaster source)
    {
        int width = source.Width / 2 + source.Width % 2;
        int height = source.Height / 2 + source.Height % 2;
        int pixelCount = CheckedDimensions.CalculateArea(
            width,
            height,
            QrLimits.DefaultMaximumImagePixels);
        CheckedDimensions.CalculateAllocationBytes(
            pixelCount,
            1,
            QrLimits.DefaultMaximumAllocationBytes);
        QrRaster result = new QrRaster
        {
            Pixels = new byte[pixelCount],
            Width = width,
            Height = height,
            Stride = width
        };
        for (int row = 0; row < height; row++)
        {
            int firstSourceRow = row * 2;
            int lastSourceRow = firstSourceRow + 1;
            if (lastSourceRow >= source.Height)
            {
                lastSourceRow = firstSourceRow;
            }

            for (int column = 0; column < width; column++)
            {
                int firstSourceColumn = column * 2;
                int lastSourceColumn = firstSourceColumn + 1;
                if (lastSourceColumn >= source.Width)
                {
                    lastSourceColumn = firstSourceColumn;
                }

                int sampleCount = 0;
                int darkCount = 0;
                for (int sourceRow = firstSourceRow;
                    sourceRow <= lastSourceRow;
                    sourceRow++)
                {
                    for (int sourceColumn = firstSourceColumn;
                        sourceColumn <= lastSourceColumn;
                        sourceColumn++)
                    {
                        sampleCount++;
                        if (source.Pixels[sourceRow * source.Stride + sourceColumn] == 0)
                        {
                            darkCount++;
                        }
                    }
                }

                result.Pixels[row * result.Stride + column] = darkCount * 2 >= sampleCount
                    ? (byte)0
                    : (byte)255;
            }
        }

        return result;
    }
}
