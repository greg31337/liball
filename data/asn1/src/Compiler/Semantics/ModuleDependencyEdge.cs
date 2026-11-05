namespace asn1;

internal class ModuleDependencyEdge
{
    internal ModuleDependencyNode? Source;
    internal ModuleDependencyNode? Target;
    internal ImportResolution? Import;
}
