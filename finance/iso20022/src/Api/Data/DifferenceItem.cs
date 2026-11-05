using System.Collections.Generic;

namespace iso20022;

/// <summary>Describes one matched, added, removed, changed, ambiguous, or conflicting item.</summary>
public class DifferenceItem
{
    /// <summary>The normalized difference classification.</summary>
    public DifferenceKind Kind;
    /// <summary>The repository kind of the affected entity when applicable.</summary>
    public RepositoryEntityKind EntityKind;
    /// <summary>The exact source XMI or logical identifier.</summary>
    public string SourceIdentifier = string.Empty;
    /// <summary>The exact target XMI or logical identifier.</summary>
    public string TargetIdentifier = string.Empty;
    /// <summary>The source qualified or repository name.</summary>
    public string SourceName = string.Empty;
    /// <summary>The target qualified or repository name.</summary>
    public string TargetName = string.Empty;
    /// <summary>The affected source message or repository path.</summary>
    public XmlPathData SourcePath = new XmlPathData();
    /// <summary>The affected target message or repository path.</summary>
    public XmlPathData TargetPath = new XmlPathData();
    /// <summary>The changed feature, field, facet, or relationship name.</summary>
    public string PropertyName = string.Empty;
    /// <summary>The stable source lexical or logical value.</summary>
    public string SourceValue = string.Empty;
    /// <summary>The stable target lexical or logical value.</summary>
    public string TargetValue = string.Empty;
    /// <summary>The automatic or overridden match confidence from zero through one.</summary>
    public decimal MatchConfidence;
    /// <summary>Whether matching used identical stable repository identity.</summary>
    public bool ExactIdentifierMatch;
    /// <summary>Whether matching used qualified semantic fallback.</summary>
    public bool SemanticFallbackMatch;
    /// <summary>Whether a caller override selected the match.</summary>
    public bool OverrideMatch;
    /// <summary>Whether multiple plausible targets remain.</summary>
    public bool Ambiguous;
    /// <summary>The deterministic alternative target identifiers for an ambiguous match.</summary>
    public List<string> AlternativeTargetIdentifiers = new List<string>();
    /// <summary>Whether the change breaks at least one assessed compatibility dimension.</summary>
    public bool BreakingChange;
    /// <summary>Whether applying the change can discard source information.</summary>
    public bool DataLossRisk;
    /// <summary>The stable rationale for matching and difference classification.</summary>
    public string Rationale = string.Empty;
    /// <summary>The exact source artifact provenance when known.</summary>
    public ArtifactManifest? SourceArtifact;
    /// <summary>The exact target artifact provenance when known.</summary>
    public ArtifactManifest? TargetArtifact;
}
