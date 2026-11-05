namespace barcode;

internal class ImageBinarizationResult
{
    internal ModuleMatrix Modules = new ModuleMatrix(1, 1);
    internal BarcodeBinarizationMode Mode;
    internal BarcodePolarity Polarity;
    internal int GlobalThreshold;
    internal int[] RowThresholds = new int[0];
    internal int[] TileThresholds = new int[0];
    internal int TileColumns;
    internal int TileRows;
    internal ImageHistogramAnalysis Analysis = new ImageHistogramAnalysis();
}

internal static class ImageBinarizer
{
    internal const int LowContrastBoundary = 24;
    internal const int UnevenContrastBoundary = 96;

    internal static ImageBinarizationResult Binarize(
        ManagedGrayImage? source,
        BarcodeBinarizationMode mode,
        BarcodePolarity polarity)
    {
        ValidateMode(mode);
        ValidateRequestedPolarity(polarity);
        ImageHistogramAnalysis analysis =
            HistogramAnalyzer.Analyze(source);
        ManagedGrayImage image = source == null
            ? new ManagedGrayImage(1, 1)
            : source;
        BarcodeBinarizationMode selectedMode = mode
            == BarcodeBinarizationMode.Automatic
            ? SelectAutomaticMode(image, analysis)
            : mode;
        BarcodePolarity selectedPolarity = polarity
            == BarcodePolarity.Automatic
            ? analysis.EstimatedPolarity
            : polarity;
        ImageBinarizationResult result = new ImageBinarizationResult();
        result.Mode = selectedMode;
        result.Polarity = selectedPolarity;
        result.Analysis = analysis;
        result.GlobalThreshold = selectedMode
            == BarcodeBinarizationMode.Global
            ? analysis.GlobalThreshold
            : analysis.OtsuThreshold;
        if (analysis.Contrast == 0)
        {
            result.Modules = new ModuleMatrix(
                image.Width,
                image.Height);
            return result;
        }

        switch (selectedMode)
        {
            case BarcodeBinarizationMode.Global:
                result.Modules = GlobalBinarizer.BinarizeThreshold(
                    image,
                    result.GlobalThreshold,
                    selectedPolarity);
                break;
            case BarcodeBinarizationMode.Otsu:
                result.Modules = OtsuBinarizer.BinarizeOtsu(
                    image,
                    selectedPolarity);
                break;
            case BarcodeBinarizationMode.Row:
                PopulateRowResult(
                    result,
                    RowBinarizer.BinarizeRows(
                        image,
                        result.GlobalThreshold,
                        selectedPolarity));
                break;
            case BarcodeBinarizationMode.Adaptive:
                PopulateAdaptiveResult(
                    result,
                    AdaptiveBinarizer.BinarizeAdaptive(
                        image,
                        result.GlobalThreshold,
                        AdaptiveBinarizer.DefaultTileSize,
                        selectedPolarity));
                break;
            case BarcodeBinarizationMode.Hybrid:
                PopulateAdaptiveResult(
                    result,
                    AdaptiveBinarizer.BinarizeHybrid(
                        image,
                        result.GlobalThreshold,
                        AdaptiveBinarizer.DefaultTileSize,
                        selectedPolarity));
                break;
            default:
                throw new BarcodeValidationException(
                    "The selected binarization mode is invalid.");
        }

        return result;
    }

    static BarcodeBinarizationMode SelectAutomaticMode(
        ManagedGrayImage source,
        ImageHistogramAnalysis analysis)
    {
        if (analysis.Contrast < LowContrastBoundary)
        {
            return BarcodeBinarizationMode.Adaptive;
        }

        if (source.Width >= AdaptiveBinarizer.DefaultTileSize * 2
            && source.Height >= AdaptiveBinarizer.DefaultTileSize * 2
            && analysis.Contrast < UnevenContrastBoundary)
        {
            return BarcodeBinarizationMode.Hybrid;
        }

        return BarcodeBinarizationMode.Otsu;
    }

    static void PopulateRowResult(
        ImageBinarizationResult result,
        RowBinarizationData data)
    {
        result.Modules = data.Modules;
        result.RowThresholds = data.Thresholds;
    }

    static void PopulateAdaptiveResult(
        ImageBinarizationResult result,
        AdaptiveBinarizationData data)
    {
        result.Modules = data.Modules;
        result.TileThresholds = data.Thresholds;
        result.TileColumns = data.TileColumns;
        result.TileRows = data.TileRows;
    }

    static void ValidateMode(BarcodeBinarizationMode mode)
    {
        int value = (int)mode;
        if (value < (int)BarcodeBinarizationMode.Global
            || value > (int)BarcodeBinarizationMode.Automatic)
        {
            throw new BarcodeValidationException(
                "The requested binarization mode is invalid.");
        }
    }

    static void ValidateRequestedPolarity(BarcodePolarity polarity)
    {
        int value = (int)polarity;
        if (value < (int)BarcodePolarity.DarkOnLight
            || value > (int)BarcodePolarity.Automatic)
        {
            throw new BarcodeValidationException(
                "The requested barcode polarity is invalid.");
        }
    }
}
