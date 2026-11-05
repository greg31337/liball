namespace svg;

internal class SvgStrokePrepared
{
    internal SvgStrokeError Error;
    internal SvgPathData? Outline;
    internal SvgMatrix? OutlineTransform;

    internal SvgStrokePrepared(SvgStrokeError error, SvgPathData? outline, SvgMatrix? transform)
    {
        Error = error;
        Outline = outline;
        OutlineTransform = transform;
    }
}
