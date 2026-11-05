namespace chart;
/// <summary>
/// Represents a layout item used by the chart model or rendering pipeline.
/// </summary>
public class ChartLayoutItem : IChartLayoutItem
{
    internal ChartLayoutItemKind Kind;
    internal string Id;
    internal ChartRect Bounds;
    internal ChartSide Side;
    /// <summary>
    /// Initializes a layout item from the supplied kind, id, bounds, and side.
    /// </summary>
    /// <param name = "kind">The kind.</param>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "bounds">The rectangle that constrains the generated layout or drawing.</param>
    /// <param name = "side">The side.</param>
    public ChartLayoutItem(ChartLayoutItemKind kind, string id, ChartRect bounds, ChartSide side)
    {
        ChartPlot.ValidateId(id);
        ChartAxis.ValidatePlotArea(bounds);
        Kind = kind;
        Id = id;
        Bounds = ChartPlot.CopyRect(bounds);
        Side = side;
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartLayoutItem Copy()
    {
        return new ChartLayoutItem(Kind, Id, Bounds, Side);
    }

    /// <summary>Returns the current kind.</summary>
    /// <returns>The current field value.</returns>
    public ChartLayoutItemKind GetKind()
    {
        return Kind;
    }

    /// <summary>Replaces the current kind.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetKind(ChartLayoutItemKind value)
    {
        Kind = value;
    }

    /// <summary>Returns the current id.</summary>
    /// <returns>The current field value.</returns>
    public string GetId()
    {
        return Id;
    }

    /// <summary>Replaces the current id.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetId(string value)
    {
        Id = value;
    }

    /// <summary>Returns the current bounds.</summary>
    /// <returns>The current field value.</returns>
    public ChartRect GetBounds()
    {
        return Bounds;
    }

    /// <summary>Replaces the current bounds.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetBounds(ChartRect value)
    {
        Bounds = value;
    }

    /// <summary>Returns the current side.</summary>
    /// <returns>The current field value.</returns>
    public ChartSide GetSide()
    {
        return Side;
    }

    /// <summary>Replaces the current side.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetSide(ChartSide value)
    {
        Side = value;
    }
}
