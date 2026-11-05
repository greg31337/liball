namespace fix;

/// <summary>Generates deterministic SBE XML schemas from a canonical dictionary.</summary>
public interface ISbeSchemaGenerator
{
    /// <summary>Generates an SBE schema without writing caller files.</summary>
    /// <param name="dictionary">The source dictionary.</param>
    /// <param name="options">The SBE schema options.</param>
    /// <returns>The generated schema file and diagnostics.</returns>
    GenerationResult GenerateSbeSchema(FixDictionary dictionary, SbeGenerationOptions options);
}
