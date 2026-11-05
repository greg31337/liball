namespace asn1;

/// <summary>
/// Validates runtime values with the shared normalized-constraint evaluator.
/// </summary>
public class AsnRuntimeConstraintEvaluator : IAsnValueValidator
{
    /// <summary>
    /// Initializes the shared runtime constraint evaluator.
    /// </summary>
    public AsnRuntimeConstraintEvaluator()
    {
    }

    /// <summary>
    /// Validates one runtime value against a reflected type.
    /// </summary>
    /// <param name="value">The runtime value.</param>
    /// <param name="type">The reflected governing type.</param>
    /// <returns>Constraint diagnostics in deterministic traversal order.</returns>
    public AsnDiagnostic[] ValidateRuntimeValue(AsnRuntimeValue value, AsnTypeDescriptor type)
    {
        return new RuntimeConstraintEvaluator().ValidateRuntimeValue(
            value,
            type,
            new AsnResourceLimits());
    }
}
