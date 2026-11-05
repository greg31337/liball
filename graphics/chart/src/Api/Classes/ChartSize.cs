namespace chart;
/// <summary>
/// Represents a size used by the chart model or rendering pipeline.
/// </summary>
public class ChartSize : IChartSize
{
    internal double Width;
    internal double Height;
    /// <summary>
    /// Initializes a size from the supplied width and height.
    /// </summary>
    /// <param name = "width">The width in pixels or chart units, which must be positive.</param>
    /// <param name = "height">The height in pixels or chart units, which must be positive.</param>
    public ChartSize(double width, double height)
    {
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Applies the area operation to the supplied data and returns the computed result.
    /// </summary>
    /// <returns>The resulting double.</returns>
    public double Area()
    {
        return Width * Height;
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
    /// Applies the scale operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "factor">The factor.</param>
    /// <returns>The resulting size.</returns>
    public ChartSize Scale(double factor)
    {
        return new ChartSize(Width * factor, Height * factor);
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
