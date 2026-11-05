namespace asn1;

/// <summary>
/// Exposes runtime-value validation through a composed validator implementation.
/// </summary>
public class AsnValueValidator : IAsnValueValidator
{
    internal IAsnValueValidator Implementation;

    /// <summary>
    /// Initializes a value-validator facade over an operation implementation.
    /// </summary>
    /// <param name="implementation">The implementation that validates runtime values.</param>
    public AsnValueValidator(IAsnValueValidator implementation)
    {
        this.Implementation = implementation == null ? throw new System.ArgumentNullException("implementation") : implementation;
    }

    /// <summary>
    /// Validates one runtime value against a reflected type.
    /// </summary>
    /// <param name="value">The runtime value.</param>
    /// <param name="type">The reflected governing type.</param>
    /// <returns>The validation diagnostics.</returns>
    public AsnDiagnostic[] ValidateRuntimeValue(AsnRuntimeValue value, AsnTypeDescriptor type)
    {
        return this.Implementation.ValidateRuntimeValue(value, type);
    }
}
