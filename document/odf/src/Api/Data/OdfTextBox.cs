namespace odf;

/// <summary>
/// Describes text content hosted by a drawing frame.
/// </summary>
public class OdfTextBox
{
    /// <summary>The XML identifier.</summary>
    public string XmlId = "";
    /// <summary>The optional text-namespace identifier.</summary>
    public string TextId = "";
    /// <summary>The name of the next frame in the text-box chain.</summary>
    public string ChainNextName = "";
    /// <summary>
    /// The minimum text-box width.
    /// </summary>
    public string MinimumWidth = "";
    /// <summary>
    /// The minimum text-box height.
    /// </summary>
    public string MinimumHeight = "";
    /// <summary>
    /// The maximum text-box width.
    /// </summary>
    public string MaximumWidth = "";
    /// <summary>
    /// The maximum text-box height.
    /// </summary>
    public string MaximumHeight = "";
    /// <summary>
    /// The text-box corner radius.
    /// </summary>
    public string CornerRadius = "";
    /// <summary>
    /// Paragraphs and headings contained by the text box.
    /// </summary>
    public OdfTextBlock[] TextBlocks = [];
    /// <summary>
    /// The containing frame.
    /// </summary>
    public OdfTextFrame? Frame;
    /// <summary>
    /// The underlying <c>draw:text-box</c> element.
    /// </summary>
    public OdfNode? Node;
    internal string OriginalXmlId = "";
    internal string OriginalTextId = "";
    internal string OriginalChainNextName = "";
}
