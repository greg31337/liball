namespace svg;

internal class SvgUseDimension
{
    public bool Success;
    public bool Auto;
    public bool Authored;
    public SvgLength? Length;
    public SvgSourceSpan? Source;

    public SvgUseDimension(bool success, bool auto, bool authored, SvgLength? length, SvgSourceSpan? source)
    {
        Success = success;
        Auto = auto;
        Authored = authored;
        Length = length;
        Source = source;
    }
}

internal class SvgUsePending
{
    public SvgNode Source;
    public SvgNode ShadowNode;
    public SvgUseInstance? Parent;
    public string Path;
    public SvgComputedStyle? ParentStyle;
    public SvgLengthContext LengthContext;
    public SvgUsePaintContext? PaintContext;
    public System.Collections.Generic.List<SvgNode> ActiveTargets;
    public int ReferenceDepth;
    public bool InstanceRoot;
    public bool ShadowAttachment;
    public SvgNode ShadowHostSource;

    public SvgUsePending(SvgNode source, SvgNode shadowNode, SvgUseInstance? parent, string path, SvgComputedStyle? parentStyle, SvgLengthContext lengthContext, SvgUsePaintContext? paintContext, System.Collections.Generic.List<SvgNode> activeTargets, int depth, bool root, bool shadowAttachment, SvgNode shadowHost)
    {
        Source = source;
        ShadowNode = shadowNode;
        Parent = parent;
        Path = path;
        ParentStyle = parentStyle;
        LengthContext = lengthContext;
        PaintContext = paintContext;
        ActiveTargets = activeTargets;
        ReferenceDepth = depth;
        InstanceRoot = root;
        ShadowAttachment = shadowAttachment;
        ShadowHostSource = shadowHost;
    }
}
