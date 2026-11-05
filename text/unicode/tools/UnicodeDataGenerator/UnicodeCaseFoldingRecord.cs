namespace unicode.Tools.UnicodeDataGenerator;

public sealed class UnicodeCaseFoldingRecord
{
    public int CodePoint;
    public string Status;
    public int[] Mapping;
    public string Comment;

    public UnicodeCaseFoldingRecord(int codePoint, string status, int[] mapping, string comment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(status);
        ArgumentNullException.ThrowIfNull(mapping);
        ArgumentNullException.ThrowIfNull(comment);

        CodePoint = codePoint;
        Status = status;
        Mapping = mapping;
        Comment = comment;
    }
}
