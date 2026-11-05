namespace unicode.Tools.UnicodeDataGenerator;

public sealed class UnicodePropertyRangeRecord
{
    public int StartCodePoint;
    public int EndCodePoint;
    public string PropertyName;

    public UnicodePropertyRangeRecord(int startCodePoint, int endCodePoint, string propertyName)
    {
        if (endCodePoint < startCodePoint)
        {
            throw new ArgumentException("Range end must be greater than or equal to range start.");
        }

        StartCodePoint = startCodePoint;
        EndCodePoint = endCodePoint;
        PropertyName = propertyName;
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
