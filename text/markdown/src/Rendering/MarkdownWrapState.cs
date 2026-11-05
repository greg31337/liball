namespace markdown;

internal class MarkdownWrapState
{
    internal int Column;
    internal bool PendingSpace;

    internal MarkdownWrapState()
    {
        Column = 0;
        PendingSpace = false;
    }
}
