namespace barcode;

/// <summary>
/// Selects the standard or compact PDF417 row layout.
/// </summary>
public enum Pdf417Variant
{
    /// <summary>
    /// Uses the standard PDF417 layout.
    /// </summary>
    Standard = 0,

    /// <summary>
    /// Uses the Compact PDF417 layout.
    /// </summary>
    Compact = 1
}
