namespace svg;

internal class SvgPathEndpoint
{
    internal bool Success;
    internal double X;
    internal double Y;

    internal SvgPathEndpoint(bool success, double x, double y)
    {
        Success = success;
        X = x;
        Y = y;
    }
}
