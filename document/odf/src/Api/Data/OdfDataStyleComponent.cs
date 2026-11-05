namespace odf;

/// <summary>
/// Describes one ordered component of an OpenDocument data style.
/// </summary>
public class OdfDataStyleComponent
{
    /// <summary>
    /// The qualified component element name.
    /// </summary>
    public OdfQualifiedName Name = new OdfQualifiedName();
    /// <summary>
    /// The component attributes and their preserved lexical values.
    /// </summary>
    public OdfStyleProperty[] Attributes = [];
    /// <summary>
    /// The ordered component content, including embedded text elements.
    /// </summary>
    public OdfNode[] Content = [];
    /// <summary>
    /// The concatenated direct text content.
    /// </summary>
    public string Text = "";
    /// <summary>
    /// The underlying component element, when loaded or written.
    /// </summary>
    public OdfNode? Node;
    internal OdfNode? OriginalNode;
    internal OdfAttribute[] OriginalAttributes = [];
    internal OdfNode[] OriginalContent = [];
    internal string OriginalNamespaceName = "";
    internal string OriginalLocalName = "";
    internal string OriginalPrefix = "";
    internal string OriginalText = "";
}
