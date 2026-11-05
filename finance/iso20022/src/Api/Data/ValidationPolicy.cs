using System.Collections.Generic;

namespace iso20022;

/// <summary>Controls stage selection, collection behavior, provenance policy, and secure validation defaults.</summary>
public class ValidationPolicy
{
    /// <summary>The explicitly selected stages, or an empty list to run every applicable stage.</summary>
    public List<ValidationStage> Stages = new List<ValidationStage>();
    /// <summary>Whether validation stops after the first diagnostic that makes the message invalid.</summary>
    public bool FailFast;
    /// <summary>Whether warning diagnostics make the final report invalid.</summary>
    public bool TreatWarningsAsErrors;
    /// <summary>The parsing, collection, schema, and diagnostic resource bounds.</summary>
    public ResourceLimits Limits = new ResourceLimits();
    /// <summary>Whether draft repository, schema, profile, and code-set artifacts may be used.</summary>
    public bool AllowDraftArtifacts;
    /// <summary>The exact external code-set edition, or an empty value when selecting the latest edition.</summary>
    public string ExternalCodeEdition = string.Empty;
    /// <summary>Whether the latest available external code-set edition is selected instead of a pinned edition.</summary>
    public bool SelectLatestExternalCodeEdition = true;
    /// <summary>Whether inactive or obsolete external codes may pass code validation.</summary>
    public bool IncludeInactiveExternalCodes;
    /// <summary>The policy applied to content without a known generated or schema model.</summary>
    public UnknownContentMode UnknownContentMode = UnknownContentMode.Reject;
    /// <summary>Whether date and time values that permit an absent timezone must supply one.</summary>
    public bool RequireTimezone;
    /// <summary>Whether a message must contain a signature that passes the selected signature policy.</summary>
    public bool RequireValidSignature;
    /// <summary>Whether a signature is validated when present even when it is not required.</summary>
    public bool ValidatePresentSignatures = true;
    /// <summary>Whether diagnostic offending values are omitted from reports.</summary>
    public bool RedactOffendingValues = true;
}
