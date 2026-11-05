namespace languagecodes;

internal class AliasRow
{
    internal string? EntityKey;
    internal string Value;
    internal LanguageAliasKind Kind;
    internal string? PreferredValue;
    internal ReconciledSource Source;
    internal string SourceRecordIdentifier;
    internal bool IsDeprecated;

    internal AliasRow(
        string? entityKey,
        string value,
        LanguageAliasKind kind,
        string? preferredValue,
        ReconciledSource source,
        string sourceRecordIdentifier,
        bool isDeprecated)
    {
        EntityKey = entityKey;
        Value = value;
        Kind = kind;
        PreferredValue = preferredValue;
        Source = source;
        SourceRecordIdentifier = sourceRecordIdentifier;
        IsDeprecated = isDeprecated;
    }
}
