namespace markdown;

internal class MarkdownEventCursorFrame
{
    internal MarkdownNode Node;
    internal int Depth;
    internal int ChildIndex;
    internal int NextChildIndex;
    internal bool Entered;

    internal MarkdownEventCursorFrame(MarkdownNode node, int depth, int childIndex)
    {
        Node = node;
        Depth = depth;
        ChildIndex = childIndex;
        NextChildIndex = 0;
        Entered = false;
    }
}
