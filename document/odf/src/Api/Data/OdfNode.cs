namespace odf;

/// <summary>
/// Describes the kind of a universal XML tree node.
/// </summary>
public class OdfNode
{
    /// <summary>
    /// The qualified element or processing-instruction name, when applicable.
    /// </summary>
    public OdfQualifiedName? Name;
    /// <summary>
    /// The containing element, or <c>null</c> for a top-level node.
    /// </summary>
    public OdfNode? Parent;
    /// <summary>
    /// The node kind.
    /// </summary>
    public OdfNodeKind Kind;
    /// <summary>
    /// Namespace declarations owned by this node.
    /// </summary>
    public OdfNamespaceDeclaration[] NamespaceDeclarations = [];
    /// <summary>
    /// Attributes owned by this node.
    /// </summary>
    public OdfAttribute[] Attributes = [];
    /// <summary>
    /// Child nodes in source order, including text, comments, and processing instructions.
    /// </summary>
    public OdfNode[] Children = [];
    /// <summary>
    /// The exact text, comment, or processing-instruction data for this node.
    /// </summary>
    public string Value = "";
    /// <summary>
    /// The XML ID declared directly on this element, or an empty string.
    /// </summary>
    public string XmlId = "";
    /// <summary>
    /// The source location of this node, when available.
    /// </summary>
    public OdfLocation Location = new OdfLocation();
    /// <summary>
    /// Whether this text node contains whitespace preserved by XML space rules.
    /// </summary>
    public bool IsSignificantWhitespace;
    /// <summary>
    /// Whether this element used XML empty-element syntax when loaded.
    /// </summary>
    public bool WasEmptyElement;
}
