using System.Collections.Generic;

namespace geo;

internal class RTreeNode
{
    internal Envelope Bounds = new Envelope();
    internal RTreeNode? Parent;
    internal List<RTreeNode> Children = new List<RTreeNode>();
    internal List<PackedItem> Items = new List<PackedItem>();
    internal bool Leaf;
    internal int Order;

    internal RTreeNode(bool leaf, int order)
    {
        Leaf = leaf;
        Order = order;
    }

    internal bool IsLeafNode()
    {
        return Leaf;
    }

    internal int GetEntryCount()
    {
        return Leaf ? Items.Count : Children.Count;
    }

    internal void RecalculateBounds()
    {
        Envelope bounds = EnvelopeAlgorithms.CreateEmptyEnvelope();
        int index = 0;
        if (Leaf)
        {
            while (index < Items.Count)
            {
                bounds = EnvelopeAlgorithms.ExpandEnvelopeByEnvelope(
                    bounds, Items[index].Item.Envelope);
                index++;
            }
        }
        else
        {
            while (index < Children.Count)
            {
                bounds = EnvelopeAlgorithms.ExpandEnvelopeByEnvelope(
                    bounds, Children[index].Bounds);
                index++;
            }
        }
        Bounds = bounds;
    }
}
