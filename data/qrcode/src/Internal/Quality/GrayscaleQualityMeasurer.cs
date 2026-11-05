namespace qrcode;

internal class GrayscaleQualityMeasurer
{
    // Input is the caller's selected grid of aperture/module grayscale samples,
    // not an image to binarize or normalize. Byte values are intensity proxies,
    // not calibrated reflectance. Sampling aperture and region selection belong
    // to the caller or the image-quality pipeline.
    // SC = (maximum - minimum) / 255; GT = (maximum + minimum) / 2.
    // Per-module MOD = 2 * abs(sample - GT) / (maximum - minimum).
    // The minimum is a conservative indicator, not a profile/codeword grade.
    // Formula cross-check: Cognex TruCheck Legacy User Manual, ISO 15415
    // Data Matrix Parameters (https://www.cognex.com/support/downloads/ns/397/401/TruCheck%20Legacy%20User%20Manual.pdf).
    internal static GrayscaleQualityMeasurement Measure(QrRaster samples, QrQualityOptions options)
    {
        int area = ValidateInput(samples, options);
        int minimum = 255;
        int maximum = 0;
        for (int row = 0; row < samples.Height; row++)
        {
            for (int column = 0; column < samples.Width; column++)
            {
                int value = samples.Pixels[row * samples.Stride + column];
                if (value < minimum) { minimum = value; }
                if (value > maximum) { maximum = value; }
            }
        }
        GrayscaleQualityMeasurement result = new GrayscaleQualityMeasurement
        {
            SampleCount = area, MinimumSample = minimum, MaximumSample = maximum,
            GlobalThreshold = (minimum + maximum) / 2.0
        };
        int contrast = maximum - minimum;
        if (options.AnalyzeContrast)
        {
            result.SymbolContrast.Value = contrast / 255.0;
            result.SymbolContrast.IsCalculated = true;
            result.SymbolContrast.Limitation = "Calculated from uncalibrated grayscale intensities, not calibrated reflectance.";
        }
        else { result.SymbolContrast.Limitation = "Symbol contrast measurement is disabled."; }
        if (!options.AnalyzeModulation)
        {
            result.Modulation.Limitation = "Modulation measurement is disabled.";
            return result;
        }
        if (contrast == 0)
        {
            result.Modulation.Limitation = "Modulation is undefined when symbol contrast is zero.";
            return result;
        }
        result.ModuleModulation = new double[area];
        double weakest = 1.0;
        for (int row = 0; row < samples.Height; row++)
        {
            for (int column = 0; column < samples.Width; column++)
            {
                // Integer doubled distance preserves half-integer thresholds.
                int distance = global::System.Math.Abs(2 * samples.Pixels[row * samples.Stride + column] - minimum - maximum);
                double modulation = (double)distance / contrast;
                result.ModuleModulation[row * samples.Width + column] = modulation;
                if (modulation < weakest) { weakest = modulation; }
            }
        }
        result.Modulation.Value = weakest;
        result.Modulation.IsCalculated = true;
        result.Modulation.Limitation = "Minimum supplied-module modulation from uncalibrated grayscale samples; profile and codeword grading are not applied.";
        return result;
    }

    static int ValidateInput(QrRaster samples, QrQualityOptions options)
    {
        if (samples == null || options == null)
        {
            throw new QrInputException("Grayscale samples and quality options are required.", "samples");
        }
        CheckedDimensions.ValidateDimensionBounds(samples.Width, samples.Height,
            QrLimits.DefaultMaximumImageDimension, QrLimits.DefaultMaximumImageDimension, options.MaximumModuleSamples);
        int area = CheckedDimensions.CalculateArea(samples.Width, samples.Height, options.MaximumModuleSamples);
        if (samples.Pixels == null) { throw new QrInputException("The grayscale sample buffer is required.", "Pixels"); }
        CheckedDimensions.ValidateStride(samples.Stride, samples.Width);
        int required = CheckedDimensions.CalculateRequiredBufferLength(samples.Height, samples.Stride, samples.Width);
        CheckedDimensions.ValidateBufferLength(required, samples.Pixels.Length);
        int allocation = options.AnalyzeModulation
            ? CheckedDimensions.CalculateAllocationBytes(area, 8, QrLimits.DefaultMaximumAllocationBytes) : 0;
        CheckedDimensions.CalculateSum(allocation, 512, QrLimits.DefaultMaximumAllocationBytes, "MaximumAllocationBytes");
        return area;
    }
}
