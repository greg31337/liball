namespace asn1;

internal class EcnPlanDependency
{
    internal EcnPlanDependencyKind Kind;
    internal EcnPlanUnit Unit;
    internal string SourceStepId = "";
    internal string Path = "";
    internal string StableTargetId = "";
    internal long Minimum;
    internal bool MustPrecede;
    internal AsnSourceSpan? Span;
}
