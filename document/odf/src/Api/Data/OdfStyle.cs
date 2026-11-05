namespace odf;

/// <summary>
/// Describes the family of an OpenDocument style.
/// </summary>
public class OdfStyle
{
    /// <summary>
    /// The style name, or an empty string for an unnamed default style.
    /// </summary>
    public string Name = "";
    /// <summary>
    /// The optional user-facing style display name.
    /// </summary>
    public string DisplayName = "";
    /// <summary>
    /// The style family.
    /// </summary>
    public OdfStyleFamily Family;
    /// <summary>
    /// The preserved lexical style-family name.
    /// </summary>
    public string FamilyName = "";
    /// <summary>
    /// The XML part containing the style, or an empty string for a flat document.
    /// </summary>
    public string PartPath = "";
    /// <summary>
    /// The parent style name, or an empty string when no parent is declared.
    /// </summary>
    public string ParentStyleName = "";
    /// <summary>
    /// The next paragraph style name, or an empty string when none is declared.
    /// </summary>
    public string NextStyleName = "";
    /// <summary>
    /// The associated list style name, or an empty string when none is declared.
    /// </summary>
    public string ListStyleName = "";
    /// <summary>
    /// The associated master page name, or an empty string when none is declared.
    /// </summary>
    public string MasterPageName = "";
    /// <summary>
    /// The associated data style name, or an empty string when none is declared.
    /// </summary>
    public string DataStyleName = "";
    /// <summary>
    /// The qualified attributes of the style text-properties element.
    /// </summary>
    public OdfStyleProperty[] TextProperties = [];
    /// <summary>
    /// The child content of the style text-properties element.
    /// </summary>
    public OdfNode[] TextPropertyChildren = [];
    /// <summary>
    /// The underlying style text-properties element, when present.
    /// </summary>
    public OdfNode? TextPropertiesNode;
    /// <summary>
    /// The qualified attributes of the style paragraph-properties element.
    /// </summary>
    public OdfStyleProperty[] ParagraphProperties = [];
    /// <summary>
    /// The child content of the style paragraph-properties element.
    /// </summary>
    public OdfNode[] ParagraphPropertyChildren = [];
    /// <summary>
    /// The underlying style paragraph-properties element, when present.
    /// </summary>
    public OdfNode? ParagraphPropertiesNode;
    /// <summary>
    /// The qualified attributes of the style section-properties element.
    /// </summary>
    public OdfStyleProperty[] SectionProperties = [];
    /// <summary>
    /// The child content of the style section-properties element.
    /// </summary>
    public OdfNode[] SectionPropertyChildren = [];
    /// <summary>
    /// The underlying style section-properties element, when present.
    /// </summary>
    public OdfNode? SectionPropertiesNode;
    /// <summary>
    /// The qualified attributes of the style table-properties element.
    /// </summary>
    public OdfStyleProperty[] TableProperties = [];
    /// <summary>
    /// The child content of the style table-properties element.
    /// </summary>
    public OdfNode[] TablePropertyChildren = [];
    /// <summary>
    /// The underlying style table-properties element, when present.
    /// </summary>
    public OdfNode? TablePropertiesNode;
    /// <summary>
    /// The qualified attributes of the style table-column-properties element.
    /// </summary>
    public OdfStyleProperty[] TableColumnProperties = [];
    /// <summary>
    /// The child content of the style table-column-properties element.
    /// </summary>
    public OdfNode[] TableColumnPropertyChildren = [];
    /// <summary>
    /// The underlying style table-column-properties element, when present.
    /// </summary>
    public OdfNode? TableColumnPropertiesNode;
    /// <summary>
    /// The qualified attributes of the style table-row-properties element.
    /// </summary>
    public OdfStyleProperty[] TableRowProperties = [];
    /// <summary>
    /// The child content of the style table-row-properties element.
    /// </summary>
    public OdfNode[] TableRowPropertyChildren = [];
    /// <summary>
    /// The underlying style table-row-properties element, when present.
    /// </summary>
    public OdfNode? TableRowPropertiesNode;
    /// <summary>
    /// The qualified attributes of the style table-cell-properties element.
    /// </summary>
    public OdfStyleProperty[] TableCellProperties = [];
    /// <summary>
    /// The child content of the style table-cell-properties element.
    /// </summary>
    public OdfNode[] TableCellPropertyChildren = [];
    /// <summary>
    /// The underlying style table-cell-properties element, when present.
    /// </summary>
    public OdfNode? TableCellPropertiesNode;
    /// <summary>
    /// The qualified attributes of the style graphic-properties element used by graphic and presentation styles.
    /// </summary>
    public OdfStyleProperty[] GraphicProperties = [];
    /// <summary>
    /// The child content of the style graphic-properties element.
    /// </summary>
    public OdfNode[] GraphicPropertyChildren = [];
    /// <summary>
    /// The underlying style graphic-properties element, when present.
    /// </summary>
    public OdfNode? GraphicPropertiesNode;
    /// <summary>
    /// The qualified attributes of the style chart-properties element.
    /// </summary>
    public OdfStyleProperty[] ChartProperties = [];
    /// <summary>
    /// The child content of the style chart-properties element.
    /// </summary>
    public OdfNode[] ChartPropertyChildren = [];
    /// <summary>
    /// The underlying style chart-properties element, when present.
    /// </summary>
    public OdfNode? ChartPropertiesNode;
    /// <summary>
    /// The qualified attributes of the style drawing-page-properties element.
    /// </summary>
    public OdfStyleProperty[] DrawingPageProperties = [];
    /// <summary>
    /// The child content of the style drawing-page-properties element.
    /// </summary>
    public OdfNode[] DrawingPagePropertyChildren = [];
    /// <summary>
    /// The underlying style drawing-page-properties element, when present.
    /// </summary>
    public OdfNode? DrawingPagePropertiesNode;
    /// <summary>
    /// The qualified attributes of the style ruby-properties element.
    /// </summary>
    public OdfStyleProperty[] RubyProperties = [];
    /// <summary>
    /// The child content of the style ruby-properties element.
    /// </summary>
    public OdfNode[] RubyPropertyChildren = [];
    /// <summary>
    /// The underlying style ruby-properties element, when present.
    /// </summary>
    public OdfNode? RubyPropertiesNode;
    /// <summary>
    /// The qualified attributes of the style list-level-properties element.
    /// </summary>
    public OdfStyleProperty[] ListProperties = [];
    /// <summary>
    /// The child content of the style list-level-properties element.
    /// </summary>
    public OdfNode[] ListPropertyChildren = [];
    /// <summary>
    /// The underlying style list-level-properties element, when present.
    /// </summary>
    public OdfNode? ListPropertiesNode;
    /// <summary>
    /// Whether the style has a style name.
    /// </summary>
    public bool IsNamed;
    /// <summary>
    /// Whether the style is represented by <c>style:default-style</c>.
    /// </summary>
    public bool IsDefault;
    /// <summary>
    /// Whether the style belongs to the common styles section.
    /// </summary>
    public bool IsCommon;
    /// <summary>
    /// Whether the style belongs to an automatic styles section.
    /// </summary>
    public bool IsAutomatic;
    /// <summary>
    /// The underlying style XML element for lexical and extension access, when loaded or written.
    /// </summary>
    public OdfNode? Node;
    /// <summary>
    /// The resolved parent style, when declared and found.
    /// </summary>
    public OdfStyle? ParentStyle;
    /// <summary>
    /// The resolved next style, when declared and found.
    /// </summary>
    public OdfStyle? NextStyle;
    /// <summary>
    /// The default style for this style family, when present.
    /// </summary>
    public OdfStyle? DefaultStyle;
    /// <summary>
    /// The resolved list-style XML definition, when present.
    /// </summary>
    public OdfNode? ListStyleNode;
    /// <summary>
    /// The resolved typed list-style definition, when present.
    /// </summary>
    public OdfListStyle? ListStyle;
    /// <summary>
    /// The resolved master-page XML definition, when present.
    /// </summary>
    public OdfNode? MasterPageNode;
    /// <summary>
    /// The resolved data-style XML definition, when present.
    /// </summary>
    public OdfNode? DataStyleNode;
    /// <summary>
    /// The resolved typed data-style definition, when present.
    /// </summary>
    public OdfDataStyle? DataStyle;
    /// <summary>
    /// Whether the parent-style chain contains a cycle.
    /// </summary>
    public bool HasParentCycle;
    /// <summary>
    /// Whether the next-style chain contains a cycle.
    /// </summary>
    public bool HasNextCycle;
    internal OdfNode? OriginalNode;
    internal OdfStyleFamily OriginalFamily;
    internal string OriginalName = "";
    internal string OriginalDisplayName = "";
    internal string OriginalFamilyName = "";
    internal string OriginalPartPath = "";
    internal string OriginalParentStyleName = "";
    internal string OriginalNextStyleName = "";
    internal string OriginalListStyleName = "";
    internal string OriginalMasterPageName = "";
    internal string OriginalDataStyleName = "";
    internal bool OriginalIsDefault;
    internal bool OriginalIsAutomatic;
    internal OdfNode? OriginalTextPropertiesNode;
    internal OdfAttribute[] OriginalTextPropertyAttributes = [];
    internal OdfNode[] OriginalTextPropertyChildren = [];
    internal OdfNode? OriginalParagraphPropertiesNode;
    internal OdfAttribute[] OriginalParagraphPropertyAttributes = [];
    internal OdfNode[] OriginalParagraphPropertyChildren = [];
    internal OdfNode? OriginalSectionPropertiesNode;
    internal OdfAttribute[] OriginalSectionPropertyAttributes = [];
    internal OdfNode[] OriginalSectionPropertyChildren = [];
    internal OdfNode? OriginalTablePropertiesNode;
    internal OdfAttribute[] OriginalTablePropertyAttributes = [];
    internal OdfNode[] OriginalTablePropertyChildren = [];
    internal OdfNode? OriginalTableColumnPropertiesNode;
    internal OdfAttribute[] OriginalTableColumnPropertyAttributes = [];
    internal OdfNode[] OriginalTableColumnPropertyChildren = [];
    internal OdfNode? OriginalTableRowPropertiesNode;
    internal OdfAttribute[] OriginalTableRowPropertyAttributes = [];
    internal OdfNode[] OriginalTableRowPropertyChildren = [];
    internal OdfNode? OriginalTableCellPropertiesNode;
    internal OdfAttribute[] OriginalTableCellPropertyAttributes = [];
    internal OdfNode[] OriginalTableCellPropertyChildren = [];
    internal OdfNode? OriginalGraphicPropertiesNode;
    internal OdfAttribute[] OriginalGraphicPropertyAttributes = [];
    internal OdfNode[] OriginalGraphicPropertyChildren = [];
    internal OdfNode? OriginalChartPropertiesNode;
    internal OdfAttribute[] OriginalChartPropertyAttributes = [];
    internal OdfNode[] OriginalChartPropertyChildren = [];
    internal OdfNode? OriginalDrawingPagePropertiesNode;
    internal OdfAttribute[] OriginalDrawingPagePropertyAttributes = [];
    internal OdfNode[] OriginalDrawingPagePropertyChildren = [];
    internal OdfNode? OriginalRubyPropertiesNode;
    internal OdfAttribute[] OriginalRubyPropertyAttributes = [];
    internal OdfNode[] OriginalRubyPropertyChildren = [];
    internal OdfNode? OriginalListPropertiesNode;
    internal OdfAttribute[] OriginalListPropertyAttributes = [];
    internal OdfNode[] OriginalListPropertyChildren = [];
}
