using System.Collections.Generic;

namespace iso20022;

internal class SchemaResolutionState
{
    internal SchemaRegistryOptions Options = new SchemaRegistryOptions();
    internal SchemaCompilationResult Result = new SchemaCompilationResult();
    internal string RootPath = string.Empty;
    internal List<string> ActiveUris = new List<string>();
    internal List<string> VisitedUris = new List<string>();
    internal List<SchemaArtifact> LocalSchemas = new List<SchemaArtifact>();
    internal List<string> LocalSchemaUris = new List<string>();
    internal bool Success = true;
}
