namespace asn1;

/// <summary>
/// Describes one classified operation in an encoding plan.
/// </summary>
public class AsnEncodingPlanStep
{
    /// <summary>
    /// Contains the plan-step category.
    /// </summary>
    public AsnPlanStepKind Kind;

    /// <summary>
    /// Contains the encoding instruction associated with the step when present.
    /// </summary>
    public AsnEncodingInstruction? Instruction;

    /// <summary>
    /// Contains the reflected value or field path operated on by the step.
    /// </summary>
    public string Path = "";

    /// <summary>
    /// Contains nested plan steps in execution order.
    /// </summary>
    public AsnEncodingPlanStep[] Children = [];
}
