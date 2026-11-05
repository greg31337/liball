namespace asn1;

internal class ParameterInstantiationTrace
{
    internal ParsedParameterizedReferenceSyntax? Reference;
    internal SymbolTable? Module;
    internal SymbolTableEntry? Declaration;
    internal AsnSourceSpan? Span;
    internal SubstitutionTokenOrigin[] InputOrigins = [];
}
