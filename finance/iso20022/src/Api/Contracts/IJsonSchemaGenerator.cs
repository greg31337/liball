namespace iso20022;

/// <summary>Generates deterministic JSON Schema Draft 2020-12 files from normalized repository and XSD concepts.</summary>
public interface IJsonSchemaGenerator
{
    /// <summary>Generates the selected JSON Schema files and their ownership manifest.</summary>
    /// <param name="plan">The exact sources, selection, settings, and previous ownership state.</param><returns>The exception-less deterministic generation result.</returns>
    GenerationResult GenerateJsonSchema(GenerationPlan? plan);
}
