namespace barcode;

/// <summary>
/// Holds barcode conformance status, governing standards, issues, and quality metrics.
/// </summary>
public class BarcodeValidationReport
{
    /// <summary>
    /// Indicates whether the target conforms under the requested mode.
    /// </summary>
    public bool IsConformant;

    /// <summary>
    /// The strict or tolerant mode used to produce the report.
    /// </summary>
    public BarcodeValidationMode Mode;

    /// <summary>
    /// The governing standard.
    /// </summary>
    public string? Standard;

    /// <summary>
    /// The governing application profile when one applies.
    /// </summary>
    public string? Profile;

    /// <summary>
    /// The validation issues in stable source order.
    /// </summary>
    public BarcodeValidationIssue[]? Issues;

    /// <summary>
    /// The measured quality and conformance diagnostics.
    /// </summary>
    public BarcodeQualityMetric[]? Metrics;

    /// <summary>
    /// The processing diagnostics produced while validating the target.
    /// </summary>
    public BarcodeDiagnostic[]? Diagnostics;
}
