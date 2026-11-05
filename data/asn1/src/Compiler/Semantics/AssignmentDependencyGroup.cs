namespace asn1;

internal class AssignmentDependencyGroup
{
    internal AssignmentDependencyNode[] Nodes = [];
    internal int CompilationOrder;
    internal bool Recursive;
    internal bool LegalTypeRecursion;
}
