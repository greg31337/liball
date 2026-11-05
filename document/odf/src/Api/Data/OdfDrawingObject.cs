namespace odf;

/// <summary>
/// Describes a drawing page, layer, frame, group, or shape.
/// </summary>
public class OdfDrawingObject
{
    /// <summary>
    /// The drawing-object kind.
    /// </summary>
    public OdfDrawingKind Kind;
    /// <summary>The source element local name.</summary>
    public string ElementName = "";
    /// <summary>The optional <c>draw:id</c> identifier.</summary>
    public string Id = "";
    /// <summary>The optional XML identifier.</summary>
    public string XmlId = "";
    /// <summary>The drawing object name.</summary>
    public string Name = "";
    /// <summary>The optional display name.</summary>
    public string DisplayName = "";
    /// <summary>The referenced drawing or presentation style name.</summary>
    public string StyleName = "";
    /// <summary>The namespace of the style-name attribute.</summary>
    public string StyleNamespace = "";
    /// <summary>The lexical list of drawing or presentation class names.</summary>
    public string ClassNames = "";
    /// <summary>The referenced text style name.</summary>
    public string TextStyleName = "";
    /// <summary>The drawing layer name.</summary>
    public string LayerName = "";
    /// <summary>The referenced master-page name for a drawing page.</summary>
    public string MasterPageName = "";
    /// <summary>The referenced presentation page-layout name.</summary>
    public string PresentationPageLayoutName = "";
    /// <summary>The lexical page navigation order.</summary>
    public string NavigationOrder = "";
    /// <summary>The lexical layer display mode.</summary>
    public string Display = "";
    /// <summary>The preserved layer protection value.</summary>
    public string ProtectedValue = "";
    /// <summary>Whether the layer is protected.</summary>
    public bool IsProtected;
    /// <summary>The horizontal position.</summary>
    public string X = "";
    /// <summary>The vertical position.</summary>
    public string Y = "";
    /// <summary>The object width.</summary>
    public string Width = "";
    /// <summary>The object height.</summary>
    public string Height = "";
    /// <summary>The shape-specific lexical geometry.</summary>
    public OdfGeometry Geometry = new OdfGeometry();
    /// <summary>The three-dimensional scene or object metadata.</summary>
    public OdfThreeDimensionalGeometry ThreeDimensional =
        new OdfThreeDimensionalGeometry();
    /// <summary>The effective graphic properties applied to this object.</summary>
    public OdfGraphicProperties GraphicProperties =
        new OdfGraphicProperties();
    /// <summary>The image description when this object is a drawing image.</summary>
    public OdfImage? Image;
    /// <summary>The passive media description when this is a media object.</summary>
    public OdfMediaResource? MediaResource;
    /// <summary>The embedded-content description when this is an object.</summary>
    public OdfEmbeddedObject? EmbeddedObject;
    /// <summary>The form-control identifier referenced by a control shape.</summary>
    public string ControlId = "";
    /// <summary>Paragraphs and headings directly contained by the shape.</summary>
    public OdfTextBlock[] TextBlocks = [];
    /// <summary>The text-box view when this object is a frame text box.</summary>
    public OdfTextBox? TextBox;
    /// <summary>The text or spreadsheet anchor description.</summary>
    public OdfDrawingAnchor Anchor = new OdfDrawingAnchor();
    /// <summary>The stacking-order description.</summary>
    public OdfDrawingZOrder ZOrder = new OdfDrawingZOrder();
    /// <summary>The preserved drawing transform.</summary>
    public OdfTransform Transform = new OdfTransform();
    /// <summary>The clipping and wrapping contour attached to a frame.</summary>
    public OdfContour Contour = new OdfContour();
    /// <summary>The accessible title and description.</summary>
    public OdfDrawingAccessibility Accessibility =
        new OdfDrawingAccessibility();
    /// <summary>The inert event-listener descriptions.</summary>
    public OdfDrawingEvent[] Events = [];
    /// <summary>The outgoing object and resource references.</summary>
    public OdfDrawingReference[] References = [];
    /// <summary>The nearest containing drawing object.</summary>
    public OdfDrawingObject? Parent;
    /// <summary>The containing drawing page.</summary>
    public OdfDrawingObject? Page;
    /// <summary>The resolved drawing layer.</summary>
    public OdfDrawingObject? Layer;
    /// <summary>The directly contained drawing objects in XML order.</summary>
    public OdfDrawingObject[] Children = [];
    /// <summary>The directly contained drawing objects in stacking order.</summary>
    public OdfDrawingObject[] ZOrderedChildren = [];
    /// <summary>The XML part containing this object.</summary>
    public string PartPath = "";
    /// <summary>The underlying drawing element.</summary>
    public OdfNode? Node;
    internal string OriginalId = "";
    internal string OriginalXmlId = "";
    internal string OriginalName = "";
    internal string OriginalDisplayName = "";
    internal string OriginalStyleName = "";
    internal string OriginalStyleNamespace = "";
    internal string OriginalClassNames = "";
    internal string OriginalTextStyleName = "";
    internal string OriginalLayerName = "";
    internal string OriginalMasterPageName = "";
    internal string OriginalPresentationPageLayoutName = "";
    internal string OriginalNavigationOrder = "";
    internal string OriginalDisplay = "";
    internal string OriginalProtectedValue = "";
    internal bool OriginalIsProtected;
    internal string OriginalX = "";
    internal string OriginalY = "";
    internal string OriginalWidth = "";
    internal string OriginalHeight = "";
    internal string OriginalControlId = "";
}
