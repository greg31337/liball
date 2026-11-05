namespace fix;

/// <summary>Validates Orchestra repositories beyond their XML schema constraints.</summary>
public class OrchestraValidator : IOrchestraValidator
{
    /// <summary>Validates repository references and behavior semantics.</summary>
    /// <param name="repository">The repository to validate.</param>
    /// <returns>The repository and ordered diagnostics.</returns>
    public OrchestraReadResult ValidateRepository(OrchestraRepository repository)
    {
        return OrchestraSemanticValidator.ValidateRepository(repository);
    }
}
