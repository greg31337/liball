using System.Collections.Generic;

namespace iso20022;

/// <summary>Contains the outcome of cross-checking normalized external-code publication formats.</summary>
public class ExternalCodeCatalogComparisonResult
{
    /// <summary>Whether comparison completed without an invalid request.</summary>
    public bool Success;
    /// <summary>Whether all comparable published content is equivalent.</summary>
    public bool Equivalent;
    /// <summary>The common compared edition, or an empty value when editions disagree.</summary>
    public string Edition = string.Empty;
    /// <summary>The number of distinct code-set identifiers examined.</summary>
    public int ComparedCodeSetCount;
    /// <summary>The number of distinct code values examined across code sets.</summary>
    public int ComparedCodeValueCount;
    /// <summary>Whether additional disagreements were omitted by the configured limit.</summary>
    public bool Truncated;
    /// <summary>The deterministic retained disagreements.</summary>
    public List<ExternalCodeDisagreement> Disagreements = new List<ExternalCodeDisagreement>();
    /// <summary>Stable ordered diagnostics describing format coverage limitations.</summary>
    public List<string> DiagnosticCodes = new List<string>();
    /// <summary>Human-readable messages corresponding to the diagnostic codes.</summary>
    public List<string> DiagnosticMessages = new List<string>();
    /// <summary>A stable error code when the comparison request is invalid.</summary>
    public string ErrorCode = string.Empty;
    /// <summary>A human-readable error message when the comparison request is invalid.</summary>
    public string ErrorMessage = string.Empty;
}
