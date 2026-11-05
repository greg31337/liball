namespace xml;

internal class XdmNamespaceCacheEntry
{
    internal XmlDocument? Document;
    internal long MutationVersion;
    internal XdmNode[] Nodes;

    internal XdmNamespaceCacheEntry(XmlDocument? document, long mutationVersion, XdmNode[] nodes)
    {
        Document = document;
        MutationVersion = mutationVersion;
        Nodes = nodes;
    }
}
