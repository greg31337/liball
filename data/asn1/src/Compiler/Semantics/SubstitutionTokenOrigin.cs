namespace asn1;

internal class SubstitutionTokenOrigin
{
    internal AsnToken? Token;
    internal AsnToken? SourceToken;
    internal SymbolTable? Module;
    internal SymbolTableEntry? Declaration;
    internal ParsedParameterSyntax? FormalParameter;
    internal ParsedActualParameterSyntax? ActualParameter;
    internal bool Inserted;
}
