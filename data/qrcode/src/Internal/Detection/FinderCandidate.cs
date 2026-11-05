namespace qrcode;

internal class FinderCandidate
{
    internal double CenterX;

    internal double CenterY;

    internal double ModuleSize;

    internal double Confidence;

    internal int ScaleToSource = 1;

    internal bool ScannedVertically;

    internal QrPolarity Polarity = QrPolarity.Automatic;

    internal int EvidenceCount = 1;

    internal int HorizontalEvidenceCount;

    internal int VerticalEvidenceCount;

    internal int MinimumScaleToSource;

    internal int MaximumScaleToSource;

    internal bool MixedPolarityEvidence;

    internal bool RegionValidated;

    internal double RegionConfidence;

    internal int CenterRegionArea;

    internal int OuterRegionArea;

    internal QrQuadrilateral? FinderCorners;
}
