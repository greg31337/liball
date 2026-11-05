namespace barcode;

internal static class ImageNormalizer
{
    internal const int MaximumNormalizationRadius = 32;
    internal const int MaximumContrastGain = 16;

    internal static ManagedGrayImage NormalizeIllumination(
        ManagedGrayImage? source,
        int radius,
        int contrastGain)
    {
        HistogramAnalyzer.ValidateGrayImage(source);
        if (radius <= 0 || radius > MaximumNormalizationRadius)
        {
            throw new BarcodeValidationException(
                "The illumination-normalization radius is outside the supported range.");
        }

        if (contrastGain <= 0
            || contrastGain > MaximumContrastGain)
        {
            throw new BarcodeValidationException(
                "The illumination-normalization gain is outside the supported range.");
        }

        ManagedGrayImage image = source == null
            ? new ManagedGrayImage(1, 1)
            : source;
        ManagedGrayImage localMean = ImageFilter.BoxSmooth(
            image,
            radius);
        ManagedGrayImage result = new ManagedGrayImage(
            image.Width,
            image.Height);
        int index = 0;
        while (index < image.Samples.Length)
        {
            int difference = image.Samples[index]
                - localMean.Samples[index];
            result.Samples[index] = ImageFilter.ClampSample(
                128 + (difference * contrastGain));
            index = index + 1;
        }

        return result;
    }
}
