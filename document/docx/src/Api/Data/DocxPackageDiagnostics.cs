namespace docx;

/// <summary>
/// Summarizes structural, compatibility, and recoverability observations for a DOCX package.
/// </summary>
public class DocxPackageDiagnostics
{
    /// <summary>
    /// Whether the supplied data could be read as an OPC package.
    /// </summary>
    public bool IsReadablePackage;
    /// <summary>
    /// Whether duplicate ZIP entries or duplicate loaded part names were detected.
    /// </summary>
    public bool HasDuplicateZipEntries;
    /// <summary>
    /// Whether a required OPC or WordprocessingML package item is missing.
    /// </summary>
    public bool HasMissingRequiredItems;
    /// <summary>
    /// Whether corrupt package data prevented complete inspection.
    /// </summary>
    public bool HasCorruptInput;
    /// <summary>
    /// Whether macro-enabled content was detected.
    /// </summary>
    public bool HasMacroEnabledContent;
    /// <summary>
    /// The detected Strict or Transitional namespace conformance.
    /// </summary>
    public DocxNamespaceConformance NamespaceConformance;
    /// <summary>
    /// Whether one or more packaging defects can be repaired by supported repair operations.
    /// </summary>
    public bool HasRecoverableDefects;
    /// <summary>
    /// Validation and parsing observations discovered during inspection.
    /// </summary>
    public DocxValidationReport Report;
    /// <summary>
    /// Repairs applied only to an internal diagnostic copy. The inspected package remains unchanged.
    /// </summary>
    public DocxValidationReport RecoverableDefects;

    /// <summary>
    /// Creates an empty package diagnostics result.
    /// </summary>
    public DocxPackageDiagnostics()
    {
        this.IsReadablePackage = false;
        this.HasDuplicateZipEntries = false;
        this.HasMissingRequiredItems = false;
        this.HasCorruptInput = false;
        this.HasMacroEnabledContent = false;
        this.NamespaceConformance = DocxNamespaceConformance.Unknown;
        this.HasRecoverableDefects = false;
        this.Report = new DocxValidationReport();
        this.RecoverableDefects = new DocxValidationReport();
    }
}
