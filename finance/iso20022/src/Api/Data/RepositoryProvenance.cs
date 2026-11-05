using System.Collections.Generic;

namespace iso20022;

/// <summary>Records the exact artifact, metamodel, adapter, and import context of a repository snapshot.</summary>
public class RepositoryProvenance
{
    /// <summary>The primary repository artifact manifest.</summary>
    public ArtifactManifest Artifact = new ArtifactManifest();
    /// <summary>The selected entry name when the artifact was an archive.</summary>
    public string SourceEntryName = string.Empty;
    /// <summary>The namespace of the serialized repository root.</summary>
    public string RepositoryNamespace = string.Empty;
    /// <summary>The discovered metamodel namespace.</summary>
    public string MetamodelNamespace = string.Empty;
    /// <summary>The discovered metamodel version or edition.</summary>
    public string MetamodelVersion = string.Empty;
    /// <summary>The identifier of the adapter used to normalize the source.</summary>
    public string AdapterIdentifier = string.Empty;
    /// <summary>The import time when recorded by the caller or importer.</summary>
    public IsoDateTimeValue? ImportedAt;
    /// <summary>Additional artifacts that contributed to the normalized snapshot.</summary>
    public List<ArtifactManifest> RelatedArtifacts = new List<ArtifactManifest>();
    /// <summary>Unrecognized provenance metadata.</summary>
    public List<RepositoryUnknownData> UnknownContent = new List<RepositoryUnknownData>();
}
