using System.Collections.Generic;

namespace iso20022;

/// <summary>Contains exact generation inputs, settings, schemas, and prior ownership state.</summary>
public class GenerationPlan
{
    /// <summary>The normalized repository snapshot used as the logical-model source when available.</summary>
    public RepositorySnapshot? Repository;
    /// <summary>The ordered XSD artifacts used as authoritative or supplementary schema sources.</summary>
    public List<SchemaArtifact> Schemas = new List<SchemaArtifact>();
    /// <summary>The deterministic selection, naming, formatting, and output settings.</summary>
    public GenerationOptions Options = new GenerationOptions();
    /// <summary>The previous owned-output manifest used for incremental replacement when available.</summary>
    public GenerationManifest? PreviousManifest;
}
