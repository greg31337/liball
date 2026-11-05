using System.Collections.Generic;

namespace iso20022;

/// <summary>Contains one deterministic inspectable source-to-target migration plan.</summary>
public class MigrationPlan
{
    /// <summary>Whether planning completed under the supplied configuration and limits.</summary>
    public bool Success;
    /// <summary>The exact source message identifier.</summary>
    public MessageIdentifier? SourceMessageIdentifier;
    /// <summary>The exact target message identifier.</summary>
    public MessageIdentifier? TargetMessageIdentifier;
    /// <summary>The source-to-target difference used to derive this plan.</summary>
    public RepositoryDiff? Difference;
    /// <summary>The ordered deterministic migration operations.</summary>
    public List<MigrationStep> Steps = new List<MigrationStep>();
    /// <summary>The compatibility assessment associated with the plan.</summary>
    public CompatibilityAssessment? Compatibility;
    /// <summary>Whether at least one planned step can lose source information.</summary>
    public bool HasDataLoss;
    /// <summary>Whether at least one planned step requires caller implementation.</summary>
    public bool RequiresCustomRules;
    /// <summary>Whether the plan authorizes supplementary-data retention for unmapped content.</summary>
    public bool SupplementaryFallbackAuthorized;
    /// <summary>The ordered located planning diagnostics.</summary>
    public List<ValidationDiagnostic> Diagnostics = new List<ValidationDiagnostic>();
    /// <summary>A stable operation error code when planning could not complete.</summary>
    public string ErrorCode = string.Empty;
    /// <summary>A human-readable operation error message when planning could not complete.</summary>
    public string ErrorMessage = string.Empty;
}
