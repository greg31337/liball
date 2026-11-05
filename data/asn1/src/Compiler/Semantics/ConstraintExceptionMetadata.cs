namespace asn1;

internal class ConstraintExceptionMetadata
{
    internal ResolvedTypeSyntax? GoverningType;
    internal ResolvedTypeSyntax? ExceptionType;
    internal ResolvedValueSyntax? ExceptionValue;
    internal ParsedTypeSyntax? SourceTypeSyntax;
    internal ParsedValueSyntax? SourceValueSyntax;
    internal NormalizedConstraintNode? SourceNode;
    internal string IdentifierText = "";
    internal bool UsesImplicitIntegerType;
    internal bool DefinedIdentifier;
}
