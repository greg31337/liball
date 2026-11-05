namespace uri;

internal class PercentReplacementMeasureResult
{
    internal long Length;
    internal bool UsedReplacement;

    internal PercentReplacementMeasureResult(long length, bool usedReplacement)
    {
        Length = length;
        UsedReplacement = usedReplacement;
    }
}
