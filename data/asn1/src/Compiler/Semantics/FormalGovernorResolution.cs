namespace asn1;

internal class FormalGovernorResolution
{
    internal AsnParameterKind Kind;
    internal ParsedTypeSyntax? TypeSyntax;
    internal ParsedObjectClassSyntax? ObjectClassSyntax;
    internal ParsedParameterSyntax? DummyParameter;
    internal bool InvalidDummy;
    internal bool Ambiguous;
}
