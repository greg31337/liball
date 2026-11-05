namespace odf;

/// <summary>
/// Describes an OpenDocument page layout and its page, header, and footer properties.
/// </summary>
public class OdfPageLayout
{
    /// <summary>
    /// The layout name, or an empty string for the default page layout.
    /// </summary>
    public string Name = "";
    /// <summary>
    /// The preserved page-usage value.
    /// </summary>
    public string PageUsage = "";
    /// <summary>
    /// Whether this is represented by <c>style:default-page-layout</c>.
    /// </summary>
    public bool IsDefault;
    /// <summary>
    /// The XML part containing the layout, or an empty string for a flat document.
    /// </summary>
    public string PartPath = "";
    /// <summary>
    /// The preserved page number format.
    /// </summary>
    public string NumberFormat = "";
    /// <summary>
    /// The preserved page number prefix.
    /// </summary>
    public string NumberPrefix = "";
    /// <summary>
    /// The preserved page number suffix.
    /// </summary>
    public string NumberSuffix = "";
    /// <summary>
    /// The preserved page number letter-synchronization value.
    /// </summary>
    public string NumberLetterSync = "";
    /// <summary>
    /// The preserved first-page-number value.
    /// </summary>
    public string FirstPageNumber = "";
    /// <summary>
    /// The preserved print orientation.
    /// </summary>
    public string PrintOrientation = "";
    /// <summary>
    /// The qualified page-layout properties.
    /// </summary>
    public OdfStyleProperty[] PageProperties = [];
    /// <summary>
    /// The child content of the page-layout properties.
    /// </summary>
    public OdfNode[] PagePropertyChildren = [];
    /// <summary>
    /// The underlying page-layout-properties element, when present.
    /// </summary>
    public OdfNode? PagePropertiesNode;
    /// <summary>
    /// The page background-image child, when present.
    /// </summary>
    public OdfNode? BackgroundImageNode;
    /// <summary>
    /// The page columns child, when present.
    /// </summary>
    public OdfNode? ColumnsNode;
    /// <summary>
    /// The page footnote separator child, when present.
    /// </summary>
    public OdfNode? FootnoteSeparatorNode;
    /// <summary>
    /// The qualified header-style properties.
    /// </summary>
    public OdfStyleProperty[] HeaderProperties = [];
    /// <summary>
    /// The child content of the header-style properties.
    /// </summary>
    public OdfNode[] HeaderPropertyChildren = [];
    /// <summary>
    /// The underlying header-footer-properties element for the header style.
    /// </summary>
    public OdfNode? HeaderPropertiesNode;
    /// <summary>
    /// The qualified footer-style properties.
    /// </summary>
    public OdfStyleProperty[] FooterProperties = [];
    /// <summary>
    /// The child content of the footer-style properties.
    /// </summary>
    public OdfNode[] FooterPropertyChildren = [];
    /// <summary>
    /// The underlying header-footer-properties element for the footer style.
    /// </summary>
    public OdfNode? FooterPropertiesNode;
    /// <summary>
    /// The underlying page-layout element, when loaded or written.
    /// </summary>
    public OdfNode? Node;
    internal OdfNode? OriginalNode;
    internal OdfNode? OriginalPagePropertiesNode;
    internal OdfAttribute[] OriginalPagePropertyAttributes = [];
    internal OdfNode[] OriginalPagePropertyChildren = [];
    internal OdfNode? OriginalHeaderStyleNode;
    internal OdfNode? OriginalHeaderPropertiesNode;
    internal OdfAttribute[] OriginalHeaderPropertyAttributes = [];
    internal OdfNode[] OriginalHeaderPropertyChildren = [];
    internal OdfNode? OriginalFooterStyleNode;
    internal OdfNode? OriginalFooterPropertiesNode;
    internal OdfAttribute[] OriginalFooterPropertyAttributes = [];
    internal OdfNode[] OriginalFooterPropertyChildren = [];
    internal OdfNode? OriginalBackgroundImageNode;
    internal OdfNode? OriginalColumnsNode;
    internal OdfNode? OriginalFootnoteSeparatorNode;
    internal string OriginalName = "";
    internal string OriginalPageUsage = "";
    internal string OriginalPartPath = "";
    internal string OriginalNumberFormat = "";
    internal string OriginalNumberPrefix = "";
    internal string OriginalNumberSuffix = "";
    internal string OriginalNumberLetterSync = "";
    internal string OriginalFirstPageNumber = "";
    internal string OriginalPrintOrientation = "";
    internal bool OriginalIsDefault;
}
