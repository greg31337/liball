namespace odf;

/// <summary>
/// Identifies a node in the universal XML tree.
/// </summary>
public enum OdfNodeKind
{
    /// <summary>
    /// The node kind is absent or unrecognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// An XML element node.
    /// </summary>
    Element = 1,
    /// <summary>
    /// An XML text node, including CDATA-equivalent text.
    /// </summary>
    Text = 2,
    /// <summary>
    /// An XML comment node.
    /// </summary>
    Comment = 3,
    /// <summary>
    /// An XML processing-instruction node.
    /// </summary>
    ProcessingInstruction = 4
}
