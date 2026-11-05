namespace languagecodes;

internal class RelationshipRow
{
    internal string ParentCode;
    internal string ChildCode;
    internal LanguageRelationshipKind Kind;
    internal ReconciledRelationshipStatus Status;
    internal ReconciledSource Source;

    internal RelationshipRow(
        string parentCode,
        string childCode,
        LanguageRelationshipKind kind,
        ReconciledRelationshipStatus status,
        ReconciledSource source)
    {
        ParentCode = parentCode;
        ChildCode = childCode;
        Kind = kind;
        Status = status;
        Source = source;
    }
}
