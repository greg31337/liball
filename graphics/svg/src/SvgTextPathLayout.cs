namespace svg;

internal class SvgTextPathFrame
{
    internal SvgPoint? Point;
    internal SvgVector? Tangent;
    internal SvgVector? Normal;
    internal double Distance;
    internal double ActualDistance;
    internal bool Wrapped;

    internal SvgTextPathFrame()
    {
        Point = null;
        Tangent = null;
        Normal = null;
        Distance = 0;
        ActualDistance = 0;
        Wrapped = false;
    }
}
