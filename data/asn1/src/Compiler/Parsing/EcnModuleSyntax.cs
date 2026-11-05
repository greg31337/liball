namespace asn1;

internal class EcnModuleSyntax
{
    internal string Name = "";
    internal bool LinkDefinitions;
    internal bool ExportsSpecified;
    internal bool ExportsAll;
    internal string[] Exports = [];
    internal string[] Imports = [];
    internal EcnImportSyntax[] ImportedModules = [];
    internal AsnToken[] RenameTokens = [];
    internal EcnAssignmentSyntax[] Assignments = [];
    internal EcnSymbolTable? SymbolTable;
    internal AsnSourceSpan? Span;
}
