namespace barcode;

/// <summary>
/// Controls calculation, validation, and transmission of barcode check characters.
/// </summary>
public enum BarcodeChecksumMode
{
    /// <summary>
    /// Does not calculate or verify a check character.
    /// </summary>
    None = 0,

    /// <summary>
    /// Calculates and appends a check character.
    /// </summary>
    Calculate = 1,

    /// <summary>
    /// Requires a supplied check character.
    /// </summary>
    Require = 2,

    /// <summary>
    /// Verifies and retains the check character in decoded data.
    /// </summary>
    VerifyKeep = 3,

    /// <summary>
    /// Verifies and removes the check character from decoded data.
    /// </summary>
    VerifyStrip = 4
}
