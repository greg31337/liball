namespace asn1;

internal class EcnExecutablePlan
{
    internal string StableId = "";
    internal string ModuleName = "";
    internal string ObjectName = "";
    internal string ClassName = "";
    internal EcnExecutablePlanStep? Root;
    internal EcnExecutablePlanStep[] Steps = [];
    internal EcnPlanBounds Bounds = new EcnPlanBounds();
    internal EcnPlanBufferRequirement Buffer =
        new EcnPlanBufferRequirement();
    internal EcnPlanValidationIssue[] ValidationIssues = [];
    internal bool Reversible;
    internal bool Valid;
    internal bool Deterministic;
    internal bool Decodable;
    internal bool Complete;
    internal AsnSourceSpan? Span;
}
