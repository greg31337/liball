namespace pdf;

/// <summary>
/// Specifies the severity of a PDF/A preparation finding.
/// </summary>
public enum PdfAPreparationIssueSeverity
{
    /// <summary>
    /// Identifies a condition that should be reviewed before external PDF/A validation.
    /// </summary>
    Warning = 0,
    /// <summary>
    /// Identifies a condition that prevents the document from being ready for PDF/A conversion.
    /// </summary>
    Error = 1
}
