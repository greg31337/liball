namespace chart;
/// <summary>
/// Represents a pie layout used by the chart model or rendering pipeline.
/// </summary>
public class ChartPieLayout : IChartPieLayout
{
    internal double CenterX;
    internal double CenterY;
    internal ChartPieSlice[] Slices;
    internal int SliceCount;
    /// <summary>
    /// Initializes a pie layout from the supplied center x, center y, and slices.
    /// </summary>
    /// <param name = "centerX">The center x.</param>
    /// <param name = "centerY">The center y.</param>
    /// <param name = "slices">The slices.</param>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartPieLayout(double centerX, double centerY, ChartPieSlice[] slices)
    {
        ChartLinearScale.ValidateFinite(centerX, "centerX");
        ChartLinearScale.ValidateFinite(centerY, "centerY");
        if (slices == null)
        {
            throw new System.ArgumentNullException("slices");
        }

        if (slices.Length == 0)
        {
            throw new System.ArgumentException("Pie layout requires at least one slice.");
        }

        CenterX = centerX;
        CenterY = centerY;
        Slices = CopySlices(slices);
        SliceCount = slices.Length;
    }

    /// <summary>
    /// Returns the requested slice.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting pie slice.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartPieSlice SliceAt(int index)
    {
        if (index < 0 || index >= SliceCount)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return Slices[index];
    }

    internal static ChartPieSlice[] CopySlices(ChartPieSlice[] slices)
    {
        if (slices == null)
        {
            throw new System.ArgumentNullException("slices");
        }

        ChartPieSlice[] copy = new ChartPieSlice[slices.Length];
        int index = 0;
        while (index < slices.Length)
        {
            if (slices[index] == null)
            {
                throw new System.ArgumentException("Pie slices cannot contain null values.");
            }

            copy[index] = slices[index].Copy();
            index = index + 1;
        }

        return copy;
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

    /// <summary>Returns the current slices.</summary>
    /// <returns>The current field value.</returns>
    public ChartPieSlice[] GetSlices()
    {
        return Slices;
    }

    /// <summary>Replaces the current slices.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetSlices(ChartPieSlice[] value)
    {
        Slices = value;
    }

    /// <summary>Returns the current slice count.</summary>
    /// <returns>The current field value.</returns>
    public int GetSliceCount()
    {
        return SliceCount;
    }

    /// <summary>Replaces the current slice count.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetSliceCount(int value)
    {
        SliceCount = value;
    }
}
