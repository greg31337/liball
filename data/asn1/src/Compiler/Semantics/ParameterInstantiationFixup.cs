namespace asn1;

internal class ParameterInstantiationFixup
{
    internal ParsedParameterizedReferenceSyntax? Reference;
    internal SymbolTableEntry? Target;
    internal AsnToken? Token;
    internal AsnSourceSpan? Span;
    internal string ReplacementName = "";
    internal string TargetStableId = "";
    internal bool OptionalBoundary;
    internal bool ChoiceBoundary;
}
