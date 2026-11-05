namespace languagecodes;

internal class NameRow
{
    internal string EntityKey;
    internal string Text;
    internal string NameLanguage;
    internal LanguageNameKind Kind;
    internal ReconciledSource Source;
    internal bool IsReferenceName;

    internal NameRow(
        string entityKey,
        string text,
        string nameLanguage,
        LanguageNameKind kind,
        ReconciledSource source,
        bool isReferenceName)
    {
        EntityKey = entityKey;
        Text = text;
        NameLanguage = nameLanguage;
        Kind = kind;
        Source = source;
        IsReferenceName = isReferenceName;
    }
}
