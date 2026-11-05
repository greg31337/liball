namespace asn1;

internal class ImportResolution
{
    internal SymbolTable? SourceModule;
    internal ImportedModuleSyntax? Import;
    internal SymbolTable? TargetModule;
    internal ImportedSymbolBinding[] Symbols = [];
    internal bool Success;
    internal bool Ambiguous;
}
