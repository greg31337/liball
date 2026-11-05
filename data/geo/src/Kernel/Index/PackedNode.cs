namespace geo;

internal class PackedItem
{
    internal SpatialIndexItem Item = new SpatialIndexItem();
    internal double CenterX;
    internal double CenterY;
    internal int Order;
    internal int SortPosition;
}

internal class PackedNode
{
    internal Envelope Bounds;
    internal PackedNode[] Children;
    internal PackedItem[] Items;
    internal double CenterX;
    internal double CenterY;
    internal int Level;
    internal int Order;
    internal int SortPosition;

    internal PackedNode(
        Envelope bounds, PackedNode[] children, PackedItem[] items,
        int level, int order)
    {
        Bounds = bounds;
        Children = children;
        Items = items;
        Level = level;
        Order = order;
        Coordinate? center = EnvelopeAlgorithms.GetEnvelopeCenter(bounds);
        if (center == null)
        {
            throw new SpatialIndexException(
                "A packed node cannot have empty bounds.");
        }
        CenterX = center.X;
        CenterY = center.Y;
    }

    internal bool IsLeafNode()
    {
        return Level == 0;
    }
}
