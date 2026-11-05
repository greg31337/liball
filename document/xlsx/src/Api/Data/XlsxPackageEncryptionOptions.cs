namespace xlsx;

/// <summary>
/// Controls password-based encryption of an OOXML package.
/// </summary>
/// <remarks>
/// Password characters are supplied as a mutable array so callers can erase them after use. The operation does not retain or modify the caller-owned array and erases its temporary password bytes and derived keys.
/// </remarks>
public class XlsxPackageEncryptionOptions
{
    /// <summary>
    /// Contains the caller-owned password characters.
    /// </summary>
    public char[] Password;
    /// <summary>
    /// Selects Standard or Agile encryption.
    /// </summary>
    public XlsxPackageEncryptionKind Kind;
    /// <summary>
    /// Selects the AES key size.
    /// </summary>
    public XlsxPackageEncryptionAlgorithm Algorithm;
    /// <summary>
    /// Specifies the Agile password-hash iteration count. Standard encryption always uses its specified iteration count.
    /// </summary>
    public int SpinCount;

    /// <summary>
    /// Initializes options for Agile AES-256 encryption with one hundred thousand password-hash iterations.
    /// </summary>
    public XlsxPackageEncryptionOptions()
    {
        Password = new char[0];
        Kind = XlsxPackageEncryptionKind.Agile;
        Algorithm = XlsxPackageEncryptionAlgorithm.Aes256;
        SpinCount = 100000;
    }
}
