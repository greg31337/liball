namespace asn1;

internal class ParameterInstantiationResult
{
    internal AsnAssignmentKind Kind;
    internal AsnToken[] Tokens = [];
    internal SubstitutionTokenOrigin[] Origins = [];
    internal AsnToken[] BodyTokens = [];
    internal SubstitutionTokenOrigin[] BodyOrigins = [];
    internal ParsedParameterizedAssignmentSyntax? AssignmentSyntax;
    internal ParsedParameterizedReferenceSyntax[] NestedReferences = [];
    internal ParameterInstantiationFixup[] DeferredFixups = [];
    internal ParameterInstantiationTrace? Trace;
    internal string CacheKey = "";
    internal string StableId = "";
    internal string FailureMessage = "";
    internal ParameterInstantiationFailureKind FailureKind;
    internal bool CacheHit;
    internal bool Deferred;
    internal bool Success;
}
