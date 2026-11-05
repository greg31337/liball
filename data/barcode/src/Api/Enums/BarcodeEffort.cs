namespace barcode;

/// <summary>
/// Selects the amount of image-processing work used to find and decode barcodes.
/// </summary>
public enum BarcodeEffort
{
    /// <summary>
    /// Uses the fastest processing path.
    /// </summary>
    Fast = 0,

    /// <summary>
    /// Balances processing cost and recovery capability.
    /// </summary>
    Balanced = 1,

    /// <summary>
    /// Uses the most thorough processing path.
    /// </summary>
    Thorough = 2
}
