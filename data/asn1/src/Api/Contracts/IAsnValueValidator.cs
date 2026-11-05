namespace asn1;

/// <summary>
/// Validates normalized runtime values against reflected types.
/// </summary>
public interface IAsnValueValidator
{
    /// <summary>
    /// Validates one runtime value and returns all diagnostics.
    /// </summary>
    /// <param name="value">The runtime value.</param>
    /// <param name="type">The reflected governing type.</param>
    /// <returns>The validation diagnostics.</returns>
    public AsnDiagnostic[] ValidateRuntimeValue(AsnRuntimeValue value, AsnTypeDescriptor type);
}
