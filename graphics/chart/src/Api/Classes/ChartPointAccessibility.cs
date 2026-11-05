namespace chart;
/// <summary>
/// Represents a point accessibility used by the chart model or rendering pipeline.
/// </summary>
public class ChartPointAccessibility : IChartPointAccessibility
{
    internal string PointId;
    internal string Description;
    internal int RowIndex;
    /// <summary>
    /// Initializes a point accessibility from the supplied point id, description, and row index.
    /// </summary>
    /// <param name = "pointId">The point id.</param>
    /// <param name = "description">The description.</param>
    /// <param name = "rowIndex">The zero-based row index.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartPointAccessibility(string pointId, string description, int rowIndex)
    {
        ChartPlot.ValidateId(pointId);
        ChartTextBlock.ValidateText(description, "description");
        if (rowIndex < 0)
        {
            throw new System.ArgumentOutOfRangeException("rowIndex");
        }

        PointId = pointId;
        Description = description;
        RowIndex = rowIndex;
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartPointAccessibility Copy()
    {
        return new ChartPointAccessibility(PointId, Description, RowIndex);
    }

    /// <summary>Returns the current point id.</summary>
    /// <returns>The current field value.</returns>
    public string GetPointId()
    {
        return PointId;
    }

    /// <summary>Replaces the current point id.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetPointId(string value)
    {
        PointId = value;
    }

    /// <summary>Returns the current description.</summary>
    /// <returns>The current field value.</returns>
    public string GetDescription()
    {
        return Description;
    }

    /// <summary>Replaces the current description.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetDescription(string value)
    {
        Description = value;
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
}
