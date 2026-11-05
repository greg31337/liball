using System.Collections.Generic;

namespace iso20022;

/// <summary>Contains one normalized edition of ISO 20022 external code sets.</summary>
public class ExternalCodeCatalog
{
    /// <summary>The source artifact and integrity provenance.</summary>
    public ArtifactManifest Artifact = new ArtifactManifest();
    /// <summary>The external-code publication edition.</summary>
    public string Edition = string.Empty;
    /// <summary>The catalogue publication date when known.</summary>
    public IsoPartialDateValue? PublicationDate;
    /// <summary>The source schema identifier or namespace when present.</summary>
    public string SchemaIdentifier = string.Empty;
    /// <summary>The source schema version when present.</summary>
    public string SchemaVersion = string.Empty;
    /// <summary>The normalized source format, such as JSON, XSD, or XLSX.</summary>
    public string SourceFormat = string.Empty;
    /// <summary>The external code sets in deterministic source order.</summary>
    public List<ExternalCodeSet> CodeSets = new List<ExternalCodeSet>();
}
