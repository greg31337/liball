namespace chart;
/// <summary>
/// Represents a rect used by the chart model or rendering pipeline.
/// </summary>
public class ChartRect : IChartRect
{
    internal double X;
    internal double Y;
    internal double Width;
    internal double Height;
    /// <summary>
    /// Initializes a rect from the supplied x, y, width, and height.
    /// </summary>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    /// <param name = "width">The width in pixels or chart units, which must be positive.</param>
    /// <param name = "height">The height in pixels or chart units, which must be positive.</param>
    public ChartRect(double x, double y, double width, double height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Applies the left operation to the supplied data and returns the computed result.
    /// </summary>
    /// <returns>The resulting double.</returns>
    public double Left()
    {
        return X;
    }

    /// <summary>
    /// Applies the right operation to the supplied data and returns the computed result.
    /// </summary>
    /// <returns>The resulting double.</returns>
    public double Right()
    {
        return X + Width;
    }

    /// <summary>
    /// Converts this value to p.
    /// </summary>
    /// <returns>The converted double.</returns>
    public double Top()
    {
        return Y;
    }

    /// <summary>
    /// Applies the bottom operation to the supplied data and returns the computed result.
    /// </summary>
    /// <returns>The resulting double.</returns>
    public double Bottom()
    {
        return Y + Height;
    }

    /// <summary>
    /// Applies the center operation to the supplied data and returns the computed result.
    /// </summary>
    /// <returns>The resulting point.</returns>
    public ChartPoint Center()
    {
        return new ChartPoint(X + Width * 0.5, Y + Height * 0.5);
    }

    /// <summary>
    /// Determines whether this value is empty.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public bool IsEmpty()
    {
        return Width <= 0.0 || Height <= 0.0;
    }

    /// <summary>
    /// Determines whether this value contains point.
    /// </summary>
    /// <param name = "point">The point.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public bool ContainsPoint(ChartPoint point)
    {
        return point.X >= Left() && point.X <= Right() && point.Y >= Top() && point.Y <= Bottom();
    }

    /// <summary>
    /// Computes the intersection with s using the supplied data and configuration.
    /// </summary>
    /// <param name = "rect">The rect.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public bool Intersects(ChartRect rect)
    {
        return Left() < rect.Right() && Right() > rect.Left() && Top() < rect.Bottom() && Bottom() > rect.Top();
    }

    /// <summary>
    /// Computes the intersection with ion using the supplied data and configuration.
    /// </summary>
    /// <param name = "rect">The rect.</param>
    /// <returns>The resulting rect.</returns>
    public ChartRect Intersection(ChartRect rect)
    {
        double left = System.Math.Max(Left(), rect.Left());
        double top = System.Math.Max(Top(), rect.Top());
        double right = System.Math.Min(Right(), rect.Right());
        double bottom = System.Math.Min(Bottom(), rect.Bottom());
        if (right <= left || bottom <= top)
        {
            return new ChartRect(left, top, 0.0, 0.0);
        }

        return new ChartRect(left, top, right - left, bottom - top);
    }

    /// <summary>
    /// Computes the union with the configured value using the supplied data and configuration.
    /// </summary>
    /// <param name = "rect">The rect.</param>
    /// <returns>The resulting rect.</returns>
    public ChartRect Union(ChartRect rect)
    {
        double left = System.Math.Min(Left(), rect.Left());
        double top = System.Math.Min(Top(), rect.Top());
        double right = System.Math.Max(Right(), rect.Right());
        double bottom = System.Math.Max(Bottom(), rect.Bottom());
        return new ChartRect(left, top, right - left, bottom - top);
    }

    /// <summary>
    /// Applies the translate operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "dx">The dx.</param>
    /// <param name = "dy">The dy.</param>
    /// <returns>The resulting rect.</returns>
    public ChartRect Translate(double dx, double dy)
    {
        return new ChartRect(X + dx, Y + dy, Width, Height);
    }

    /// <summary>
    /// Expands the configured value using the supplied data and configuration.
    /// </summary>
    /// <param name = "dx">The dx.</param>
    /// <param name = "dy">The dy.</param>
    /// <returns>The resulting rect.</returns>
    public ChartRect Inflate(double dx, double dy)
    {
        return new ChartRect(X - dx, Y - dy, Width + dx * 2.0, Height + dy * 2.0);
    }

    /// <summary>
    /// Shrinks the configured value using the supplied data and configuration.
    /// </summary>
    /// <param name = "dx">The dx.</param>
    /// <param name = "dy">The dy.</param>
    /// <returns>The resulting rect.</returns>
    public ChartRect Deflate(double dx, double dy)
    {
        return Inflate(0.0 - dx, 0.0 - dy);
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

    /// <summary>Returns the current width.</summary>
    /// <returns>The current field value.</returns>
    public double GetWidth()
    {
        return Width;
    }

    /// <summary>Replaces the current width.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetWidth(double value)
    {
        Width = value;
    }

    /// <summary>Returns the current height.</summary>
    /// <returns>The current field value.</returns>
    public double GetHeight()
    {
        return Height;
    }

    /// <summary>Replaces the current height.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetHeight(double value)
    {
        Height = value;
    }
}
