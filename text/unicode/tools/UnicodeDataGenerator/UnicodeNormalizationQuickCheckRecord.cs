namespace unicode.Tools.UnicodeDataGenerator;

public sealed class UnicodeNormalizationQuickCheckRecord
{
    public int StartCodePoint;
    public int EndCodePoint;
    public string Form;
    public string QuickCheckValue;

    public UnicodeNormalizationQuickCheckRecord(int startCodePoint, int endCodePoint, string form, string quickCheckValue)
    {
        if (endCodePoint < startCodePoint)
        {
            throw new ArgumentException("Range end must be greater than or equal to range start.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(form);
        ArgumentException.ThrowIfNullOrWhiteSpace(quickCheckValue);

        StartCodePoint = startCodePoint;
        EndCodePoint = endCodePoint;
        Form = form;
        QuickCheckValue = quickCheckValue;
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
