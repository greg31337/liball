namespace odf;

/// <summary>
/// Represents a namespace-aware element retained from an OpenDocument manifest.
/// </summary>
public class OdfManifestNode
{
    /// <summary>
    /// The qualified element name, including its original prefix.
    /// </summary>
    public OdfQualifiedName Name = new OdfQualifiedName();
    /// <summary>
    /// Namespace declarations owned by the element.
    /// </summary>
    public OdfNamespaceDeclaration[] NamespaceDeclarations = [];
    /// <summary>
    /// All element attributes in source order, including namespace declarations.
    /// </summary>
    public OdfAttribute[] Attributes = [];
    /// <summary>
    /// Child elements in source order.
    /// </summary>
    public OdfManifestNode[] Children = [];
    /// <summary>
    /// The exact child XML tokens in source order, including text, comments, and processing instructions.
    /// </summary>
    public string[] OrderedContentXml = [];
    /// <summary>
    /// The child-element index for each ordered content token, or <c>-1</c> for non-element content.
    /// </summary>
    public int[] OrderedContentElementIndexes = [];
    /// <summary>
    /// The concatenated text content of the element.
    /// </summary>
    public string TextContent = "";
    /// <summary>
    /// The preserved XML representation of this element.
    /// </summary>
    public string OriginalXml = "";
    /// <summary>
    /// Whether the element belongs to the standard manifest namespace.
    /// </summary>
    public bool IsManifestElement;
}
