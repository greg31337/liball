namespace odf;

/// <summary>
/// Describes an anchored drawing frame and its text-box flow relationship.
/// </summary>
public class OdfTextFrame
{
    /// <summary>
    /// The frame name.
    /// </summary>
    public string Name = "";
    /// <summary>
    /// The drawing style name.
    /// </summary>
    public string StyleName = "";
    /// <summary>
    /// The default paragraph style name for text-box content.
    /// </summary>
    public string TextStyleName = "";
    /// <summary>
    /// The drawing layer name.
    /// </summary>
    public string Layer = "";
    /// <summary>
    /// The typed anchor kind.
    /// </summary>
    public OdfTextAnchorKind AnchorKind;
    /// <summary>
    /// The preserved lexical anchor type.
    /// </summary>
    public string AnchorType = "";
    /// <summary>
    /// The anchor page number.
    /// </summary>
    public string AnchorPageNumber = "";
    /// <summary>
    /// The horizontal frame position.
    /// </summary>
    public string X = "";
    /// <summary>
    /// The vertical frame position.
    /// </summary>
    public string Y = "";
    /// <summary>
    /// The frame width.
    /// </summary>
    public string Width = "";
    /// <summary>
    /// The frame height.
    /// </summary>
    public string Height = "";
    /// <summary>
    /// The minimum frame width.
    /// </summary>
    public string MinimumWidth = "";
    /// <summary>
    /// The minimum frame height.
    /// </summary>
    public string MinimumHeight = "";
    /// <summary>
    /// The relative frame width.
    /// </summary>
    public string RelativeWidth = "";
    /// <summary>
    /// The relative frame height.
    /// </summary>
    public string RelativeHeight = "";
    /// <summary>
    /// The frame stacking index.
    /// </summary>
    public string ZIndex = "";
    /// <summary>
    /// The frame transform expression.
    /// </summary>
    public string Transform = "";
    /// <summary>
    /// The name of the next frame in the text flow.
    /// </summary>
    public string ChainNextName = "";
    /// <summary>
    /// The resolved next frame in the text flow.
    /// </summary>
    public OdfTextFrame? NextFrame;
    /// <summary>
    /// The resolved preceding frame in the text flow.
    /// </summary>
    public OdfTextFrame? PreviousFrame;
    /// <summary>
    /// Whether the next-frame name is missing or ambiguous.
    /// </summary>
    public bool HasBrokenChain;
    /// <summary>
    /// Whether this frame participates in a chain cycle.
    /// </summary>
    public bool HasChainCycle;
    /// <summary>
    /// The contained text box, when present.
    /// </summary>
    public OdfTextBox? TextBox;
    /// <summary>
    /// The XML part containing the frame.
    /// </summary>
    public string PartPath = "";
    /// <summary>
    /// The underlying <c>draw:frame</c> element.
    /// </summary>
    public OdfNode? Node;
}
