namespace asn1;

internal class ParameterSubstitutionResult
{
    internal AsnAssignmentKind Kind;
    internal AsnToken[] Tokens = [];
    internal SubstitutionTokenOrigin[] Origins = [];
    internal ParsedParameterizedAssignmentSyntax? AssignmentSyntax;
    internal string FailureMessage = "";
    internal bool Success;
}
