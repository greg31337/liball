namespace svg;

/// <summary>Stores one source-located node and its document-order tie breaker.</summary>
internal class SvgSourceIndexEntry
{
    internal SvgNode Node;
    internal int Order;

    internal SvgSourceIndexEntry(SvgNode node, int order)
    {
        Node = node;
        Order = order;
    }
}
