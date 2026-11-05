namespace asn1;

internal class ModuleDependencyGroup
{
    internal ModuleDependencyNode[] Nodes = [];
    internal int CompilationOrder;
    internal bool Recursive;
}
