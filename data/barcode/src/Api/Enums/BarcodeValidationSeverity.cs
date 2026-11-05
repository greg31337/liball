namespace barcode;

/// <summary>
/// Identifies the severity of a barcode validation issue.
/// </summary>
public enum BarcodeValidationSeverity
{
    /// <summary>
    /// Provides informational diagnostic context.
    /// </summary>
    Information = 0,

    /// <summary>
    /// Identifies a condition that may reduce usability or conformance.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Identifies a validation failure.
    /// </summary>
    Error = 2
}
