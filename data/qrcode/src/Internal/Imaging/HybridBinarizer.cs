namespace qrcode;

internal class HybridBinarizer
{
    internal const int BlockSize = 8;
    internal const int MinimumLocalDimension = 40;
    internal const int MinimumBlockContrast = 24;
    internal const int NeighborhoodRadius = 2;

    internal static HybridBinarizerResult Binarize(
        QrImage luminance,
        QrDetectionOptions options)
    {
        int pixelCount = GlobalHistogramBinarizer.ValidateImage(luminance, options);
        if (luminance.Width < MinimumLocalDimension
            || luminance.Height < MinimumLocalDimension)
        {
            GlobalHistogramResult global = GlobalHistogramBinarizer.Binarize(
                luminance,
                options);
            return new HybridBinarizerResult
            {
                Thresholds = new int[] { global.Threshold },
                BlockColumns = 1,
                BlockRows = 1,
                UsedGlobalHistogram = true,
                DarkOnLight = global.DarkOnLight,
                LightOnDark = global.LightOnDark
            };
        }

        int blockColumns = (luminance.Width + BlockSize - 1) / BlockSize;
        int blockRows = (luminance.Height + BlockSize - 1) / BlockSize;
        int blockCount = CheckedDimensions.CalculateArea(
            blockColumns,
            blockRows,
            int.MaxValue);
        CheckedDimensions.CalculateAllocationBytes(
            blockCount,
            4,
            QrLimits.DefaultMaximumAllocationBytes);
        int[] blackPoints = CalculateBlackPoints(
            luminance,
            blockColumns,
            blockRows);
        int[] thresholds = SmoothThresholds(
            blackPoints,
            blockColumns,
            blockRows);
        CheckedDimensions.CalculateAllocationBytes(
            pixelCount,
            1,
            QrLimits.DefaultMaximumAllocationBytes);
        QrRaster darkOnLight = CreateRaster(
            luminance,
            thresholds,
            blockColumns,
            pixelCount,
            false);
        QrRaster lightOnDark = CreateRaster(
            luminance,
            thresholds,
            blockColumns,
            pixelCount,
            true);
        return new HybridBinarizerResult
        {
            Thresholds = thresholds,
            BlockColumns = blockColumns,
            BlockRows = blockRows,
            DarkOnLight = darkOnLight,
            LightOnDark = lightOnDark
        };
    }

    static int[] CalculateBlackPoints(
        QrImage luminance,
        int blockColumns,
        int blockRows)
    {
        int[] blackPoints = new int[blockColumns * blockRows];
        for (int blockRow = 0; blockRow < blockRows; blockRow++)
        {
            int sourceRow = blockRow * BlockSize;
            if (sourceRow > luminance.Height - BlockSize)
            {
                sourceRow = luminance.Height - BlockSize;
            }

            for (int blockColumn = 0; blockColumn < blockColumns; blockColumn++)
            {
                int sourceColumn = blockColumn * BlockSize;
                if (sourceColumn > luminance.Width - BlockSize)
                {
                    sourceColumn = luminance.Width - BlockSize;
                }

                int sum = 0;
                int minimum = 255;
                int maximum = 0;
                for (int rowOffset = 0; rowOffset < BlockSize; rowOffset++)
                {
                    int rowIndex = (sourceRow + rowOffset) * luminance.Stride;
                    for (int columnOffset = 0; columnOffset < BlockSize; columnOffset++)
                    {
                        int value = luminance.Pixels[rowIndex + sourceColumn + columnOffset];
                        sum += value;
                        if (value < minimum)
                        {
                            minimum = value;
                        }

                        if (value > maximum)
                        {
                            maximum = value;
                        }
                    }
                }

                int blackPoint = sum / (BlockSize * BlockSize);
                if (maximum - minimum < MinimumBlockContrast)
                {
                    blackPoint = minimum / 2;
                    if (blockRow > 0 && blockColumn > 0)
                    {
                        int top = blackPoints[(blockRow - 1) * blockColumns + blockColumn];
                        int left = blackPoints[blockRow * blockColumns + blockColumn - 1];
                        int topLeft = blackPoints[(blockRow - 1) * blockColumns + blockColumn - 1];
                        int neighbor = (top + left * 2 + topLeft) / 4;
                        if (minimum < neighbor)
                        {
                            blackPoint = neighbor;
                        }
                    }
                }

                blackPoints[blockRow * blockColumns + blockColumn] = blackPoint;
            }
        }

        return blackPoints;
    }

    static int[] SmoothThresholds(
        int[] blackPoints,
        int blockColumns,
        int blockRows)
    {
        int[] thresholds = new int[blackPoints.Length];
        for (int blockRow = 0; blockRow < blockRows; blockRow++)
        {
            int firstRow = blockRow - NeighborhoodRadius;
            if (firstRow < 0)
            {
                firstRow = 0;
            }

            int lastRow = blockRow + NeighborhoodRadius;
            if (lastRow >= blockRows)
            {
                lastRow = blockRows - 1;
            }

            for (int blockColumn = 0; blockColumn < blockColumns; blockColumn++)
            {
                int firstColumn = blockColumn - NeighborhoodRadius;
                if (firstColumn < 0)
                {
                    firstColumn = 0;
                }

                int lastColumn = blockColumn + NeighborhoodRadius;
                if (lastColumn >= blockColumns)
                {
                    lastColumn = blockColumns - 1;
                }

                int sum = 0;
                int count = 0;
                for (int row = firstRow; row <= lastRow; row++)
                {
                    for (int column = firstColumn; column <= lastColumn; column++)
                    {
                        sum += blackPoints[row * blockColumns + column];
                        count++;
                    }
                }

                thresholds[blockRow * blockColumns + blockColumn] = sum / count;
            }
        }

        return thresholds;
    }

    static QrRaster CreateRaster(
        QrImage luminance,
        int[] thresholds,
        int blockColumns,
        int pixelCount,
        bool inverted)
    {
        QrRaster raster = new QrRaster
        {
            Pixels = new byte[pixelCount],
            Width = luminance.Width,
            Height = luminance.Height,
            Stride = luminance.Width
        };
        for (int row = 0; row < luminance.Height; row++)
        {
            int blockRow = row / BlockSize;
            int sourceRow = row * luminance.Stride;
            int destinationRow = row * raster.Stride;
            for (int column = 0; column < luminance.Width; column++)
            {
                int blockColumn = column / BlockSize;
                int threshold = thresholds[blockRow * blockColumns + blockColumn];
                bool dark = luminance.Pixels[sourceRow + column] <= threshold;
                if (inverted)
                {
                    dark = !dark;
                }

                raster.Pixels[destinationRow + column] = dark ? (byte)0 : (byte)255;
            }
        }

        return raster;
    }
}
