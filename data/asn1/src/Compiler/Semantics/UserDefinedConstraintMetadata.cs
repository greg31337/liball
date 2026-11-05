namespace asn1;

internal class UserDefinedConstraintMetadata
{
    internal ResolvedTypeSyntax? GoverningType;
    internal NormalizedConstraintNode? SourceNode;
    internal AsnToken[] BodyTokens = [];
    internal string BodyText = "";
    internal UserDefinedConstraintParameter[] Parameters = [];
    internal ParsedParameterSyntax[] AssignmentFormalParameters = [];
    internal ConstraintExceptionMetadata? Exception;
    internal string ValidationRequirement = "";
    internal bool HasExecutableMeaning;
    internal bool RequiresApplicationValidation;
}
