namespace asn1;

internal class ImportedModuleSyntax
{
    internal string ModuleName = "";
    internal ImportExportSymbolSyntax[] Symbols = [];
    internal AssignedModuleIdentifierSyntax? AssignedIdentifier;
    internal ImportResolution? Resolution;
    internal AsnSourceSpan? Span;
}
