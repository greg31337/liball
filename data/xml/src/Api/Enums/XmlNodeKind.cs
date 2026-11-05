namespace xml;

/// <summary>
/// Identifies a mutable XML tree node kind.
/// </summary>
public enum XmlNodeKind
{
    /// <summary>
    /// Identifies a document node.
    /// </summary>
    Document = 0,
    /// <summary>
    /// Identifies a document fragment node.
    /// </summary>
    DocumentFragment = 1,
    /// <summary>
    /// Identifies an element node.
    /// </summary>
    Element = 2,
    /// <summary>
    /// Identifies an attribute node.
    /// </summary>
    Attribute = 3,
    /// <summary>
    /// Identifies a namespace node.
    /// </summary>
    Namespace = 4,
    /// <summary>
    /// Identifies a text node.
    /// </summary>
    Text = 5,
    /// <summary>
    /// Identifies a CDATA section node.
    /// </summary>
    CData = 6,
    /// <summary>
    /// Identifies an entity reference node.
    /// </summary>
    EntityReference = 7,
    /// <summary>
    /// Identifies a processing instruction node.
    /// </summary>
    ProcessingInstruction = 8,
    /// <summary>
    /// Identifies a comment node.
    /// </summary>
    Comment = 9,
    /// <summary>
    /// Identifies a document type node.
    /// </summary>
    DocumentType = 10
}
