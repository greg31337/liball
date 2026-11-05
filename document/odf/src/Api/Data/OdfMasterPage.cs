namespace odf;

/// <summary>
/// Describes an OpenDocument master page and its header and footer variants.
/// </summary>
public class OdfMasterPage
{
    /// <summary>
    /// The master-page name.
    /// </summary>
    public string Name = "";
    /// <summary>
    /// The optional display name.
    /// </summary>
    public string DisplayName = "";
    /// <summary>
    /// The referenced page-layout name.
    /// </summary>
    public string PageLayoutName = "";
    /// <summary>
    /// The optional drawing style name.
    /// </summary>
    public string DrawingStyleName = "";
    /// <summary>
    /// The presentation page-layout name assigned to this master.
    /// </summary>
    public string PresentationPageLayoutName = "";
    /// <summary>The header declaration name assigned to this master.</summary>
    public string HeaderName = "";
    /// <summary>The footer declaration name assigned to this master.</summary>
    public string FooterName = "";
    /// <summary>The date-and-time declaration name assigned to this master.</summary>
    public string DateTimeName = "";
    /// <summary>
    /// The next master-page name.
    /// </summary>
    public string NextStyleName = "";
    /// <summary>
    /// The XML part containing the master page, or an empty string for a flat document.
    /// </summary>
    public string PartPath = "";
    /// <summary>
    /// The default header content element.
    /// </summary>
    public OdfNode? Header;
    /// <summary>
    /// The left-page header content element.
    /// </summary>
    public OdfNode? HeaderLeft;
    /// <summary>
    /// The first-page header content element.
    /// </summary>
    public OdfNode? HeaderFirst;
    /// <summary>
    /// The default footer content element.
    /// </summary>
    public OdfNode? Footer;
    /// <summary>
    /// The left-page footer content element.
    /// </summary>
    public OdfNode? FooterLeft;
    /// <summary>
    /// The first-page footer content element.
    /// </summary>
    public OdfNode? FooterFirst;
    /// <summary>
    /// The non-header and non-footer master-page child content.
    /// </summary>
    public OdfNode[] Content = [];
    /// <summary>
    /// The typed text-flow views of the header and footer variants.
    /// </summary>
    public OdfMasterPageContent[] TextContents = [];
    /// <summary>
    /// The underlying master-page element, when loaded or written.
    /// </summary>
    public OdfNode? Node;
    /// <summary>
    /// The resolved page layout, when present.
    /// </summary>
    public OdfPageLayout? PageLayout;
    /// <summary>
    /// The resolved next master page, when present.
    /// </summary>
    public OdfMasterPage? NextMasterPage;
    /// <summary>The resolved presentation page layout.</summary>
    public OdfStyleResource? PresentationPageLayout;
    /// <summary>The resolved presentation header declaration.</summary>
    public OdfPresentationDeclaration? PresentationHeader;
    /// <summary>The resolved presentation footer declaration.</summary>
    public OdfPresentationDeclaration? PresentationFooter;
    /// <summary>The resolved presentation date-and-time declaration.</summary>
    public OdfPresentationDeclaration? PresentationDateTime;
    /// <summary>The presentation placeholders contained by this master.</summary>
    public OdfPresentationPlaceholder[] PresentationPlaceholders = [];
    /// <summary>The drawing objects contained by this master.</summary>
    public OdfDrawingObject[] DrawingObjects = [];
    internal OdfNode? OriginalNode;
    internal OdfNode? OriginalHeader;
    internal OdfNode? OriginalHeaderLeft;
    internal OdfNode? OriginalHeaderFirst;
    internal OdfNode? OriginalFooter;
    internal OdfNode? OriginalFooterLeft;
    internal OdfNode? OriginalFooterFirst;
    internal OdfNode[] OriginalContent = [];
    internal string OriginalName = "";
    internal string OriginalDisplayName = "";
    internal string OriginalPageLayoutName = "";
    internal string OriginalDrawingStyleName = "";
    internal string OriginalPresentationPageLayoutName = "";
    internal string OriginalHeaderName = "";
    internal string OriginalFooterName = "";
    internal string OriginalDateTimeName = "";
    internal string OriginalNextStyleName = "";
    internal string OriginalPartPath = "";
}
