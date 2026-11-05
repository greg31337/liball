namespace unicode.Tools.UnicodeDataGenerator;

public sealed class UnicodePropertyAliasRecord
{
    public string ShortName;
    public string LongName;
    public string[] Aliases;

    public UnicodePropertyAliasRecord(string shortName, string longName, string[] aliases)
    {
        ShortName = shortName;
        LongName = longName;
        Aliases = aliases;
    }
}
