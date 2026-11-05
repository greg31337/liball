namespace svg;

internal class SvgTypedAttributeCacheEntry
{
    public SvgNode Node;
    public string NamespaceUri;
    public string LocalName;
    public long Version;
    public SvgTypedAttributeValue Result;

    public SvgTypedAttributeCacheEntry(SvgNode node, string namespaceUri, string localName, SvgTypedAttributeValue result)
    {
        Node = node;
        NamespaceUri = namespaceUri;
        LocalName = localName;
        Version = node.Version;
        Result = result;
    }
}
