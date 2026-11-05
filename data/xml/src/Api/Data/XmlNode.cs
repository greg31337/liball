namespace xml;

/// <summary>
/// Carries one mutable XML information-set tree node.
/// </summary>
public class XmlNode
{
    /// <summary>
    /// The node kind.
    /// </summary>
    public XmlNodeKind Kind;
    /// <summary>
    /// The node name.
    /// </summary>
    public XmlName? Name;
    /// <summary>
    /// The node value.
    /// </summary>
    public string? Value;
    /// <summary>
    /// The parent node.
    /// </summary>
    public XmlNode? Parent;
    /// <summary>
    /// The owner document.
    /// </summary>
    public XmlDocument? OwnerDocument;
    /// <summary>
    /// The ordered child nodes.
    /// </summary>
    public XmlNode[]? Children;
    /// <summary>
    /// The ordered attribute nodes.
    /// </summary>
    public XmlNode[]? Attributes;
    /// <summary>
    /// The namespace bindings declared on the node.
    /// </summary>
    public XmlNamespaceBinding[]? NamespaceBindings;
    /// <summary>
    /// The original source range.
    /// </summary>
    public SourceRange? Range;
    /// <summary>
    /// The schema type annotation when the node has been assessed.
    /// </summary>
    public XmlSchemaTypeInfo? TypeInfo;
    /// <summary>
    /// The identity allocated by the owner document.
    /// </summary>
    public long Identity;
    /// <summary>
    /// The owner document mutation version observed by the node.
    /// </summary>
    public long MutationVersion;
    /// <summary>
    /// Whether mutation of the node is prohibited.
    /// </summary>
    public bool IsReadOnly;
    /// <summary>
    /// The outermost-to-innermost XInclude elements that produced this top-level included item.
    /// </summary>
    public XmlNode[]? IncludeHistory;
}
