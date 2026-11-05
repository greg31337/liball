namespace asn1;

internal class ParsedParameterSyntax
{
    internal AsnParameter Parameter = new AsnParameter();
    internal AsnParameterKind[] CandidateKinds = [];
    internal ParsedTypeSyntax? GovernorTypeSyntax;
    internal ParsedObjectClassSyntax? GovernorObjectClassSyntax;
    internal ParsedParameterSyntax? DummyGovernor;
    internal string DummyGovernorName = "";
    internal AsnToken[] ReferenceTokens = [];
    internal AsnToken[] GovernorTokens = [];
    internal AsnToken[] SourceTokens = [];
    internal AsnSourceSpan? Span;
    internal bool BindingFailed;
}
