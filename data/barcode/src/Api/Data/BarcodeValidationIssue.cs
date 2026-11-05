namespace barcode;

/// <summary>
/// Holds one barcode validation issue with stable identity and source context.
/// </summary>
public class BarcodeValidationIssue
{
    /// <summary>
    /// The issue severity.
    /// </summary>
    public BarcodeValidationSeverity Severity;

    /// <summary>
    /// The stable machine-readable issue code.
    /// </summary>
    public string? Code;

    /// <summary>
    /// The human-readable issue message.
    /// </summary>
    public string? Message;

    /// <summary>
    /// The governing standard or application profile.
    /// </summary>
    public string? Standard;

    /// <summary>
    /// The affected field or GS1 Application Identifier.
    /// </summary>
    public string? Field;

    /// <summary>
    /// The zero-based source offset, or a negative value when no offset applies.
    /// </summary>
    public int Offset;

    /// <summary>
    /// The expected constraint or value.
    /// </summary>
    public string? Expected;

    /// <summary>
    /// The observed value.
    /// </summary>
    public string? Observed;
}
