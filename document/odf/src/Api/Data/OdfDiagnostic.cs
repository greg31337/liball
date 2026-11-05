namespace odf;

/// <summary>
/// Describes the severity of a validation diagnostic.
/// </summary>
public class OdfDiagnostic
{
    /// <summary>
    /// The diagnostic severity.
    /// </summary>
    public OdfValidationSeverity Severity;
    /// <summary>
    /// The stable diagnostic code.
    /// </summary>
    public string Code = "";
    /// <summary>
    /// The human-readable diagnostic message.
    /// </summary>
    public string Message = "";
    /// <summary>
    /// The primary diagnostic location.
    /// </summary>
    public OdfLocation Location = new OdfLocation();
    /// <summary>
    /// A related location when <see cref="HasRelatedLocation"/> is true.
    /// </summary>
    public OdfLocation RelatedLocation = new OdfLocation();
    /// <summary>
    /// Whether the related location is meaningful.
    /// </summary>
    public bool HasRelatedLocation;
    /// <summary>
    /// The ODF version associated with the diagnostic, or <see cref="OdfVersion.Unknown"/>.
    /// </summary>
    public OdfVersion Version;
    /// <summary>
    /// The applicable specification section, or an empty string.
    /// </summary>
    public string SpecificationSection = "";
    /// <summary>
    /// Whether processing may safely continue after the diagnostic.
    /// </summary>
    public bool IsRecoverable;
}
