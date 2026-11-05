namespace unicode.Tools.UnicodeDataGenerator;

public sealed class UnicodeCompositionExclusionRecord
{
    public int StartCodePoint;
    public int EndCodePoint;

    public UnicodeCompositionExclusionRecord(int startCodePoint, int endCodePoint)
    {
        if (endCodePoint < startCodePoint)
        {
            throw new ArgumentException("Range end must be greater than or equal to range start.");
        }

        StartCodePoint = startCodePoint;
        EndCodePoint = endCodePoint;
    }

    public bool Contains(int codePoint)
    {
        if (codePoint < StartCodePoint)
        {
            return false;
        }

        return codePoint <= EndCodePoint;
    }
}
