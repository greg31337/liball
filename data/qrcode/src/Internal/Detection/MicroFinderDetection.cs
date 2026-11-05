namespace qrcode;

internal class MicroFinderDetection
{
    internal FinderCandidate Finder = new FinderCandidate();

    internal QrQuadrilateral SymbolCorners = new QrQuadrilateral();

    internal int Version;

    internal int Dimension;

    internal int QuarterTurnsClockwise;

    internal int ErrorCorrectionIndex;

    internal int Mask;

    internal int FormatCode;

    internal int FormatDistance;

    internal double TimingConfidence;

    internal double FormatConfidence;

    internal double ExtentConfidence;

    internal double Confidence;

    internal QrPolarity Polarity = QrPolarity.Automatic;

    internal bool HasMirrorEstimate;

    internal bool IsMirrored;
}
