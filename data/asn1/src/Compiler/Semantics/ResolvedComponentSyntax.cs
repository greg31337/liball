namespace asn1;

internal class ResolvedComponentSyntax
{
    internal AsnComponent? Component;
    internal ResolvedTypeSyntax? TypeResolution;
    internal ComponentProvenanceSyntax[] Provenance = [];
    internal ResolvedTagSyntax[] TagChain = [];
    internal ResolvedTagSyntax? AutomaticTag;
    internal ResolvedValueSyntax? DefaultValue;
    internal AsnEncodingInstruction[] PerTargetedEncodingInstructions = [];
    internal AsnEncodingInstruction[] FinalPerEncodingInstructions = [];
    internal AsnEncodingPlanStep[] PerInstructionPlanSteps = [];
    internal int PerInstructionResolutionState;
}
