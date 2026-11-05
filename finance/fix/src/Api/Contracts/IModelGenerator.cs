namespace fix;

/// <summary>Generates deterministic client models from a canonical dictionary.</summary>
public interface IModelGenerator
{
    /// <summary>Generates the selected models without writing caller files.</summary>
    /// <param name="dictionary">The source dictionary.</param>
    /// <param name="options">The generation selection and naming options.</param>
    /// <returns>The generated files, diagnostics, and manifest.</returns>
    GenerationResult GenerateModels(FixDictionary dictionary, ModelGenerationOptions options);
}
