using System.Collections.Generic;
using System.Threading;

namespace iso20022;

/// <summary>Requests a bounded repository, message-version, or profile comparison.</summary>
public class RepositoryComparisonRequest
{
    /// <summary>The exact source repository snapshot.</summary>
    public RepositorySnapshot? Source;
    /// <summary>The exact target repository snapshot.</summary>
    public RepositorySnapshot? Target;
    /// <summary>The exact source message selected for message-version comparison.</summary>
    public MessageIdentifier? SourceMessageIdentifier;
    /// <summary>The exact target message selected for message-version comparison.</summary>
    public MessageIdentifier? TargetMessageIdentifier;
    /// <summary>The common profile baseline used for a three-way rebase comparison.</summary>
    public ProfileDefinition? BaselineProfile;
    /// <summary>The locally modified profile used for a three-way rebase comparison.</summary>
    public ProfileDefinition? LocalProfile;
    /// <summary>The target-release profile used for a three-way rebase comparison.</summary>
    public ProfileDefinition? TargetProfile;
    /// <summary>Caller mappings that override automatic source-to-target entity matches.</summary>
    public MappingDefinition? MatchOverrides;
    /// <summary>Additional direct override rules evaluated in their supplied order.</summary>
    public List<MappingRuleDefinition> MatchOverrideRules = new List<MappingRuleDefinition>();
    /// <summary>The minimum semantic fallback match confidence from zero through one.</summary>
    public decimal MinimumMatchConfidence = 0.80m;
    /// <summary>Whether unchanged matched content is retained in the difference list.</summary>
    public bool IncludeUnchanged;
    /// <summary>Whether qualified semantic fallback may infer renames when XMI identity changes.</summary>
    public bool DetectRenames = true;
    /// <summary>Whether matched content may be classified as moved.</summary>
    public bool DetectMoves = true;
    /// <summary>Whether reader, writer, schema, loss, and semantic compatibility is assessed.</summary>
    public bool AssessCompatibility = true;
    /// <summary>The resource bounds applied during indexing, matching, and difference production.</summary>
    public ResourceLimits Limits = new ResourceLimits();
    /// <summary>The cancellation request observed during long-running comparison.</summary>
    public CancellationToken CancellationToken;
}
