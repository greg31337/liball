namespace asn1;

internal class EcnPlanValidationIssue
{
    internal int Code;
    internal string Message = "";
    internal string StepId = "";
    internal string Path = "";
    internal AsnSourceSpan? Span;
}
