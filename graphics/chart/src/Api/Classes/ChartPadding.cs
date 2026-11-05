namespace chart;
/// <summary>
/// Represents a padding used by the chart model or rendering pipeline.
/// </summary>
public class ChartPadding : IChartPadding
{
    internal double Left;
    internal double Top;
    internal double Right;
    internal double Bottom;
    /// <summary>
    /// Initializes a padding from the supplied left, top, right, and bottom.
    /// </summary>
    /// <param name = "left">The left.</param>
    /// <param name = "top">The top.</param>
    /// <param name = "right">The right.</param>
    /// <param name = "bottom">The bottom.</param>
    public ChartPadding(double left, double top, double right, double bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    internal static ChartPadding All(double value)
    {
        return new ChartPadding(value, value, value, value);
    }

    /// <summary>
    /// Applies the horizontal operation to the supplied data and returns the computed result.
    /// </summary>
    /// <returns>The resulting double.</returns>
    public double Horizontal()
    {
        return Left + Right;
    }

    /// <summary>
    /// Applies the vertical operation to the supplied data and returns the computed result.
    /// </summary>
    /// <returns>The resulting double.</returns>
    public double Vertical()
    {
        return Top + Bottom;
    }

    /// <summary>
    /// Shrinks rect using the supplied data and configuration.
    /// </summary>
    /// <param name = "rect">The rect.</param>
    /// <returns>The resulting rect.</returns>
    public ChartRect DeflateRect(ChartRect rect)
    {
        return new ChartRect(rect.X + Left, rect.Y + Top, rect.Width - Horizontal(), rect.Height - Vertical());
    }

    /// <summary>
    /// Expands rect using the supplied data and configuration.
    /// </summary>
    /// <param name = "rect">The rect.</param>
    /// <returns>The resulting rect.</returns>
    public ChartRect InflateRect(ChartRect rect)
    {
        return new ChartRect(rect.X - Left, rect.Y - Top, rect.Width + Horizontal(), rect.Height + Vertical());
    }

    /// <summary>Returns the current left.</summary>
    /// <returns>The current field value.</returns>
    public double GetLeft()
    {
        return Left;
    }

    /// <summary>Replaces the current left.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetLeft(double value)
    {
        Left = value;
    }

    /// <summary>Returns the current top.</summary>
    /// <returns>The current field value.</returns>
    public double GetTop()
    {
        return Top;
    }

    /// <summary>Replaces the current top.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetTop(double value)
    {
        Top = value;
    }

    /// <summary>Returns the current right.</summary>
    /// <returns>The current field value.</returns>
    public double GetRight()
    {
        return Right;
    }

    /// <summary>Replaces the current right.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetRight(double value)
    {
        Right = value;
    }

    /// <summary>Returns the current bottom.</summary>
    /// <returns>The current field value.</returns>
    public double GetBottom()
    {
        return Bottom;
    }

    /// <summary>Replaces the current bottom.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetBottom(double value)
    {
        Bottom = value;
    }
}
