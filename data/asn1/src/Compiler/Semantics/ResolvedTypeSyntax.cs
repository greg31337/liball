namespace asn1;

internal class ResolvedTypeSyntax
{
    internal ParsedTypeSyntax? SourceSyntax;
    internal AsnType? SourceType;
    internal AsnType? EffectiveType;
    internal AsnTypeKind EffectiveKind;
    internal SymbolTableEntry? SourceDeclaration;
    internal SymbolTableEntry? EffectiveDeclaration;
    internal ResolvedTypeSyntax? AliasTarget;
    internal ResolvedTypeSyntax? InnerType;
    internal ResolvedTypeSyntax[] ConstraintTypes = [];
    internal ResolvedComponentSyntax? SelectedComponent;
    internal ResolvedComponentSyntax[] Components = [];
    internal ConstraintDomainIdentity? ConstraintDomain;
    internal NormalizedConstraintGraph[] NormalizedConstraints = [];
    internal NormalizedConstraintGraph? EffectiveConstraint;
    internal IntegerConstraintResult? IntegerConstraints;
    internal SizeConstraintResult? SizeConstraints;
    internal PermittedAlphabetResult? PermittedAlphabet;
    internal PatternProgram[] Patterns = [];
    internal TimePropertySettings[] TimeSettings = [];
    internal NormalizedComponentConstraintSet[] ComponentConstraintSets = [];
    internal NormalizedTypeConstraint[] TypeConstraints = [];
    internal InnerCodecMetadata[] ContentsConstraints = [];
    internal InnerCodecMetadata? EffectiveInnerCodec;
    internal ResolvedObjectClassFieldSyntax? ObjectClassField;
    internal TableConstraintRelationship[] TableConstraints = [];
    internal UserDefinedConstraintMetadata[] UserDefinedConstraints = [];
    internal ConstraintExceptionMetadata[] ConstraintExceptions = [];
    internal ResolvedTagSyntax[] TagChain = [];
    internal int TagResolutionState;
    internal int AutomaticTaggingState;
    internal bool TagResolutionSuccess;
    internal bool AutomaticTaggingApplied;
    internal bool AutomaticTaggingSuppressed;
    internal bool AutomaticTaggingSuccess;
    internal bool ParameterReference;
    internal bool RecursiveReference;
    internal bool RequiresApplicationValidation;
    internal EncodingEffectiveConstraints EncodingConstraints =
        new EncodingEffectiveConstraints();
    internal AsnEncodingInstruction[] TargetedEncodingInstructions = [];
    internal AsnEncodingInstruction[] FinalEncodingInstructions = [];
    internal int XerInstructionResolutionState;
    internal bool XerModifiedEncodings;
    internal AsnEncodingInstruction[] PerTargetedEncodingInstructions = [];
    internal AsnEncodingInstruction[] FinalPerEncodingInstructions = [];
    internal AsnEncodingPlanStep[] PerInstructionPlanSteps = [];
    internal int PerInstructionResolutionState;
    internal bool EffectiveConstraintEmpty;
    internal bool ConstraintValidationSuccess = true;
    internal bool Success;
}
