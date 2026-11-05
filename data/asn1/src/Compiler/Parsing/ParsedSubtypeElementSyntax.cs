namespace asn1;

internal class ParsedSubtypeElementSyntax
{
    internal AsnConstraintElement Element = new AsnConstraintElement();
    internal ParsedValueSyntax? ValueSyntax;
    internal ParsedValueSyntax? LowerValueSyntax;
    internal ParsedValueSyntax? UpperValueSyntax;
    internal ParsedTypeSyntax? TypeSyntax;
    internal ParsedConstraintSyntax? NestedConstraintSyntax;
    internal ParsedNamedConstraintSyntax[] NamedConstraints = [];
    internal bool LowerOpen;
    internal bool UpperOpen;
    internal bool LowerIsMinimum;
    internal bool UpperIsMaximum;
    internal bool PartialSpecification;
    internal AsnToken[] SourceTokens = [];
    internal AsnSourceSpan? Span;
}
