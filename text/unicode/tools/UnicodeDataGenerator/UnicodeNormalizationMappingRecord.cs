namespace unicode.Tools.UnicodeDataGenerator;

public sealed class UnicodeNormalizationMappingRecord
{
    public int StartCodePoint;
    public int EndCodePoint;
    public int[] Mapping;

    public UnicodeNormalizationMappingRecord(int startCodePoint, int endCodePoint, int[] mapping)
    {
        if (endCodePoint < startCodePoint)
        {
            throw new ArgumentException("Range end must be greater than or equal to range start.");
        }

        ArgumentNullException.ThrowIfNull(mapping);

        StartCodePoint = startCodePoint;
        EndCodePoint = endCodePoint;
        Mapping = mapping;
    }
}
