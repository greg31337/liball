namespace qrcode;

internal class TimingPatternRefinementResult
{
    internal bool Success;

    internal bool UsedFallback;

    internal bool DimensionChanged;

    internal int OriginalVersion;

    internal int OriginalDimension;

    internal int RefinedVersion;

    internal int RefinedDimension;

    internal int VersionsEvaluated;

    internal int HorizontalSampleCount;

    internal int VerticalSampleCount;

    internal double HorizontalConfidence;

    internal double VerticalConfidence;

    internal double HorizontalPhaseMargin;

    internal double VerticalPhaseMargin;

    internal double Confidence;

    internal double HorizontalModuleSize;

    internal double VerticalModuleSize;
}
