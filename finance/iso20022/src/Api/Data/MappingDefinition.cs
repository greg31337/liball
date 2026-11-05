using System.Collections.Generic;

namespace iso20022;

/// <summary>Defines one explicit inspectable mapping between messages or canonical models.</summary>
public class MappingDefinition
{
    /// <summary>The stable mapping identifier.</summary>
    public string Identifier = string.Empty;
    /// <summary>The exact mapping version.</summary>
    public string Version = string.Empty;
    /// <summary>The human-readable mapping purpose and scope.</summary>
    public string Definition = string.Empty;
    /// <summary>The exact source ISO message identifier when mapping from an ISO message.</summary>
    public MessageIdentifier? SourceMessageIdentifier;
    /// <summary>The exact target ISO message identifier when mapping to an ISO message.</summary>
    public MessageIdentifier? TargetMessageIdentifier;
    /// <summary>The stable source canonical-model name when applicable.</summary>
    public string SourceModelName = string.Empty;
    /// <summary>The stable target canonical-model name when applicable.</summary>
    public string TargetModelName = string.Empty;
    /// <summary>The deterministic ordered explicit mapping rules.</summary>
    public List<MappingRuleDefinition> Rules = new List<MappingRuleDefinition>();
    /// <summary>Whether unmatched source content is permitted.</summary>
    public bool AllowUnmappedSource;
    /// <summary>Whether every required target must be produced by a rule.</summary>
    public bool RequireAllTargets = true;
    /// <summary>The exact artifact provenance of the mapping definition when known.</summary>
    public ArtifactManifest? Artifact;
}
