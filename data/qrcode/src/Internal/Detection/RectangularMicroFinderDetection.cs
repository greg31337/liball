namespace qrcode;

internal class RectangularMicroFinderDetection
{
    internal FinderCandidate Finder = new FinderCandidate();

    internal QrPoint SubfinderCenter = new QrPoint();

    internal QrQuadrilateral SymbolCorners = new QrQuadrilateral();

    internal int VersionIndex;

    internal int Width;

    internal int Height;

    internal int QuarterTurnsClockwise;

    internal int ErrorCorrectionIndex;

    internal int FinderFormatCode;

    internal int SubfinderFormatCode;

    internal int FinderFormatDistance;

    internal int SubfinderFormatDistance;

    internal bool FormatCopiesAgree;

    internal double SubfinderConfidence;

    internal double FormatConfidence;

    internal double Confidence;

    internal QrPolarity Polarity = QrPolarity.Automatic;

    internal bool HasMirrorEstimate;

    internal bool IsMirrored;
}
