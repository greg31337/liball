namespace barcode;

internal static class GlobalBinarizer
{
    internal static ModuleMatrix BinarizeThreshold(
        ManagedGrayImage? source,
        int threshold,
        BarcodePolarity polarity)
    {
        HistogramAnalyzer.ValidateGrayImage(source);
        ValidateThreshold(threshold);
        ValidatePolarity(polarity);
        ManagedGrayImage image = source == null
            ? new ManagedGrayImage(1, 1)
            : source;
        ModuleMatrix result = new ModuleMatrix(
            image.Width,
            image.Height);
        int row = 0;
        while (row < image.Height)
        {
            int column = 0;
            while (column < image.Width)
            {
                int sample = image.ReadSample(column, row);
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

    internal static void ValidatePolarity(BarcodePolarity polarity)
    {
        if (polarity != BarcodePolarity.DarkOnLight
            && polarity != BarcodePolarity.LightOnDark)
        {
            throw new BarcodeValidationException(
                "Binarization requires an explicit barcode polarity.");
        }
    }

    static void ValidateThreshold(int threshold)
    {
        if (threshold < 0 || threshold > 255)
        {
            throw new BarcodeValidationException(
                "A grayscale threshold must be between zero and 255.");
        }
    }
}

internal static class OtsuBinarizer
{
    internal static ModuleMatrix BinarizeOtsu(
        ManagedGrayImage? source,
        BarcodePolarity polarity)
    {
        ImageHistogramAnalysis analysis =
            HistogramAnalyzer.Analyze(source);
        return GlobalBinarizer.BinarizeThreshold(
            source,
            analysis.OtsuThreshold,
            polarity);
    }
}
