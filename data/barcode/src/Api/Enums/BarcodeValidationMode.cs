namespace barcode;

/// <summary>
/// Selects strict conformance validation or tolerant diagnostic analysis.
/// </summary>
public enum BarcodeValidationMode
{
    /// <summary>
    /// Requires strict conformance to the applicable standard or profile.
    /// </summary>
    Strict = 0,

    /// <summary>
    /// Allows recovery while continuing to report nonconformance.
    /// </summary>
    Tolerant = 1
}
