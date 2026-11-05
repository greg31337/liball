namespace svg;

internal class SvgAccessibilityTreeFrame
{
    internal SvgResolvedNode? Resolved;
    internal SvgAccessibilityTreeNode? AccessibleParent;
    internal SvgAccessibilityTreeNode? Created;
    internal int NextChild;
    internal bool Entered;

    internal SvgAccessibilityTreeFrame(SvgResolvedNode? resolved, SvgAccessibilityTreeNode? parent)
    {
        Resolved = resolved;
        AccessibleParent = parent;
        Created = null;
        NextChild = 0;
        Entered = false;
    }
}

internal class SvgAccessibilityTreeWork
{
    internal SvgAccessibilityTree Result;
    internal SvgAccessibilityTreeOptions Options;
    internal System.Text.StringBuilder Text;
    internal System.Collections.Generic.Dictionary<SvgResolvedNode, SvgFocusEntry> FocusEntries;
    internal System.Collections.Generic.Dictionary<SvgResolvedNode, SvgAccessibilityTreeNode> AccessibleNodes;
    internal SvgFocusOrderResult? Focus;

    internal SvgAccessibilityTreeWork(SvgAccessibilityTree result, SvgAccessibilityTreeOptions options)
    {
        Result = result;
        Options = options;
        Text = new System.Text.StringBuilder();
        FocusEntries = new System.Collections.Generic.Dictionary<SvgResolvedNode, SvgFocusEntry>();
        AccessibleNodes = new System.Collections.Generic.Dictionary<SvgResolvedNode, SvgAccessibilityTreeNode>();
        Focus = null;
    }
}
