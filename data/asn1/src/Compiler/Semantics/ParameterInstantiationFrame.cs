namespace asn1;

internal class ParameterInstantiationFrame
{
    internal string Key = "";
    internal string TargetIdentity = "";
    internal ParsedParameterizedReferenceSyntax? Reference;
    internal SymbolTableEntry? Target;
    internal SymbolTableEntry? Declaration;
}
