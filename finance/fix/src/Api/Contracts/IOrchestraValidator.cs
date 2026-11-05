namespace fix;

/// <summary>Performs Orchestra semantic validation beyond XML schemas.</summary>
public interface IOrchestraValidator
{
    /// <summary>Validates repository references and behavior semantics.</summary>
    /// <param name="repository">The repository to validate.</param>
    /// <returns>The repository and ordered diagnostics.</returns>
    OrchestraReadResult ValidateRepository(OrchestraRepository repository);
}
