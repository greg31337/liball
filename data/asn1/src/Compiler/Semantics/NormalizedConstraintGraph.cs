namespace asn1;

internal class NormalizedConstraintGraph
{
    internal ConstraintDomainIdentity? Domain;
    internal ResolvedTypeSyntax? GoverningType;
    internal NormalizedConstraintPartition? RootPartition;
    internal NormalizedConstraintPartition? ExtensionPartition;
    internal NormalizedConstraintNode? Exception;
    internal ConstraintExceptionMetadata? CompiledException;
    internal ParsedConstraintSyntax? SourceSyntax;
    internal NormalizedConstraintGraph[] Applications = [];
    internal IntegerConstraintSet? KnownIntegerSet;
}
