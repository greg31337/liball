namespace xlsx;

/// <summary>
/// Contains the result of inspecting an encrypted OOXML compound file.
/// </summary>
public class XlsxEncryptedPackageDetectionResult
{
    /// <summary>
    /// Indicates whether the container and encryption metadata are valid and supported.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// Contains diagnostics, including explicit unsupported-algorithm diagnostics.
    /// </summary>
    public XlsxValidationResult Validation;
    /// <summary>
    /// Contains the parsed encryption metadata.
    /// </summary>
    public XlsxEncryptionInfo EncryptionInfo;

    /// <summary>
    /// Initializes an empty detection result.
    /// </summary>
    public XlsxEncryptedPackageDetectionResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        EncryptionInfo = new XlsxEncryptionInfo();
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }
}
