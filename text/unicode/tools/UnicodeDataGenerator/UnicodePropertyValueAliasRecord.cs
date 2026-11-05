namespace unicode.Tools.UnicodeDataGenerator;

public sealed class UnicodePropertyValueAliasRecord
{
    public string PropertyName;
    public string ValueName;
    public string[] Aliases;

    public UnicodePropertyValueAliasRecord(string propertyName, string valueName, string[] aliases)
    {
        PropertyName = propertyName;
        ValueName = valueName;
        Aliases = aliases;
    }
}
