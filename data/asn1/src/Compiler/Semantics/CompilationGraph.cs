namespace asn1;

internal class CompilationGraph
{
    internal ModuleDependencyNode[] Modules = [];
    internal ModuleDependencyEdge[] ModuleEdges = [];
    internal ModuleDependencyGroup[] ModuleGroups = [];
    internal AssignmentDependencyNode[] Assignments = [];
    internal AssignmentDependencyEdge[] AssignmentEdges = [];
    internal AssignmentDependencyGroup[] AssignmentGroups = [];
    internal bool Success = true;
}
