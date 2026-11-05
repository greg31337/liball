using System.IO;

namespace iso20022;

/// <summary>Validates raw XML and generic message documents against one exact registered XSD closure.</summary>
public interface IXsdValidator
{
    /// <summary>Validates a raw XML stream against one exact registered schema key.</summary>
    /// <param name="input">The raw XML stream.</param><param name="selector">The exact schema registry key.</param><param name="options">The registry, compilation, validation, and resource policy.</param><returns>The validation result with stable diagnostics and schema provenance.</returns>
    SchemaCompilationResult ValidateXmlStream(
        Stream? input,
        SchemaArtifact? selector,
        SchemaRegistryOptions? options);
    /// <summary>Validates a generic message document against one exact registered schema key.</summary>
    /// <param name="document">The generic message document.</param><param name="selector">The exact schema registry key.</param><param name="options">The registry, compilation, validation, and resource policy.</param><returns>The validation result with stable diagnostics and schema provenance.</returns>
    SchemaCompilationResult ValidateMessageDocument(
        MessageDocument? document,
        SchemaArtifact? selector,
        SchemaRegistryOptions? options);
}
