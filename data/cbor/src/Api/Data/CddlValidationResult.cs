namespace cbor;

/// <summary>Holds the outcome of validating data against a compiled CDDL schema.</summary>
public class CddlValidationResult
{
    /// <summary>Indicates whether the instance matches the selected rule.</summary>
    public bool IsValid;
    /// <summary>The selected rule name.</summary>
    public string? RuleName;
    /// <summary>The bounded validation issues.</summary>
    public CddlValidationIssue[] Issues = [];
    /// <summary>Custom control outcomes retained for diagnostic context.</summary>
    public CddlControlResult[] ControlResults = [];
    /// <summary>The options used for the operation.</summary>
    public CddlValidationOptions Options = new CddlValidationOptions();
}
