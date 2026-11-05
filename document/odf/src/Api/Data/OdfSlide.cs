namespace odf;

/// <summary>Describes a presentation slide and its assignments.</summary>
public class OdfSlide
{
    /// <summary>The zero-based slide index.</summary>
    public int Index;
    /// <summary>The slide name.</summary>
    public string Name = "";
    /// <summary>The optional drawing identifier.</summary>
    public string Id = "";
    /// <summary>The optional XML identifier.</summary>
    public string XmlId = "";
    /// <summary>The drawing-page style name.</summary>
    public string StyleName = "";
    /// <summary>The master-page name.</summary>
    public string MasterPageName = "";
    /// <summary>The presentation page-layout name.</summary>
    public string PresentationPageLayoutName = "";
    /// <summary>The header declaration name.</summary>
    public string HeaderName = "";
    /// <summary>The footer declaration name.</summary>
    public string FooterName = "";
    /// <summary>The date-and-time declaration name.</summary>
    public string DateTimeName = "";
    /// <summary>The preserved visibility token.</summary>
    public string Visibility = "";
    /// <summary>Whether the slide is visible.</summary>
    public bool IsVisible = true;
    /// <summary>Whether the visibility token is absent or valid.</summary>
    public bool IsVisibilityValid = true;
    /// <summary>The preserved navigation-order token list.</summary>
    public string NavigationOrder = "";
    /// <summary>Whether every navigation-order entry resolves uniquely on the slide.</summary>
    public bool IsNavigationOrderValid;
    /// <summary>The resolved navigation order.</summary>
    public OdfDrawingObject[] NavigationObjects = [];
    /// <summary>The resolved master page.</summary>
    public OdfMasterPage? MasterPage;
    /// <summary>The resolved presentation page layout.</summary>
    public OdfStyleResource? PresentationPageLayout;
    /// <summary>The resolved header declaration.</summary>
    public OdfPresentationDeclaration? Header;
    /// <summary>The resolved footer declaration.</summary>
    public OdfPresentationDeclaration? Footer;
    /// <summary>The resolved date-and-time declaration.</summary>
    public OdfPresentationDeclaration? DateTime;
    /// <summary>The placeholders assigned on the slide.</summary>
    public OdfPresentationPlaceholder[] Placeholders = [];
    /// <summary>The drawing objects contained by the slide.</summary>
    public OdfDrawingObject[] DrawingObjects = [];
    /// <summary>The notes page belonging to the slide.</summary>
    public OdfPresentationNotes? Notes;
    /// <summary>The transition assigned through the slide's drawing-page style.</summary>
    public OdfSlideTransition? Transition;
    /// <summary>The root animation timing trees assigned to the slide.</summary>
    public OdfAnimation[] Animations = [];
    /// <summary>The drawing-page view of the slide.</summary>
    public OdfDrawingObject? Drawing;
    /// <summary>The underlying slide element.</summary>
    public OdfNode? Node;
    internal string OriginalName = "";
    internal string OriginalId = "";
    internal string OriginalXmlId = "";
    internal string OriginalStyleName = "";
    internal string OriginalMasterPageName = "";
    internal string OriginalPresentationPageLayoutName = "";
    internal string OriginalHeaderName = "";
    internal string OriginalFooterName = "";
    internal string OriginalDateTimeName = "";
    internal string OriginalVisibility = "";
    internal string OriginalNavigationOrder = "";
}
