namespace languagecodes;

internal class IanaSubtagRow
{
    internal LanguageSubtagType RecordType;
    internal string Value;
    internal string[] Descriptions;
    internal string AddedDate;
    internal string? DeprecatedDate;
    internal string? PreferredValue;
    internal string[] Prefixes;
    internal string? SuppressScript;
    internal string? Macrolanguage;
    internal string? Scope;
    internal string[] Comments;

    internal IanaSubtagRow(
        LanguageSubtagType recordType,
        string value,
        string[] descriptions,
        string addedDate,
        string? deprecatedDate,
        string? preferredValue,
        string[] prefixes,
        string? suppressScript,
        string? macrolanguage,
        string? scope,
        string[] comments)
    {
        RecordType = recordType;
        Value = value;
        Descriptions = descriptions;
        AddedDate = addedDate;
        DeprecatedDate = deprecatedDate;
        PreferredValue = preferredValue;
        Prefixes = prefixes;
        SuppressScript = suppressScript;
        Macrolanguage = macrolanguage;
        Scope = scope;
        Comments = comments;
    }
}
