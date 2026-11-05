using System.Collections.Generic;

namespace iso20022;

/// <summary>Contains one exception-less validation outcome, deterministic stages, diagnostics, and selected identity.</summary>
public class ValidationReport
{
    /// <summary>Whether the validation operation completed under the selected configuration and resource limits.</summary>
    public bool Success;
    /// <summary>Whether the message passed all diagnostics that affect validity.</summary>
    public bool Valid;
    /// <summary>The resolved exact message identifier when known.</summary>
    public MessageIdentifier? MessageIdentifier;
    /// <summary>The resolved message namespace URI when known.</summary>
    public string NamespaceUri = string.Empty;
    /// <summary>The applied profile identifier when known.</summary>
    public string ProfileIdentifier = string.Empty;
    /// <summary>The selected external-code catalogue edition when external codes were evaluated.</summary>
    public string ExternalCodeEdition = string.Empty;
    /// <summary>The deterministic stages completed before the operation ended.</summary>
    public List<ValidationStage> CompletedStages = new List<ValidationStage>();
    /// <summary>The ordered validation diagnostics.</summary>
    public List<ValidationDiagnostic> Diagnostics = new List<ValidationDiagnostic>();
    /// <summary>The number of informational diagnostics.</summary>
    public int InformationCount;
    /// <summary>The number of warning diagnostics.</summary>
    public int WarningCount;
    /// <summary>The number of error diagnostics.</summary>
    public int ErrorCount;
    /// <summary>Whether fail-fast policy stopped validation before every applicable stage completed.</summary>
    public bool StoppedEarly;
    /// <summary>Whether the configured diagnostic count limit prevented additional diagnostics.</summary>
    public bool DiagnosticLimitReached;
    /// <summary>A stable operation error code when validation could not complete.</summary>
    public string ErrorCode = string.Empty;
    /// <summary>A human-readable operation error message when validation could not complete.</summary>
    public string ErrorMessage = string.Empty;
}
