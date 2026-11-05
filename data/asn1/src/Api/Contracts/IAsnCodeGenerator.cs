namespace asn1;

/// <summary>
/// Generates deterministic C# and descriptor outputs from validated schemas.
/// </summary>
public interface IAsnCodeGenerator
{
    /// <summary>
    /// Generates files for one validation result.
    /// </summary>
    /// <param name="validation">The validated schema.</param>
    /// <param name="options">The generation options.</param>
    /// <returns>The generated files in stable relative-path order.</returns>
    public AsnGeneratedFile[] GenerateFiles(AsnValidationResult validation, AsnGenerationOptions options);
}
