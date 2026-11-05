using System.Collections.Generic;
using System.Xml.Schema;

namespace iso20022;

/// <summary>Contains the exception-less outcome, closure, diagnostics, cache state, and provenance of a schema operation.</summary>
public class SchemaCompilationResult
{
    /// <summary>Whether the schema operation completed successfully.</summary>
    public bool Success;
    /// <summary>Whether an exact schema registry key was found or accepted.</summary>
    public bool Found;
    /// <summary>The selected or imported entry-point schema when available.</summary>
    public SchemaArtifact? RootSchema;
    /// <summary>The deterministic local include and import closure, beginning with the entry point.</summary>
    public List<SchemaArtifact> Schemas = new List<SchemaArtifact>();
    /// <summary>The compiled XSD 1.0 schema set when compilation succeeded.</summary>
    public XmlSchemaSet? CompiledSchemas;
    /// <summary>Whether the selected schema was added to the registry.</summary>
    public bool Registered;
    /// <summary>Whether a higher-precedence registration replaced the previous exact key.</summary>
    public bool ReplacedExistingSchema;
    /// <summary>Whether registration invalidated a cached compiled dependency closure.</summary>
    public bool InvalidatedCachedCompilation;
    /// <summary>Whether the returned compiled schema set was reused from the exact-key cache.</summary>
    public bool ReusedCachedCompilation;
    /// <summary>Stable ordered diagnostic codes produced by import, resolution, or compilation.</summary>
    public List<string> DiagnosticCodes = new List<string>();
    /// <summary>Human-readable diagnostic messages corresponding to the diagnostic codes.</summary>
    public List<string> DiagnosticMessages = new List<string>();
    /// <summary>Source locations corresponding to diagnostics when line information is available.</summary>
    public List<SourceLocation> DiagnosticLocations = new List<SourceLocation>();
    /// <summary>Qualified XML paths corresponding to diagnostics when a message location is available.</summary>
    public List<XmlPathData> DiagnosticPaths = new List<XmlPathData>();
    /// <summary>Schema artifact URIs corresponding to diagnostics when schema provenance is available.</summary>
    public List<string> DiagnosticSchemaUris = new List<string>();
    /// <summary>A stable error code when the schema operation did not succeed.</summary>
    public string ErrorCode = string.Empty;
    /// <summary>A human-readable error message when the schema operation did not succeed.</summary>
    public string ErrorMessage = string.Empty;
}
