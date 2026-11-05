namespace odf;

/// <summary>
/// Describes an OpenDocument paragraph or heading and its structured runs.
/// </summary>
public class OdfTextBlock
{
    /// <summary>
    /// Whether the block is a heading rather than a paragraph.
    /// </summary>
    public bool IsHeading;
    /// <summary>
    /// The heading outline level, or zero for a paragraph or invalid level.
    /// </summary>
    public int OutlineLevel;
    /// <summary>
    /// The preserved heading restart-numbering value.
    /// </summary>
    public string RestartNumbering = "";
    /// <summary>
    /// The preserved heading numbering start value.
    /// </summary>
    public string StartValue = "";
    /// <summary>
    /// The preserved heading list-header value.
    /// </summary>
    public string IsListHeader = "";
    /// <summary>
    /// The paragraph style name.
    /// </summary>
    public string StyleName = "";
    /// <summary>
    /// The preserved whitespace-separated style class names.
    /// </summary>
    public string ClassNames = "";
    /// <summary>
    /// The conditional paragraph style name.
    /// </summary>
    public string ConditionalStyleName = "";
    /// <summary>
    /// The effective break-before value from the paragraph style.
    /// </summary>
    public string BreakBefore = "";
    /// <summary>
    /// The effective break-after value from the paragraph style.
    /// </summary>
    public string BreakAfter = "";
    /// <summary>
    /// The extracted plain text of the block.
    /// </summary>
    public string Text = "";
    /// <summary>
    /// The structured inline runs in source order.
    /// </summary>
    public OdfTextRun[] Runs = [];
    /// <summary>
    /// The XML part containing the block, or an empty string for a flat document.
    /// </summary>
    public string PartPath = "";
    /// <summary>
    /// The underlying paragraph or heading element, when loaded or written.
    /// </summary>
    public OdfNode? Node;
    internal OdfNode? OriginalNode;
    internal bool OriginalIsHeading;
    internal int OriginalOutlineLevel;
    internal string OriginalRestartNumbering = "";
    internal string OriginalStartValue = "";
    internal string OriginalIsListHeader = "";
    internal string OriginalStyleName = "";
    internal string OriginalClassNames = "";
    internal string OriginalConditionalStyleName = "";
    internal string OriginalBreakBefore = "";
    internal string OriginalBreakAfter = "";
    internal string OriginalText = "";
    internal string OriginalRunSignature = "";
}
