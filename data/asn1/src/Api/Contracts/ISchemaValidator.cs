namespace asn1;

/// <summary>
/// Binds, normalizes, and validates ASN.1 modules.
/// </summary>
public interface ISchemaValidator
{
    /// <summary>
    /// Validates modules supplied in a read-only span.
    /// </summary>
    /// <param name="modules">The parsed modules.</param>
    /// <returns>The validation result.</returns>
    public AsnValidationResult ValidateModules(System.ReadOnlySpan<AsnModule> modules);
}
