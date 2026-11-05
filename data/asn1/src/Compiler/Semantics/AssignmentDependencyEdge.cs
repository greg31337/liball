namespace asn1;

internal class AssignmentDependencyEdge
{
    internal AssignmentDependencyNode? Source;
    internal AssignmentDependencyNode? Target;
    internal AsnToken? ReferenceToken;
}
