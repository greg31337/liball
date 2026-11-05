namespace iso20022;

internal class XmiPassOneEntry
{
    internal string Identifier = string.Empty;
    internal string TypeName = string.Empty;
    internal RepositoryEntityKind Kind;
    internal QualifiedNameData ElementName = new QualifiedNameData();
    internal SourceLocation Location = new SourceLocation();
    internal int DocumentOrder;
    internal int Depth;
    internal string ParentIdentifier = string.Empty;
    internal string TopLevelPartitionIdentifier = string.Empty;
    internal RepositoryEntityKind TopLevelPartitionKind;
    internal bool IsTopLevelPartition;
    internal bool IsDuplicate;
}
