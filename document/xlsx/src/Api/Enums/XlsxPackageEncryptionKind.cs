namespace xlsx;

/// <summary>
/// Identifies the password-encryption format used by an OOXML encrypted package.
/// </summary>
public enum XlsxPackageEncryptionKind
{
    /// <summary>
    /// Indicates that the encryption format is unknown or has not been selected.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Indicates ECMA-376 Standard encryption.
    /// </summary>
    Standard = 1,
    /// <summary>
    /// Indicates ECMA-376 Agile encryption.
    /// </summary>
    Agile = 2
}
