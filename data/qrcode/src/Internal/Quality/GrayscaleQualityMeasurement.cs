namespace qrcode;

internal class GrayscaleQualityMeasurement
{
    internal int SampleCount;
    internal int MinimumSample;
    internal int MaximumSample;
    internal double GlobalThreshold;
    // Packed row-major values, excluding raster padding. Empty when modulation
    // is disabled or undefined because all supplied samples have the same value.
    internal double[] ModuleModulation = new double[0];
    internal QrQualityMetric SymbolContrast = new QrQualityMetric { Name = "SymbolContrast", Unit = "fraction" };
    internal QrQualityMetric Modulation = new QrQualityMetric { Name = "Modulation", Unit = "fraction" };
}
