namespace cbor;

internal class CddlEmissionNodeFrame
{
    internal CddlNode Node;
    internal int Depth;

    internal CddlEmissionNodeFrame(CddlNode node, int depth)
    {
        Node = node;
        Depth = depth;
    }
}
