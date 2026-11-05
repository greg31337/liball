namespace icc;

/// <summary>Represents one stable profile-validation diagnostic.</summary>
public class IccValidationIssue
{
    /// <summary>Gets or sets the stable rule identifier.</summary>
    public string RuleId;
    /// <summary>Gets or sets diagnostic severity.</summary>
    public IccValidationSeverity Severity;
    /// <summary>Gets or sets the diagnostic message.</summary>
    public string Message;
    /// <summary>Gets or sets the profile, tag, or element path.</summary>
    public string Path;
    /// <summary>Gets or sets the associated byte location.</summary>
    public IccByteLocation Location;
    /// <summary>Gets or sets a suggested remediation.</summary>
    public string Remediation;

    /// <summary>Initializes an empty informational diagnostic.</summary>
    public IccValidationIssue()
    {
        RuleId = string.Empty;
        Severity = IccValidationSeverity.Information;
        Message = string.Empty;
        Path = string.Empty;
        Location = new IccByteLocation();
        Remediation = string.Empty;
    }
}
