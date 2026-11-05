namespace ttf;

/// <summary>Describes merged Unicode coverage without eagerly expanding code points.</summary>
public class FontCoverage : IFontCoverage
{
    FontCoverageRange[] ranges;
    bool symbolMapping;
    long coveredValueCount;

    internal FontCoverage(FontCoverageRange[] ranges, bool symbolMapping)
    {
        this.ranges = ranges;
        this.symbolMapping = symbolMapping;
        coveredValueCount = 0;
        int index = 0;
        while (index < ranges.Length)
        {
            coveredValueCount += ranges[index].GetEndUnicodeValue() -
                ranges[index].GetStartUnicodeValue() + 1L;
            index += 1;
        }
    }

    /// <summary>Gets the number of disjoint coverage ranges.</summary>
    /// <returns>The range count.</returns>
    public int GetRangeCount() { return ranges.Length; }

    /// <summary>Gets one coverage range.</summary>
    /// <param name="index">The zero-based range index.</param>
    /// <returns>The range.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>index</c> is outside the range collection.
    /// </exception>
    public FontCoverageRange GetRange(int index)
    {
        if (index < 0 || index >= ranges.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
        return ranges[index];
    }

    /// <summary>Gets the total number of scalar values represented by the ranges.</summary>
    /// <returns>The covered value count.</returns>
    public long GetCoveredValueCount() { return coveredValueCount; }

    /// <summary>Reports whether the selected cmap uses Windows symbol semantics.</summary>
    /// <returns>True for a symbol mapping.</returns>
    public bool IsSymbolMapping() { return symbolMapping; }

    /// <summary>Reports whether a Unicode scalar value is covered.</summary>
    /// <param name="unicodeValue">The value to query.</param>
    /// <returns>True when the value occurs in a coverage range.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>unicodeValue</c> is not a Unicode scalar.
    /// </exception>
    public bool Covers(int unicodeValue)
    {
        ValidateUnicodeScalar(unicodeValue);
        int low = 0;
        int high = ranges.Length - 1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            FontCoverageRange range = ranges[middle];
            if (unicodeValue < range.GetStartUnicodeValue()) { high = middle - 1; }
            else if (unicodeValue > range.GetEndUnicodeValue()) { low = middle + 1; }
            else { return true; }
        }
        return false;
    }

    static void ValidateUnicodeScalar(int unicodeValue)
    {
        if (unicodeValue < 0 || unicodeValue > 0x10FFFF ||
            unicodeValue >= 0xD800 && unicodeValue <= 0xDFFF)
        {
            throw new System.ArgumentOutOfRangeException("unicodeValue");
        }
    }
}
