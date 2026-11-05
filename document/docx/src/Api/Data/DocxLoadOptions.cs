namespace docx;

/// <summary>
/// Controls how package parts are loaded into memory.
/// </summary>
public class DocxLoadOptions
{
    /// <summary>
    /// Whether whitespace-only XML nodes are preserved when XML parts are materialized.
    /// </summary>
    public bool PreserveXmlWhitespace;
    /// <summary>
    /// Whether package parts with unsupported content types are retained.
    /// </summary>
    public bool PreserveUnknownParts;
    /// <summary>
    /// Whether relationships with unsupported relationship types are retained.
    /// </summary>
    public bool PreserveUnknownRelationships;
    /// <summary>
    /// Whether markup in unsupported namespaces is retained in WordprocessingML parts.
    /// </summary>
    public bool PreserveUnknownMarkup;
    /// <summary>
    /// Whether the safe repair policy is applied for compatibility with earlier callers.
    /// </summary>
    public bool RepairRecoverablePackageIssues;
    /// <summary>
    /// Whether the lazy load xml parts behavior is enabled.
    /// </summary>
    public bool LazyLoadXmlParts;
    /// <summary>
    /// Whether the lazy load binary parts behavior is enabled.
    /// </summary>
    public bool LazyLoadBinaryParts;
    /// <summary>
    /// The validation performed after loading and optional repair.
    /// </summary>
    public DocxValidationMode ValidationMode;
    /// <summary>
    /// The repair policy applied before validation.
    /// </summary>
    public DocxRepairMode RepairMode;
    /// <summary>
    /// The report produced by the most recent load validation.
    /// </summary>
    public DocxValidationReport? ValidationReport;
    /// <summary>
    /// The report produced by the most recent load repair.
    /// </summary>
    public DocxValidationReport? RepairReport;

    /// <summary>
    /// Creates a load options instance.
    /// </summary>
    public DocxLoadOptions()
    {
        this.PreserveXmlWhitespace = true;
        this.PreserveUnknownParts = true;
        this.PreserveUnknownRelationships = true;
        this.PreserveUnknownMarkup = true;
        this.RepairRecoverablePackageIssues = false;
        this.LazyLoadXmlParts = true;
        this.LazyLoadBinaryParts = true;
        this.ValidationMode = DocxValidationMode.None;
        this.RepairMode = DocxRepairMode.Disabled;
        this.ValidationReport = null;
        this.RepairReport = null;
    }

    internal void ValidateValues()
    {
        if (this.ValidationMode != DocxValidationMode.None
            && this.ValidationMode != DocxValidationMode.Package
            && this.ValidationMode != DocxValidationMode.Full)
        {
            throw new ArgumentOutOfRangeException("ValidationMode");
        }

        if (this.RepairMode != DocxRepairMode.Disabled
            && this.RepairMode != DocxRepairMode.Safe
            && this.RepairMode != DocxRepairMode.Aggressive)
        {
            throw new ArgumentOutOfRangeException("RepairMode");
        }
    }
}
