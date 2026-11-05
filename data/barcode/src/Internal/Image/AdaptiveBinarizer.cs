namespace barcode;

internal class AdaptiveBinarizationData
{
    internal ModuleMatrix Modules = new ModuleMatrix(1, 1);
    internal int[] Thresholds = new int[0];
    internal int TileColumns;
    internal int TileRows;
}

internal static class AdaptiveBinarizer
{
    internal const int DefaultTileSize = 8;
    internal const int MinimumTileContrast = 12;

    internal static AdaptiveBinarizationData BinarizeAdaptive(
        ManagedGrayImage? source,
        int globalThreshold,
        int tileSize,
        BarcodePolarity polarity)
    {
        return BinarizeTiles(
            source,
            globalThreshold,
            tileSize,
            polarity,
            false);
    }

    internal static AdaptiveBinarizationData BinarizeHybrid(
        ManagedGrayImage? source,
        int globalThreshold,
        int tileSize,
        BarcodePolarity polarity)
    {
        return BinarizeTiles(
            source,
            globalThreshold,
            tileSize,
            polarity,
            true);
    }

    static AdaptiveBinarizationData BinarizeTiles(
        ManagedGrayImage? source,
        int globalThreshold,
        int tileSize,
        BarcodePolarity polarity,
        bool hybrid)
    {
        HistogramAnalyzer.ValidateGrayImage(source);
        GlobalBinarizer.ValidatePolarity(polarity);
        if (globalThreshold < 0 || globalThreshold > 255)
        {
            throw new BarcodeValidationException(
                "An adaptive fallback threshold must be between zero and 255.");
        }

        if (tileSize <= 0)
        {
            throw new BarcodeValidationException(
                "An adaptive tile size must be positive.");
        }

        ManagedGrayImage image = source == null
            ? new ManagedGrayImage(1, 1)
            : source;
        int tileColumns = (int)(((long)image.Width
            + tileSize
            - 1L) / tileSize);
        int tileRows = (int)(((long)image.Height
            + tileSize
            - 1L) / tileSize);
        int[] thresholds = new int[CheckedMath.Multiply(
            tileColumns,
            tileRows)];
        ResolveTileThresholds(
            image,
            thresholds,
            tileColumns,
            tileRows,
            tileSize,
            globalThreshold,
            hybrid);
        ModuleMatrix modules = ApplyTileThresholds(
            image,
            thresholds,
            tileColumns,
            tileSize,
            polarity);

        AdaptiveBinarizationData result =
            new AdaptiveBinarizationData();
        result.Modules = modules;
        result.Thresholds = thresholds;
        result.TileColumns = tileColumns;
        result.TileRows = tileRows;
        return result;
    }

    static void ResolveTileThresholds(
        ManagedGrayImage source,
        int[] thresholds,
        int tileColumns,
        int tileRows,
        int tileSize,
        int globalThreshold,
        bool hybrid)
    {
        int tileRow = 0;
        while (tileRow < tileRows)
        {
            int top = tileRow * tileSize;
            int bottom = top + tileSize;
            if (bottom > source.Height)
            {
                bottom = source.Height;
            }

            int tileColumn = 0;
            while (tileColumn < tileColumns)
            {
                int left = tileColumn * tileSize;
                int right = left + tileSize;
                if (right > source.Width)
                {
                    right = source.Width;
                }

                int minimum = 255;
                int maximum = 0;
                int row = top;
                while (row < bottom)
                {
                    int column = left;
                    while (column < right)
                    {
                        int sample = source.ReadSample(column, row);
                        if (sample < minimum)
                        {
                            minimum = sample;
                        }

                        if (sample > maximum)
                        {
                            maximum = sample;
                        }

                        column = column + 1;
                    }

                    row = row + 1;
                }

                int localThreshold = (minimum + maximum) / 2;
                int threshold = localThreshold;
                if (hybrid
                    && maximum - minimum < MinimumTileContrast)
                {
                    threshold = globalThreshold;
                }

                thresholds[(tileRow * tileColumns) + tileColumn] =
                    threshold;
                tileColumn = tileColumn + 1;
            }

            tileRow = tileRow + 1;
        }
    }

    static ModuleMatrix ApplyTileThresholds(
        ManagedGrayImage source,
        int[] thresholds,
        int tileColumns,
        int tileSize,
        BarcodePolarity polarity)
    {
        ModuleMatrix result = new ModuleMatrix(
            source.Width,
            source.Height);
        int row = 0;
        while (row < source.Height)
        {
            int tileRow = row / tileSize;
            int column = 0;
            while (column < source.Width)
            {
                int tileColumn = column / tileSize;
                int threshold = thresholds[
                    (tileRow * tileColumns) + tileColumn];
                int sample = source.ReadSample(column, row);
                bool foreground = polarity == BarcodePolarity.DarkOnLight
                    ? sample <= threshold
                    : sample > threshold;
                result.SetModule(column, row, foreground);
                column = column + 1;
            }

            row = row + 1;
        }

        return result;
    }
}
