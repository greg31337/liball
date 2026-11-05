namespace qrcode;

internal class AlignmentPatternSearchResult
{
    internal bool Success;

    internal AlignmentPatternDetection[] Patterns = new AlignmentPatternDetection[0];

    internal int ExpectedCount;

    internal int FoundCount;

    internal bool UsedFallback;

    internal double Confidence;

    internal QrPoint RefinedBottomRight = new QrPoint();
}
