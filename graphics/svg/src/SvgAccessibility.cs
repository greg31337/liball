namespace svg;

internal class SvgAccessibilityWork
{
    internal SvgAccessibleSemantics Result;
    internal SvgAccessibilityOptions Options;
    internal int Nodes;
    internal int Characters;
    internal bool Failed;
    internal System.Collections.Generic.HashSet<SvgNode> Naming;

    internal SvgAccessibilityWork(SvgAccessibleSemantics result, SvgAccessibilityOptions options)
    {
        Result = result;
        Options = options;
        Nodes = 0;
        Characters = 0;
        Failed = false;
        Naming = new System.Collections.Generic.HashSet<SvgNode>();
    }
}
