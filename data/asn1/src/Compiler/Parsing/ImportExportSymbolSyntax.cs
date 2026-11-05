namespace asn1;

internal class ImportExportSymbolSyntax
{
    internal string Name = "";
    internal AsnAssignmentKind[] CandidateKinds = [];
    internal bool Parameterized;
    internal AsnSourceSpan? Span;
}
