namespace xlsx;

/// <summary>
/// Provides built-in cryptographic operations for Standard and Agile OOXML encrypted packages.
/// </summary>
public class XlsxEncryptedPackageOperations : IXlsxEncryptedPackageOperations
{
    /// <summary>
    /// Initializes encrypted-package operations.
    /// </summary>
    public XlsxEncryptedPackageOperations()
    {
    }

    /// <summary>
    /// Detects and validates encrypted-package metadata without using a password.
    /// </summary>
    /// <param name = "compoundFileBytes">The complete compound-file bytes.</param>
    /// <returns>A detection result with parsed encryption metadata and diagnostics.</returns>
    public XlsxEncryptedPackageDetectionResult Detect(byte[] compoundFileBytes)
    {
        return XlsxEncryptedPackageCryptography.Detect(compoundFileBytes);
    }

    /// <summary>
    /// Decrypts an encrypted OOXML compound file using caller-owned password characters.
    /// </summary>
    /// <param name = "compoundFileBytes">The complete compound-file bytes.</param>
    /// <param name = "password">The caller-owned password characters.</param>
    /// <returns>A result containing OOXML ZIP bytes when password and integrity verification succeed.</returns>
    public XlsxEncryptedPackageResult Decrypt(byte[] compoundFileBytes, char[] password)
    {
        return XlsxEncryptedPackageCryptography.Decrypt(compoundFileBytes, password);
    }

    /// <summary>
    /// Encrypts OOXML ZIP bytes into a compound-file package.
    /// </summary>
    /// <param name = "packageBytes">The complete OOXML ZIP bytes.</param>
    /// <param name = "options">The password and encryption settings.</param>
    /// <returns>A result containing encrypted compound-file bytes.</returns>
    public XlsxEncryptedPackageResult Encrypt(byte[] packageBytes, XlsxPackageEncryptionOptions options)
    {
        return XlsxEncryptedPackageCryptography.Encrypt(packageBytes, options);
    }
}
