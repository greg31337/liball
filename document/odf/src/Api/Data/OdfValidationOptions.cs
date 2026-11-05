namespace odf;

/// <summary>
/// Selects a validation operating mode.
/// </summary>
public class OdfValidationOptions
{
    /// <summary>
    /// The validation mode.
    /// </summary>
    public OdfValidationMode Mode = OdfValidationMode.Strict;
    /// <summary>
    /// Resource limits applied during validation.
    /// </summary>
    public OdfSecurityLimits SecurityLimits = new OdfSecurityLimits();
    /// <summary>
    /// Explicit synchronous cancellation state.
    /// </summary>
    public OdfCancellationState Cancellation = new OdfCancellationState();
    /// <summary>
    /// The version to validate against, or <see cref="OdfVersion.Unknown"/> to use the document version.
    /// </summary>
    public OdfVersion Version;
    /// <summary>
    /// The conformance class to validate against, or <see cref="OdfConformanceClass.Unknown"/> to use the document class.
    /// </summary>
    public OdfConformanceClass ConformanceClass;
    /// <summary>
    /// The compatibility profile identifier, or an empty string for the standard profile.
    /// </summary>
    public string CompatibilityProfile = "";
    /// <summary>
    /// Whether package rules are validated.
    /// </summary>
    public bool ValidatePackage = true;
    /// <summary>
    /// Whether XML and schema rules are validated.
    /// </summary>
    public bool ValidateXml = true;
    /// <summary>
    /// Whether semantic and cross-reference rules are validated.
    /// </summary>
    public bool ValidateSemantics = true;
    /// <summary>
    /// Whether formulas are validated.
    /// </summary>
    public bool ValidateFormulas = true;
    /// <summary>
    /// Whether package security and integrity metadata is validated.
    /// </summary>
    public bool ValidateSecurity = true;
    /// <summary>
    /// Whether warning diagnostics are included.
    /// </summary>
    public bool IncludeWarnings = true;
    /// <summary>
    /// Whether informational diagnostics are included.
    /// </summary>
    public bool IncludeInformation;
}
