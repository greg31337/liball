namespace asn1;

internal class AssignmentDependencyNode
{
    internal SymbolTable? Module;
    internal SymbolTableEntry? Symbol;
    internal int StableOrder;
}
