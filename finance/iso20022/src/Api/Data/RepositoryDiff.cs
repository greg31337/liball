using System.Collections.Generic;

namespace iso20022;

/// <summary>Contains deterministic matches, differences, compatibility, and diagnostics for one comparison.</summary>
public class RepositoryDiff
{
    /// <summary>Whether comparison completed under the supplied configuration and limits.</summary>
    public bool Success;
    /// <summary>Whether no retained material difference was found.</summary>
    public bool Equivalent;
    /// <summary>The compared source repository edition.</summary>
    public string SourceEdition = string.Empty;
    /// <summary>The compared target repository edition.</summary>
    public string TargetEdition = string.Empty;
    /// <summary>Whether additional differences were omitted by resource limits.</summary>
    public bool Truncated;
    /// <summary>Whether at least one automatic match remained ambiguous.</summary>
    public bool HasAmbiguousMatches;
    /// <summary>Whether at least one three-way or override conflict was found.</summary>
    public bool HasConflicts;
    /// <summary>The deterministic retained differences.</summary>
    public List<DifferenceItem> Differences = new List<DifferenceItem>();
    /// <summary>The ordered compatibility assessments derived from retained differences.</summary>
    public List<CompatibilityAssessment> Compatibility = new List<CompatibilityAssessment>();
    /// <summary>The stable located comparison diagnostics.</summary>
    public List<ValidationDiagnostic> Diagnostics = new List<ValidationDiagnostic>();
    /// <summary>A stable operation error code when comparison could not complete.</summary>
    public string ErrorCode = string.Empty;
    /// <summary>A human-readable operation error message when comparison could not complete.</summary>
    public string ErrorMessage = string.Empty;
}
