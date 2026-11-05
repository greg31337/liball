namespace asn1;

internal class EcnAssignmentSyntax
{
    internal EcnAssignmentKind Kind;
    internal string Reference = "";
    internal string Governor = "";
    internal EcnParameterSyntax[] Parameters = [];
    internal AsnToken[] HeadTokens = [];
    internal AsnToken[] BodyTokens = [];
    internal EcnClauseSyntax[] Clauses = [];
    internal EcnSymbol? Symbol;
    internal AsnSourceSpan? Span;
}
