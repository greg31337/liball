namespace odf;

/// <summary>
/// Identifies an ODF package-entry encryption method.
/// </summary>
public enum OdfEncryptionMethod
{
    /// <summary>
    /// The encryption method is absent or unrecognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// The entry is not encrypted.
    /// </summary>
    None = 1,
    /// <summary>
    /// Blowfish in cipher-feedback mode.
    /// </summary>
    BlowfishCfb = 2,
    /// <summary>
    /// AES-128 in cipher-block-chaining mode.
    /// </summary>
    Aes128Cbc = 3,
    /// <summary>
    /// AES-192 in cipher-block-chaining mode.
    /// </summary>
    Aes192Cbc = 4,
    /// <summary>
    /// AES-256 in cipher-block-chaining mode.
    /// </summary>
    Aes256Cbc = 5,
    /// <summary>
    /// The method is recognized but unsupported.
    /// </summary>
    Unsupported = 6
}
