namespace iso20022;

internal class RepositoryIntegrityDiagnostic
{
    internal string Code = string.Empty;
    internal string Message = string.Empty;
    internal string Identifier = string.Empty;
    internal RepositoryEntityKind Kind;
    internal string RelatedIdentifier = string.Empty;
    internal int Order;
    internal SourceLocation Location = new SourceLocation();
}
