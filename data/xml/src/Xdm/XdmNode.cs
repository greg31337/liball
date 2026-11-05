namespace xml;

internal class XdmNode
{
    internal XmlNode Node;
    internal XdmNodeKind Kind;
    internal XdmNode? ProjectedParent;
    internal string? ProjectedStringValue;
    internal bool HasProjectedParent;
    internal bool HasProjectedStringValue;

    internal XdmNode(XmlNode node, XdmNodeKind kind)
    {
        Node = node;
        Kind = kind;
        ProjectedParent = null;
        ProjectedStringValue = null;
        HasProjectedParent = false;
        HasProjectedStringValue = false;
    }
}
