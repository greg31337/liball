namespace pdf;

/// <summary>
/// Identifies the severity of a PDF structure or conformance validation finding.
/// </summary>
public enum PdfValidationIssueSeverity
{
    /// <summary>Identifies a condition that prevents successful validation.</summary>
    Error = 1,
    /// <summary>Identifies a condition that should be reviewed but does not invalidate the result.</summary>
    Warning = 2
}
