namespace asn1;

internal class ClassifiedReference
{
    internal AssignmentDependencyEdge? Dependency;
    internal AsnAssignmentKind[] ExpectedKinds = [];
    internal AsnAssignmentKind ActualKind;
    internal bool Success;
}
