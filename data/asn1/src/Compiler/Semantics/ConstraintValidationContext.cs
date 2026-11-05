namespace asn1;

internal class ConstraintValidationContext
{
    internal ResolvedTypeSyntax? GoverningType;
    internal AsnTypeKind ExpectedKind;
    internal int DomainMode;
}
