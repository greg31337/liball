namespace xlsx;

/// <summary>
/// Provides password-based detection, decryption, and encryption for OOXML compound-file packages.
/// </summary>
public interface IXlsxEncryptedPackageOperations
{
    /// <summary>
    /// Detects and validates encrypted-package metadata without using a password.
    /// </summary>
    /// <param name = "compoundFileBytes">The complete compound-file bytes.</param>
    /// <returns>A detection result with parsed encryption metadata and diagnostics.</returns>
    XlsxEncryptedPackageDetectionResult Detect(byte[] compoundFileBytes);
    /// <summary>
    /// Decrypts an encrypted OOXML compound file using caller-owned password characters.
    /// </summary>
    /// <param name = "compoundFileBytes">The complete compound-file bytes.</param>
    /// <param name = "password">The caller-owned password characters.</param>
    /// <returns>A result containing OOXML ZIP bytes when password and integrity verification succeed.</returns>
    XlsxEncryptedPackageResult Decrypt(byte[] compoundFileBytes, char[] password);
    /// <summary>
    /// Encrypts OOXML ZIP bytes into a compound-file package.
    /// </summary>
    /// <param name = "packageBytes">The complete OOXML ZIP bytes.</param>
    /// <param name = "options">The password and encryption settings.</param>
    /// <returns>A result containing encrypted compound-file bytes.</returns>
    XlsxEncryptedPackageResult Encrypt(byte[] packageBytes, XlsxPackageEncryptionOptions options);
}
