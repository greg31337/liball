namespace asn1;

internal class EcnExecutablePlanStep
{
    internal string StableId = "";
    internal EcnPlanStepKind Kind;
    internal string Name = "";
    internal string Path = "";
    internal string Operation = "";
    internal string Value = "";
    internal EcnExecutablePlanStep[] Children = [];
    internal EcnPlanDependency[] Dependencies = [];
    internal EcnPlanBounds Bounds = new EcnPlanBounds();
    internal EcnPlanBufferRequirement Buffer =
        new EcnPlanBufferRequirement();
    internal EcnPlanSourceTrace Trace = new EcnPlanSourceTrace();
    internal int Order;
    internal bool Encodes = true;
    internal bool Decodes = true;
    internal bool Reversible = true;
    internal bool Valid = true;
    internal bool RequiresRuntimeValidation;
}
