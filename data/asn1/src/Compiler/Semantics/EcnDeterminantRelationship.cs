namespace asn1;

internal class EcnDeterminantRelationship
{
    internal EcnControlDependencyKind ControlKind;
    internal EcnDeterminationMode Mode;
    internal EcnLinkDirection Direction;
    internal EcnFieldPath? ControlledPath;
    internal EcnFieldPath? DeterminantPath;
    internal EcnValueMapping? Mapping;
    internal EcnEncodingLink? EncodingLink;
    internal int Order;
    internal bool Valid;
    internal AsnSourceSpan? Span;
}
