namespace barcode;

/// <summary>
/// Controls how requested quiet zones relate to symbology requirements.
/// </summary>
public enum BarcodeQuietZonePolicy
{
    /// <summary>
    /// Uses the standard quiet-zone dimensions.
    /// </summary>
    Standard = 0,

    /// <summary>
    /// Uses custom dimensions while enforcing the standard minimums.
    /// </summary>
    CustomAtLeastStandard = 1,

    /// <summary>
    /// Uses exact custom dimensions for diagnostic output.
    /// </summary>
    ExactCustom = 2
}
