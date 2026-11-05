namespace icc;

internal class GamutClosestPoint
{
    internal double X;
    internal double Y;
    internal double Z;
    internal double SquaredDistance;

    internal GamutClosestPoint()
    {
        X = 0.0;
        Y = 0.0;
        Z = 0.0;
        SquaredDistance = double.MaxValue;
    }
}
