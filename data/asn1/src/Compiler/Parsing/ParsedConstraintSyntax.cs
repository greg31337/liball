namespace asn1;

internal class ParsedConstraintSyntax
{
    internal AsnConstraint Constraint = new AsnConstraint();
    internal AsnConstraintElement? RootElement;
    internal AsnConstraintElement? AdditionalElement;
    internal AsnConstraintElement? ExceptionElement;
    internal ParsedTypeSyntax? ExceptionTypeSyntax;
    internal ParsedValueSyntax? ExceptionValueSyntax;
    internal ParsedSubtypeElementSyntax[] SubtypeElements = [];
    internal ParsedGeneralConstraintSyntax[] GeneralConstraints = [];
    internal AsnToken[] SourceTokens = [];
    internal ParsedRecoverySyntax[] Recoveries = [];
    internal AsnSourceSpan? Span;
}
