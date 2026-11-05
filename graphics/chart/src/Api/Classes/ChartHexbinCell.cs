namespace chart;
/// <summary>
/// Represents a hexbin cell used by the chart model or rendering pipeline.
/// </summary>
public class ChartHexbinCell : IChartHexbinCell
{
    internal double CenterX;
    internal double CenterY;
    internal int Count;
    /// <summary>
    /// Initializes a hexbin cell from the supplied center x and center y.
    /// </summary>
    /// <param name = "centerX">The center x.</param>
    /// <param name = "centerY">The center y.</param>
    public ChartHexbinCell(double centerX, double centerY)
    {
        ChartLinearScale.ValidateFinite(centerX, "centerX");
        ChartLinearScale.ValidateFinite(centerY, "centerY");
        CenterX = centerX;
        CenterY = centerY;
        Count = 0;
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartHexbinCell Copy()
    {
        ChartHexbinCell cell = new ChartHexbinCell(CenterX, CenterY);
        cell.Count = Count;
        return cell;
    }

    /// <summary>Returns the current center x.</summary>
    /// <returns>The current field value.</returns>
    public double GetCenterX()
    {
        return CenterX;
    }

    /// <summary>Replaces the current center x.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetCenterX(double value)
    {
        CenterX = value;
    }

    /// <summary>Returns the current center y.</summary>
    /// <returns>The current field value.</returns>
    public double GetCenterY()
    {
        return CenterY;
    }

    /// <summary>Replaces the current center y.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetCenterY(double value)
    {
        CenterY = value;
    }

    /// <summary>Returns the current count.</summary>
    /// <returns>The current field value.</returns>
    public int GetCount()
    {
        return Count;
    }

    /// <summary>Replaces the current count.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetCount(int value)
    {
        Count = value;
    }
}
