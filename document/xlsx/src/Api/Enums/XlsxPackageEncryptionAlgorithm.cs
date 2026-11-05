namespace xlsx;

/// <summary>
/// Identifies a supported OOXML package-encryption cipher and key size.
/// </summary>
public enum XlsxPackageEncryptionAlgorithm
{
    /// <summary>
    /// Indicates an unknown or unsupported algorithm.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Indicates AES with a 128-bit key.
    /// </summary>
    Aes128 = 1,
    /// <summary>
    /// Indicates AES with a 192-bit key.
    /// </summary>
    Aes192 = 2,
    /// <summary>
    /// Indicates AES with a 256-bit key.
    /// </summary>
    Aes256 = 3
}
