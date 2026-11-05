namespace xml;

/// <summary>
/// Carries an ordered selection of XML nodes.
/// </summary>
public class XmlNodeSelection
{
    /// <summary>
    /// The document that owns the selected nodes.
    /// </summary>
    public XmlDocument? Document;
    /// <summary>
    /// The selected nodes in document order.
    /// </summary>
    public XmlNode[]? Nodes;
    /// <summary>
    /// The range from which the selection was created.
    /// </summary>
    public XmlRange? Range;
    /// <summary>
    /// The document mutation version at selection time.
    /// </summary>
    public long MutationVersion;
    /// <summary>
    /// Whether the selection is a frozen snapshot.
    /// </summary>
    public bool IsFrozen;
}

