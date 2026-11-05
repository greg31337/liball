namespace odf;

/// <summary>Describes the presentation handout master.</summary>
public class OdfPresentationHandout
{
    /// <summary>The handout drawing style name.</summary>
    public string StyleName = "";
    /// <summary>The presentation page-layout name assigned to the handout.</summary>
    public string PresentationPageLayoutName = "";
    /// <summary>The header declaration name.</summary>
    public string HeaderName = "";
    /// <summary>The footer declaration name.</summary>
    public string FooterName = "";
    /// <summary>The date-and-time declaration name.</summary>
    public string DateTimeName = "";
    /// <summary>The resolved presentation layout.</summary>
    public OdfStyleResource? PresentationPageLayout;
    /// <summary>The resolved header declaration.</summary>
    public OdfPresentationDeclaration? Header;
    /// <summary>The resolved footer declaration.</summary>
    public OdfPresentationDeclaration? Footer;
    /// <summary>The resolved date-and-time declaration.</summary>
    public OdfPresentationDeclaration? DateTime;
    /// <summary>The placeholders contained by the handout master.</summary>
    public OdfPresentationPlaceholder[] Placeholders = [];
    /// <summary>The drawing objects contained by the handout master.</summary>
    public OdfDrawingObject[] DrawingObjects = [];
    /// <summary>The text blocks contained by the handout master.</summary>
    public OdfTextBlock[] TextBlocks = [];
    /// <summary>The underlying handout-master element.</summary>
    public OdfNode? Node;
    internal string OriginalStyleName = "";
    internal string OriginalPresentationPageLayoutName = "";
    internal string OriginalHeaderName = "";
    internal string OriginalFooterName = "";
    internal string OriginalDateTimeName = "";
}
