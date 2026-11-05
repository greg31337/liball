namespace barcode;

/// <summary>
/// Holds a calculated and observed barcode checksum outcome.
/// </summary>
public class BarcodeChecksumResult
{
    /// <summary>
    /// The barcode format whose checksum rules were applied.
    /// </summary>
    public BarcodeFormat Format;

    /// <summary>
    /// The checksum algorithm name.
    /// </summary>
    public string? Algorithm;

    /// <summary>
    /// The calculated check value.
    /// </summary>
    public int CalculatedValue;

    /// <summary>
    /// Indicates whether an observed check value was supplied.
    /// </summary>
    public bool HasObservedValue;

    /// <summary>
    /// The observed check value when supplied.
    /// </summary>
    public int ObservedValue;

    /// <summary>
    /// Indicates whether the observed value equals the calculated value.
    /// </summary>
    public bool IsValid;
}
