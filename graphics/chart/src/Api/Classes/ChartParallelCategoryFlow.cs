namespace chart;
/// <summary>
/// Represents a parallel category flow used by the chart model or rendering pipeline.
/// </summary>
public class ChartParallelCategoryFlow : IChartParallelCategoryFlow
{
    internal int RowIndex;
    internal int SourceBandIndex;
    internal int TargetBandIndex;
    internal double X1;
    internal double Y1;
    internal double X2;
    internal double Y2;
    /// <summary>
    /// Initializes a parallel category flow from the supplied row index, source band index, target band index, x1, y1, x2, and y2.
    /// </summary>
    /// <param name = "rowIndex">The zero-based row index.</param>
    /// <param name = "sourceBandIndex">The zero-based source band index.</param>
    /// <param name = "targetBandIndex">The zero-based target band index.</param>
    /// <param name = "x1">The x1.</param>
    /// <param name = "y1">The y1.</param>
    /// <param name = "x2">The x2.</param>
    /// <param name = "y2">The y2.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartParallelCategoryFlow(int rowIndex, int sourceBandIndex, int targetBandIndex, double x1, double y1, double x2, double y2)
    {
        if (rowIndex < 0 || sourceBandIndex < 0 || targetBandIndex < 0)
        {
            throw new System.ArgumentOutOfRangeException("rowIndex");
        }

        ChartLinearScale.ValidateFinite(x1, "x1");
        ChartLinearScale.ValidateFinite(y1, "y1");
        ChartLinearScale.ValidateFinite(x2, "x2");
        ChartLinearScale.ValidateFinite(y2, "y2");
        RowIndex = rowIndex;
        SourceBandIndex = sourceBandIndex;
        TargetBandIndex = targetBandIndex;
        X1 = x1;
        Y1 = y1;
        X2 = x2;
        Y2 = y2;
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartParallelCategoryFlow Copy()
    {
        return new ChartParallelCategoryFlow(RowIndex, SourceBandIndex, TargetBandIndex, X1, Y1, X2, Y2);
    }

    /// <summary>Returns the current row index.</summary>
    /// <returns>The current field value.</returns>
    public int GetRowIndex()
    {
        return RowIndex;
    }

    /// <summary>Replaces the current row index.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetRowIndex(int value)
    {
        RowIndex = value;
    }

    /// <summary>Returns the current source band index.</summary>
    /// <returns>The current field value.</returns>
    public int GetSourceBandIndex()
    {
        return SourceBandIndex;
    }

    /// <summary>Replaces the current source band index.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetSourceBandIndex(int value)
    {
        SourceBandIndex = value;
    }

    /// <summary>Returns the current target band index.</summary>
    /// <returns>The current field value.</returns>
    public int GetTargetBandIndex()
    {
        return TargetBandIndex;
    }

    /// <summary>Replaces the current target band index.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetTargetBandIndex(int value)
    {
        TargetBandIndex = value;
    }

    /// <summary>Returns the current x1.</summary>
    /// <returns>The current field value.</returns>
    public double GetX1()
    {
        return X1;
    }

    /// <summary>Replaces the current x1.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetX1(double value)
    {
        X1 = value;
    }

    /// <summary>Returns the current y1.</summary>
    /// <returns>The current field value.</returns>
    public double GetY1()
    {
        return Y1;
    }

    /// <summary>Replaces the current y1.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetY1(double value)
    {
        Y1 = value;
    }

    /// <summary>Returns the current x2.</summary>
    /// <returns>The current field value.</returns>
    public double GetX2()
    {
        return X2;
    }

    /// <summary>Replaces the current x2.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetX2(double value)
    {
        X2 = value;
    }

    /// <summary>Returns the current y2.</summary>
    /// <returns>The current field value.</returns>
    public double GetY2()
    {
        return Y2;
    }

    /// <summary>Replaces the current y2.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetY2(double value)
    {
        Y2 = value;
    }
}
