using System.Collections.Generic;

namespace iso20022;

/// <summary>Contains the exception-less outcome of normalizing an external-code artifact.</summary>
public class ExternalCodeImportResult
{
    /// <summary>Whether the artifact was imported successfully.</summary>
    public bool Success;
    /// <summary>The normalized external-code catalogue when available.</summary>
    public ExternalCodeCatalog? Catalog;
    /// <summary>The source artifact and integrity provenance.</summary>
    public ArtifactManifest Artifact = new ArtifactManifest();
    /// <summary>The selected source or archive-entry name.</summary>
    public string SourceEntryName = string.Empty;
    /// <summary>The normalized source format, such as JSON, XSD, or XLSX.</summary>
    public string SourceFormat = string.Empty;
    /// <summary>Stable ordered diagnostic codes produced during normalization.</summary>
    public List<string> DiagnosticCodes = new List<string>();
    /// <summary>Human-readable diagnostic messages corresponding to the diagnostic codes.</summary>
    public List<string> DiagnosticMessages = new List<string>();
    /// <summary>A stable error code when importing failed.</summary>
    public string ErrorCode = string.Empty;
    /// <summary>A human-readable error message when importing failed.</summary>
    public string ErrorMessage = string.Empty;
}
