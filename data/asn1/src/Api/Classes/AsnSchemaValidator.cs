namespace asn1;

/// <summary>
/// Exposes schema validation through a composed validator implementation.
/// </summary>
public class AsnSchemaValidator : ISchemaValidator
{
    internal ISchemaValidator Implementation;

    /// <summary>
    /// Initializes a schema-validator facade over an operation implementation.
    /// </summary>
    /// <param name="implementation">The implementation that validates schemas.</param>
    public AsnSchemaValidator(ISchemaValidator implementation)
    {
        this.Implementation = implementation == null ? throw new System.ArgumentNullException("implementation") : implementation;
    }

    /// <summary>
    /// Validates parsed modules.
    /// </summary>
    /// <param name="modules">The parsed modules.</param>
    /// <returns>The validation result.</returns>
    public AsnValidationResult ValidateModules(System.ReadOnlySpan<AsnModule> modules)
    {
        return this.Implementation.ValidateModules(modules);
    }
}
