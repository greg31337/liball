namespace chart;
/// <summary>Defines a deterministic linked-brushing geometry and row snapshot.</summary>
public interface IChartBrush
{
    /// <summary>Replaces the nonnegative source row indices contained by the brush.</summary>
    /// <param name = "rows">The source row indices.</param>
    void SetRows(int[] rows);
    /// <summary>Determines whether a data-space point is inside the brush bounds.</summary>
    /// <param name = "x">The finite horizontal value.</param>
    /// <param name = "y">The finite vertical value.</param>
    /// <returns><c>true</c> when the point is inside the brush.</returns>
    bool ContainsPoint(double x, double y);
    /// <summary>Returns the brushed rows in ascending order.</summary>
    /// <returns>An independent row-index array.</returns>
    int[] CopyRows();
    /// <summary>Creates an independent brush snapshot.</summary>
    /// <returns>The copied brush.</returns>
    ChartBrush Copy();
    /// <summary>Returns the stable brush identifier.</summary>
    /// <returns>The brush identifier.</returns>
    string GetBrushId();
    /// <summary>Returns the source view identifier.</summary>
    /// <returns>The source view identifier.</returns>
    string GetSourceId();
    /// <summary>Returns the linked-selection group.</summary>
    /// <returns>The linked-selection group.</returns>
    string GetLinkGroup();
    /// <summary>Returns an independent copy of the data-space brush bounds.</summary>
    /// <returns>The copied brush bounds.</returns>
    ChartRect GetBounds();
}
