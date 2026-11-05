namespace svg;

/// <summary>Stores an optional offset split from a timing specifier.</summary>
internal class SvgTimingOffsetSplit
{
    /// <summary>The exclusive core ending offset.</summary>
    public int CoreEnd;
    /// <summary>The parsed offset seconds.</summary>
    public double OffsetSeconds;

    /// <summary>Creates an offset split.</summary>
    /// <param name="coreEnd">The core ending offset.</param>
    /// <param name="offsetSeconds">The offset seconds.</param>
    internal SvgTimingOffsetSplit(int coreEnd, double offsetSeconds)
    {
        CoreEnd = coreEnd;
        OffsetSeconds = offsetSeconds;
    }
}
