namespace odf;

/// <summary>
/// Identifies an OpenDocument document or template kind.
/// </summary>
public enum OdfDocumentKind
{
    /// <summary>
    /// The document kind is absent or unrecognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// A text document.
    /// </summary>
    Text = 1,
    /// <summary>
    /// A text document template.
    /// </summary>
    TextTemplate = 2,
    /// <summary>
    /// A master text document.
    /// </summary>
    TextMaster = 3,
    /// <summary>
    /// A master text document template.
    /// </summary>
    TextMasterTemplate = 4,
    /// <summary>
    /// A spreadsheet document.
    /// </summary>
    Spreadsheet = 5,
    /// <summary>
    /// A spreadsheet document template.
    /// </summary>
    SpreadsheetTemplate = 6,
    /// <summary>
    /// A presentation document.
    /// </summary>
    Presentation = 7,
    /// <summary>
    /// A presentation document template.
    /// </summary>
    PresentationTemplate = 8,
    /// <summary>
    /// A drawing document.
    /// </summary>
    Drawing = 9,
    /// <summary>
    /// A drawing document template.
    /// </summary>
    DrawingTemplate = 10,
    /// <summary>
    /// A chart document.
    /// </summary>
    Chart = 11,
    /// <summary>
    /// A chart document template.
    /// </summary>
    ChartTemplate = 12,
    /// <summary>
    /// A formula document.
    /// </summary>
    Formula = 13,
    /// <summary>
    /// A formula document template.
    /// </summary>
    FormulaTemplate = 14,
    /// <summary>
    /// An image document.
    /// </summary>
    Image = 15,
    /// <summary>
    /// An image document template.
    /// </summary>
    ImageTemplate = 16,
    /// <summary>
    /// A database front-end document.
    /// </summary>
    Database = 17
}
