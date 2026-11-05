using System.IO;

namespace iso20022;

/// <summary>Imports local XSD content into normalized schema artifacts without network resolution.</summary>
public interface ISchemaImporter
{
    /// <summary>Imports one XSD stream and its bounded local dependency closure.</summary>
    /// <param name="input">The encoded XSD stream.</param><param name="schema">The source identity, provenance, and registry key.</param><param name="options">The local resolution and resource policy.</param><returns>The normalized schema import result.</returns>
    SchemaCompilationResult ImportSchemaStream(
        Stream? input,
        SchemaArtifact? schema,
        SchemaRegistryOptions? options);
    /// <summary>Imports one XSD file and its bounded local dependency closure.</summary>
    /// <param name="path">The caller-selected XSD file path.</param><param name="schema">The source identity, provenance, and registry key.</param><param name="options">The local resolution and resource policy.</param><returns>The normalized schema import result.</returns>
    SchemaCompilationResult ImportSchemaFile(
        string? path,
        SchemaArtifact? schema,
        SchemaRegistryOptions? options);
}
