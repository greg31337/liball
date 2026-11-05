namespace asn1;

internal class EvaluatedObjectSetMember
{
    internal AsnInformationObject Object = new AsnInformationObject();
    internal ParsedInformationObjectSyntax? ObjectSyntax;
    internal string StableId = "";
    internal AsnSourceSpan? Span;
    internal bool Extension;
}
