namespace odf;

/// <summary>Describes the notes page belonging to one presentation slide.</summary>
public class OdfPresentationNotes
{
    /// <summary>The notes drawing style name.</summary>
    public string StyleName = "";
    /// <summary>The presentation page-layout name assigned to the notes page.</summary>
    public string PresentationPageLayoutName = "";
    /// <summary>The referenced slide number preserved by a page thumbnail.</summary>
    public string PageNumber = "";
    /// <summary>The containing slide.</summary>
    public OdfSlide? Slide;
    /// <summary>The resolved presentation layout.</summary>
    public OdfStyleResource? PresentationPageLayout;
    /// <summary>The placeholders contained by the notes page.</summary>
    public OdfPresentationPlaceholder[] Placeholders = [];
    /// <summary>The drawing objects contained by the notes page.</summary>
    public OdfDrawingObject[] DrawingObjects = [];
    /// <summary>The text blocks contained by the notes page.</summary>
    public OdfTextBlock[] TextBlocks = [];
    /// <summary>The page-thumbnail element, when present.</summary>
    public OdfNode? PageThumbnailNode;
    /// <summary>The underlying notes element.</summary>
    public OdfNode? Node;
    internal string OriginalStyleName = "";
    internal string OriginalPresentationPageLayoutName = "";
    internal string OriginalPageNumber = "";
}
