namespace unicode.Tools.UnicodeDataGenerator;

public sealed class UnicodeSpecialCasingRecord
{
    public int CodePoint;
    public int[] LowercaseMapping;
    public int[] TitlecaseMapping;
    public int[] UppercaseMapping;
    public string[] Conditions;
    public string Comment;

    public UnicodeSpecialCasingRecord(
        int codePoint,
        int[] lowercaseMapping,
        int[] titlecaseMapping,
        int[] uppercaseMapping,
        string[] conditions,
        string comment)
    {
        ArgumentNullException.ThrowIfNull(lowercaseMapping);
        ArgumentNullException.ThrowIfNull(titlecaseMapping);
        ArgumentNullException.ThrowIfNull(uppercaseMapping);
        ArgumentNullException.ThrowIfNull(conditions);
        ArgumentNullException.ThrowIfNull(comment);

        CodePoint = codePoint;
        LowercaseMapping = lowercaseMapping;
        TitlecaseMapping = titlecaseMapping;
        UppercaseMapping = uppercaseMapping;
        Conditions = conditions;
        Comment = comment;
    }
}
