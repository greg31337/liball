namespace xlsx;

/// <summary>
/// Contains encrypted or decrypted OOXML package bytes and operation status.
/// </summary>
public class XlsxEncryptedPackageResult
{
    /// <summary>
    /// Indicates whether the operation completed successfully.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// Contains diagnostics produced by the operation.
    /// </summary>
    public XlsxValidationResult Validation;
    /// <summary>
    /// Contains the encrypted compound-file bytes after encryption or the OOXML ZIP bytes after decryption.
    /// </summary>
    public byte[] Bytes;
    /// <summary>
    /// Contains the parsed or generated encryption metadata.
    /// </summary>
    public XlsxEncryptionInfo EncryptionInfo;
    /// <summary>
    /// Indicates whether password verification succeeded.
    /// </summary>
    public bool PasswordVerified;
    /// <summary>
    /// Indicates whether Agile package integrity verification succeeded. Standard encryption does not provide package integrity.
    /// </summary>
    public bool IntegrityVerified;

    /// <summary>
    /// Initializes an empty encrypted-package result.
    /// </summary>
    public XlsxEncryptedPackageResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        Bytes = new byte[0];
        EncryptionInfo = new XlsxEncryptionInfo();
        PasswordVerified = false;
        IntegrityVerified = false;
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }
}
