namespace chart;
/// <summary>
/// Represents a layout result used by the chart model or rendering pipeline.
/// </summary>
public class ChartLayoutResult : IChartLayoutResult
{
    internal ChartRect FigureRect;
    internal ChartRect ContentRect;
    internal ChartRect PlotArea;
    internal ChartLayoutItem[] Items;
    internal int ItemCount;
    /// <summary>
    /// Initializes a layout result from the supplied figure rect, content rect, and plot area.
    /// </summary>
    /// <param name = "figureRect">The figure rect.</param>
    /// <param name = "contentRect">The content rect.</param>
    /// <param name = "plotArea">The rectangle available for plotting data.</param>
    public ChartLayoutResult(ChartRect figureRect, ChartRect contentRect, ChartRect plotArea)
    {
        ChartAxis.ValidatePlotArea(figureRect);
        ChartAxis.ValidatePlotArea(contentRect);
        ChartAxis.ValidatePlotArea(plotArea);
        FigureRect = ChartPlot.CopyRect(figureRect);
        ContentRect = ChartPlot.CopyRect(contentRect);
        PlotArea = ChartPlot.CopyRect(plotArea);
        Items = new ChartLayoutItem[8];
        ItemCount = 0;
    }

    /// <summary>
    /// Adds the supplied item to this object.
    /// </summary>
    /// <param name = "kind">The kind.</param>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "bounds">The rectangle that constrains the generated layout or drawing.</param>
    /// <param name = "side">The side.</param>
    /// <returns>The resulting layout item.</returns>
    public ChartLayoutItem AddItem(ChartLayoutItemKind kind, string id, ChartRect bounds, ChartSide side)
    {
        EnsureItemCapacity(ItemCount + 1);
        Items[ItemCount] = new ChartLayoutItem(kind, id, bounds, side);
        ItemCount = ItemCount + 1;
        return Items[ItemCount - 1];
    }

    /// <summary>
    /// Returns the item at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting layout item.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartLayoutItem ItemAt(int index)
    {
        if (index < 0 || index >= ItemCount)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return Items[index];
    }

    /// <summary>
    /// Returns the zero-based item index.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <returns>The resulting int.</returns>
    public int ItemIndex(string id)
    {
        ChartPlot.ValidateId(id);
        int index = 0;
        while (index < ItemCount)
        {
            if (Items[index].Id == id)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    /// <summary>
    /// Applies the item by id operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <returns>The resulting layout item.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    public ChartLayoutItem ItemById(string id)
    {
        int index = ItemIndex(id);
        if (index < 0)
        {
            throw new System.ArgumentException("Layout item was not found.");
        }

        return Items[index];
    }

    /// <summary>
    /// Adds the supplied diagnostic overlay to this object.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    public void AddDiagnosticOverlay(IChartRenderTarget scene)
    {
        ChartDiagnosticsOperations.AddLayoutOverlay(scene, this);
    }

    /// <summary>
    /// Adds the supplied diagnostic overlay to this object.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name = "idPrefix">The prefix used to create stable identifiers for generated items.</param>
    public void AddDiagnosticOverlayWithPrefix(IChartRenderTarget scene, string idPrefix)
    {
        ChartDiagnosticsOperations.AddLayoutOverlay(scene, this, idPrefix);
    }

    /// <summary>
    /// Creates a deterministic text trace of the retained drawing commands.
    /// </summary>
    /// <returns>The converted string.</returns>
    public string ToTraceString()
    {
        return ChartDiagnosticsOperations.LayoutTrace(this);
    }

    internal void EnsureItemCapacity(int capacity)
    {
        if (capacity <= Items.Length)
        {
            return;
        }

        int newCapacity = ChartArrayUtilities.GrowthCapacity(Items.Length, capacity);
        ChartLayoutItem[] items = new ChartLayoutItem[newCapacity];
        System.Array.Copy(Items, items, ItemCount);
        Items = items;
    }

    /// <summary>Returns the current figure rect.</summary>
    /// <returns>The current field value.</returns>
    public ChartRect GetFigureRect()
    {
        return FigureRect;
    }

    /// <summary>Replaces the current figure rect.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetFigureRect(ChartRect value)
    {
        FigureRect = value;
    }

    /// <summary>Returns the current content rect.</summary>
    /// <returns>The current field value.</returns>
    public ChartRect GetContentRect()
    {
        return ContentRect;
    }

    /// <summary>Replaces the current content rect.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetContentRect(ChartRect value)
    {
        ContentRect = value;
    }

    /// <summary>Returns the current plot area.</summary>
    /// <returns>The current field value.</returns>
    public ChartRect GetPlotArea()
    {
        return PlotArea;
    }

    /// <summary>Replaces the current plot area.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetPlotArea(ChartRect value)
    {
        PlotArea = value;
    }

    /// <summary>Returns the current items.</summary>
    /// <returns>The current field value.</returns>
    public ChartLayoutItem[] GetItems()
    {
        return Items;
    }

    /// <summary>Replaces the current items.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetItems(ChartLayoutItem[] value)
    {
        Items = value;
    }

    /// <summary>Returns the current item count.</summary>
    /// <returns>The current field value.</returns>
    public int GetItemCount()
    {
        return ItemCount;
    }

    /// <summary>Replaces the current item count.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetItemCount(int value)
    {
        ItemCount = value;
    }
}
