namespace unicode.Tools.UnicodeDataGenerator;

public sealed class UnicodeNameAliasRecord
{
    public int CodePoint;
    public string Name;
    public string AliasType;

    public UnicodeNameAliasRecord(int codePoint, string name, string aliasType)
    {
        CodePoint = codePoint;
        Name = name;
        AliasType = aliasType;
    }
}
