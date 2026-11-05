namespace asn1;

/// <summary>
/// Validates and applies one application-provided PER instruction.
/// </summary>
public interface IPerInstructionHandler
{
    /// <summary>
    /// Validates instruction applicability to a type.
    /// </summary>
    /// <param name="instruction">The resolved instruction.</param>
    /// <param name="type">The target type.</param>
    /// <returns>Applicability diagnostics.</returns>
    public AsnDiagnostic[] ValidateInstruction(AsnEncodingInstruction instruction, AsnType type);

    /// <summary>
    /// Applies a validated instruction to an encoding plan.
    /// </summary>
    /// <param name="instruction">The resolved instruction.</param>
    /// <param name="type">The target type.</param>
    /// <param name="plan">The plan being lowered.</param>
    /// <returns>The plan steps contributed by the handler.</returns>
    public AsnEncodingPlanStep[] ApplyInstruction(AsnEncodingInstruction instruction, AsnType type, AsnEncodingPlan plan);
}
