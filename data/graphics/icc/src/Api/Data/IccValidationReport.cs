namespace icc;

/// <summary>Collects profile-validation policy, issues, and outcome.</summary>
public class IccValidationReport
{
    /// <summary>Gets or sets the applied policy.</summary>
    public IccValidationPolicy Policy;
    /// <summary>Gets or sets ordered validation issues.</summary>
    public IccValidationIssue[] Issues;
    /// <summary>Gets or sets the total issue count before retention limits are applied.</summary>
    public int TotalIssueCount;
    /// <summary>Gets or sets the total informational issue count.</summary>
    public int InformationCount;
    /// <summary>Gets or sets the total warning count.</summary>
    public int WarningCount;
    /// <summary>Gets or sets the total error count.</summary>
    public int ErrorCount;
    /// <summary>Gets or sets the total fatal issue count.</summary>
    public int FatalCount;
    /// <summary>Gets or sets whether issues were omitted by the configured maximum.</summary>
    public bool WasTruncated;
    /// <summary>Gets or sets whether the profile passed the policy.</summary>
    public bool IsValid;
    /// <summary>Gets or sets the operation outcome.</summary>
    public IccOperationResult Result;

    /// <summary>Initializes an unvalidated report.</summary>
    public IccValidationReport()
    {
        Policy = new IccValidationPolicy();
        Issues = new IccValidationIssue[0];
        TotalIssueCount = 0;
        InformationCount = 0;
        WarningCount = 0;
        ErrorCount = 0;
        FatalCount = 0;
        WasTruncated = false;
        IsValid = false;
        Result = new IccOperationResult();
    }
}
