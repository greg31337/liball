namespace svg;

internal class SvgViewportDimension
{
    public bool Success;
    public bool Auto;
    public SvgLength? Length;
    public SvgSourceSpan? Source;

    public SvgViewportDimension(bool success, bool auto, SvgLength? length, SvgSourceSpan? source)
    {
        Success = success;
        Auto = auto;
        Length = length;
        Source = source;
    }
}

internal class SvgViewportFragmentOverride
{
    public bool Success;
    public SvgViewportFragmentKind Kind;
    public SvgNode? Target;
    public SvgNode? ViewNode;
    public bool HasViewBox;
    public SvgViewBox? ViewBox;
    public bool HasAspectRatio;
    public SvgAspectRatio? AspectRatio;
    public SvgTransformList? Transform;

    public SvgViewportFragmentOverride()
    {
        Success = true;
        Kind = SvgViewportFragmentKind.None;
        Target = null;
        ViewNode = null;
        HasViewBox = false;
        ViewBox = null;
        HasAspectRatio = false;
        AspectRatio = null;
        Transform = null;
    }
}
