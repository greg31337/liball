namespace asn1;

internal class ImportedSymbolBinding
{
    internal ImportExportSymbolSyntax? Import;
    internal SymbolTableEntry? Target;
    internal string QualifiedName = "";
    internal AsnAssignmentKind[] CandidateKinds = [];
    internal AsnAssignmentKind Kind;
}
