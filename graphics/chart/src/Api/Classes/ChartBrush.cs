namespace chart;
/// <summary>Stores a deterministic linked-brushing geometry and row snapshot.</summary>
public class ChartBrush : IChartBrush
{
    internal string BrushId;
    internal string SourceId;
    internal string LinkGroup;
    internal ChartRect Bounds;
    internal int[] Rows;

    /// <summary>Initializes a data-space brush for a source view and linked-selection group.</summary>
    /// <param name = "brushId">The stable brush identifier.</param>
    /// <param name = "sourceId">The stable source view identifier.</param>
    /// <param name = "linkGroup">The stable linked-selection group.</param>
    /// <param name = "bounds">The positive data-space brush bounds.</param>
    public ChartBrush(string brushId, string sourceId, string linkGroup, ChartRect bounds)
    {
        ChartPlot.ValidateId(brushId);
        ChartPlot.ValidateId(sourceId);
        ChartPlot.ValidateId(linkGroup);
        ChartAxis.ValidatePlotArea(bounds);
        BrushId = brushId;
        SourceId = sourceId;
        LinkGroup = linkGroup;
        Bounds = ChartPlot.CopyRect(bounds);
        Rows = new int[0];
    }

    /// <summary>Replaces the nonnegative source row indices contained by the brush.</summary>
    /// <param name = "rows">The source row indices.</param>
    public void SetRows(int[] rows)
    {
        Rows = ChartSelection.NormalizeRows(rows);
    }

    /// <summary>Determines whether a data-space point is inside the brush bounds.</summary>
    /// <param name = "x">The finite horizontal value.</param>
    /// <param name = "y">The finite vertical value.</param>
    /// <returns><c>true</c> when the point is inside the brush.</returns>
    public bool ContainsPoint(double x, double y)
    {
        ChartRenderCommand.ValidateFinitePair(x, y, "point");
        return x >= Bounds.Left() && x <= Bounds.Right() && y >= Bounds.Top() && y <= Bounds.Bottom();
    }

    /// <summary>Returns the brushed rows in ascending order.</summary>
    /// <returns>An independent row-index array.</returns>
    public int[] CopyRows()
    {
        return ChartSelection.CopyRows(Rows);
    }

    /// <summary>Creates an independent brush snapshot.</summary>
    /// <returns>The copied brush.</returns>
    public ChartBrush Copy()
    {
        ChartBrush copy = new ChartBrush(BrushId, SourceId, LinkGroup, Bounds);
        copy.Rows = ChartSelection.CopyRows(Rows);
        return copy;
    }

    /// <summary>Returns the stable brush identifier.</summary>
    /// <returns>The brush identifier.</returns>
    public string GetBrushId()
    {
        return BrushId;
    }

    /// <summary>Returns the source view identifier.</summary>
    /// <returns>The source view identifier.</returns>
    public string GetSourceId()
    {
        return SourceId;
    }

    /// <summary>Returns the linked-selection group.</summary>
    /// <returns>The linked-selection group.</returns>
    public string GetLinkGroup()
    {
        return LinkGroup;
    }

    /// <summary>Returns an independent copy of the data-space brush bounds.</summary>
    /// <returns>The copied brush bounds.</returns>
    public ChartRect GetBounds()
    {
        return ChartPlot.CopyRect(Bounds);
    }
}
