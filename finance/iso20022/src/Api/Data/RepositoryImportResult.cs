using System.Collections.Generic;

namespace iso20022;

/// <summary>Contains the exception-less outcome and counts of importing one repository artifact.</summary>
public class RepositoryImportResult
{
    /// <summary>Whether the repository was imported and passed normalized integrity checks.</summary>
    public bool Success;
    /// <summary>The normalized repository snapshot when available.</summary>
    public RepositorySnapshot? Snapshot;
    /// <summary>The artifact manifest captured while opening the input.</summary>
    public ArtifactManifest Artifact = new ArtifactManifest();
    /// <summary>The provenance captured while selecting and normalizing the input.</summary>
    public RepositoryProvenance Provenance = new RepositoryProvenance();
    /// <summary>The number of source XMI entities observed.</summary>
    public int SourceEntityCount;
    /// <summary>The number of source XMI relationships observed.</summary>
    public int SourceReferenceCount;
    /// <summary>The number of duplicate XMI identifiers reported by the source pass.</summary>
    public int SourceDuplicateIdentifierCount;
    /// <summary>The number of source XMI relationships whose targets could not be resolved.</summary>
    public int SourceUnresolvedReferenceCount;
    /// <summary>The number of unique normalized relationships retained in the snapshot.</summary>
    public int NormalizedReferenceCount;
    /// <summary>The number of normalized indexed entities.</summary>
    public int IndexedEntityCount;
    /// <summary>Stable ordered diagnostic codes produced by integrity validation.</summary>
    public List<string> DiagnosticCodes = new List<string>();
    /// <summary>Human-readable diagnostic messages corresponding to the diagnostic codes.</summary>
    public List<string> DiagnosticMessages = new List<string>();
    /// <summary>A stable error code when importing failed before or during normalization.</summary>
    public string ErrorCode = string.Empty;
    /// <summary>A human-readable error message when importing failed.</summary>
    public string ErrorMessage = string.Empty;
}
