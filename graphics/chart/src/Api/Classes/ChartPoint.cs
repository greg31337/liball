namespace chart;
/// <summary>
/// Represents a point used by the chart model or rendering pipeline.
/// </summary>
public class ChartPoint : IChartPoint
{
    internal double X;
    internal double Y;
    /// <summary>
    /// Initializes a point from the supplied x and y.
    /// </summary>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    public ChartPoint(double x, double y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartPoint Copy()
    {
        return new ChartPoint(X, Y);
    }

    /// <summary>
    /// Applies the distance to operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "point">The point.</param>
    /// <returns>The resulting double.</returns>
    public double DistanceTo(ChartPoint point)
    {
        double dx = point.X - X;
        double dy = point.Y - Y;
        return System.Math.Sqrt(dx * dx + dy * dy);
    }

    internal static ChartPoint Add(ChartPoint left, ChartPoint right)
    {
        return new ChartPoint(left.X + right.X, left.Y + right.Y);
    }

    internal static ChartPoint Subtract(ChartPoint left, ChartPoint right)
    {
        return new ChartPoint(left.X - right.X, left.Y - right.Y);
    }

    internal static ChartPoint Scale(ChartPoint point, double factor)
    {
        return new ChartPoint(point.X * factor, point.Y * factor);
    }

    internal static ChartPoint Lerp(ChartPoint start, ChartPoint end, double amount)
    {
        double x = start.X + (end.X - start.X) * amount;
        double y = start.Y + (end.Y - start.Y) * amount;
        return new ChartPoint(x, y);
    }

    /// <summary>Returns the current x.</summary>
    /// <returns>The current field value.</returns>
    public double GetX()
    {
        return X;
    }

    /// <summary>Replaces the current x.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetX(double value)
    {
        X = value;
    }

    /// <summary>Returns the current y.</summary>
    /// <returns>The current field value.</returns>
    public double GetY()
    {
        return Y;
    }

    /// <summary>Replaces the current y.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetY(double value)
    {
        Y = value;
    }
}
