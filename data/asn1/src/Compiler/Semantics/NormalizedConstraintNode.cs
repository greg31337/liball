namespace asn1;

internal class NormalizedConstraintNode
{
    internal AsnConstraintKind Kind;
    internal ConstraintDomainIdentity? Domain;
    internal ResolvedTypeSyntax? GoverningType;
    internal AsnConstraintElement? SourceElement;
    internal ParsedSubtypeElementSyntax? SourceSyntax;
    internal ParsedNamedConstraintSyntax? SourceNamedConstraint;
    internal ParsedGeneralConstraintSyntax? SourceGeneralConstraint;
    internal IntegerConstraintSet? IntegerSet;
    internal IntegerConstraintSet? SizeSet;
    internal IntegerConstraintSet? SizeRootSet;
    internal IntegerConstraintSet? SizeExtensionSet;
    internal IntegerConstraintSet? AlphabetSet;
    internal IntegerConstraintSet? AlphabetRootSet;
    internal IntegerConstraintSet? AlphabetExtensionSet;
    internal PatternProgram? CompiledPattern;
    internal TimePropertySettings? CompiledTimeSettings;
    internal NormalizedComponentConstraintSet? CompiledComponentConstraints;
    internal NormalizedTypeConstraint? CompiledTypeConstraint;
    internal InnerCodecMetadata? CompiledContentsConstraint;
    internal TableConstraintRelationship? CompiledTableConstraint;
    internal UserDefinedConstraintMetadata? CompiledUserDefinedConstraint;
    internal ResolvedTypeSyntax? ResolvedContainedSubtype;
    internal AsnValue? Value;
    internal AsnValue? LowerValue;
    internal AsnValue? UpperValue;
    internal AsnType? Type;
    internal string Name = "";
    internal string Text = "";
    internal System.Numerics.BigInteger LowerBound;
    internal System.Numerics.BigInteger UpperBound;
    internal NormalizedConstraintNode[] Children = [];
    internal NormalizedConstraintGraph[] NestedConstraints = [];
    internal bool HasLowerBound;
    internal bool HasUpperBound;
    internal bool LowerEndpointUnbounded;
    internal bool UpperEndpointUnbounded;
    internal bool LowerEndpointIncluded;
    internal bool UpperEndpointIncluded;
    internal bool OpenSizeSet;
    internal bool OpenAlphabetSet;
}
