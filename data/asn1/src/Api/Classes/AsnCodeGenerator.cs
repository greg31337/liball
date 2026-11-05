namespace asn1;

/// <summary>
/// Exposes code generation through a composed generator implementation.
/// </summary>
public class AsnCodeGenerator : IAsnCodeGenerator
{
    internal IAsnCodeGenerator Implementation;

    /// <summary>
    /// Initializes a code-generator facade over an operation implementation.
    /// </summary>
    /// <param name="implementation">The implementation that generates outputs.</param>
    public AsnCodeGenerator(IAsnCodeGenerator implementation)
    {
        this.Implementation = implementation == null ? throw new System.ArgumentNullException("implementation") : implementation;
    }

    /// <summary>
    /// Generates files from a validated schema.
    /// </summary>
    /// <param name="validation">The validated schema.</param>
    /// <param name="options">The generation options.</param>
    /// <returns>The generated files in stable order.</returns>
    public AsnGeneratedFile[] GenerateFiles(AsnValidationResult validation, AsnGenerationOptions options)
    {
        return this.Implementation.GenerateFiles(validation, options);
    }
}
