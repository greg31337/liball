namespace asn1;

internal class ParsedPerEncodingInstructionTargetSyntax
{
    internal PerEncodingInstructionTargetKind Kind;
    internal string Text = "";
    internal string TypeReference = "";
    internal string BuiltInType = "";
    internal string[] ComponentPath = [];
    internal string[] Identifiers = [];
    internal bool AllIdentifiers;
    internal bool Components;
    internal AsnSourceSpan? Span;
}
