namespace qrcode;

internal class AlignmentPatternDetection
{
    internal int LogicalRow;

    internal int LogicalColumn;

    internal QrPoint PredictedCenter = new QrPoint();

    internal QrPoint Center = new QrPoint();

    internal bool Found;

    internal bool UsedFallback;

    internal int SearchRadius;

    internal double Confidence;
}
