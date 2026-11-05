using System.IO;

namespace iso20022;

/// <summary>Registers, selects, and compiles local schemas by exact namespace, message, version, and profile keys.</summary>
public interface ISchemaRegistry
{
    /// <summary>Imports and registers one raw XSD stream through the ordinary registry path.</summary>
    /// <param name="input">The raw XSD stream.</param><param name="schema">The exact identity and artifact provenance.</param><param name="options">The import, registry, resolution, compilation, and cache policy.</param><returns>The import and registration result.</returns>
    SchemaCompilationResult ImportRegisterSchemaStream(
        Stream? input,
        SchemaArtifact? schema,
        SchemaRegistryOptions? options);
    /// <summary>Imports and registers one local XSD file through the ordinary registry path.</summary>
    /// <param name="path">The local XSD file path.</param><param name="schema">The exact identity and artifact provenance.</param><param name="options">The import, registry, resolution, compilation, and cache policy.</param><returns>The import and registration result.</returns>
    SchemaCompilationResult ImportRegisterSchemaFile(
        string? path,
        SchemaArtifact? schema,
        SchemaRegistryOptions? options);
    /// <summary>Imports and registers one explicitly named XSD entry from a bounded local ZIP stream.</summary>
    /// <param name="input">The ZIP artifact stream.</param><param name="schema">The exact identity, selected entry name, and artifact provenance.</param><param name="options">The import, registry, resolution, compilation, and cache policy.</param><returns>The import and registration result.</returns>
    SchemaCompilationResult ImportRegisterSchemaArchive(
        Stream? input,
        SchemaArtifact? schema,
        SchemaRegistryOptions? options);
    /// <summary>Registers one schema and applies explicit collision, precedence, replacement, and cache policy.</summary>
    /// <param name="schema">The normalized schema artifact to register.</param><param name="options">The registry and compilation policy.</param><returns>The registration result and affected provenance.</returns>
    SchemaCompilationResult RegisterSchema(
        SchemaArtifact? schema,
        SchemaRegistryOptions? options);
    /// <summary>Looks up one exact namespace, message identifier, version, and profile key.</summary>
    /// <param name="selector">The exact registry key fields to select.</param><param name="options">The bounded lookup policy.</param><returns>The lookup result and selected provenance.</returns>
    SchemaCompilationResult LookupSchema(
        SchemaArtifact? selector,
        SchemaRegistryOptions? options);
    /// <summary>Compiles or reuses the bounded local closure for one exact registry key.</summary>
    /// <param name="selector">The exact registry key fields to compile.</param><param name="options">The local resolution, compilation, and cache policy.</param><returns>The compilation result, diagnostics, closure, and provenance.</returns>
    SchemaCompilationResult CompileSchema(
        SchemaArtifact? selector,
        SchemaRegistryOptions? options);
}
