namespace chart;
/// <summary>
/// Represents a sunburst layout used by the chart model or rendering pipeline.
/// </summary>
public class ChartSunburstLayout : IChartSunburstLayout
{
    internal double CenterX;
    internal double CenterY;
    internal ChartHierarchyArcItem[] Items;
    internal int ItemCount;
    /// <summary>
    /// Initializes a sunburst layout from the supplied center x, center y, and capacity.
    /// </summary>
    /// <param name = "centerX">The center x.</param>
    /// <param name = "centerY">The center y.</param>
    /// <param name = "capacity">The capacity.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartSunburstLayout(double centerX, double centerY, int capacity)
    {
        ChartPieChartsOperations.ValidateLayoutPoint(centerX, centerY);
        if (capacity <= 0)
        {
            throw new System.ArgumentOutOfRangeException("capacity");
        }

        CenterX = centerX;
        CenterY = centerY;
        Items = new ChartHierarchyArcItem[capacity];
        ItemCount = 0;
    }

    /// <summary>
    /// Adds the supplied item to this object.
    /// </summary>
    /// <param name = "item">The item.</param>
    /// <returns>The resulting hierarchy arc item.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartHierarchyArcItem AddItem(ChartHierarchyArcItem item)
    {
        if (item == null)
        {
            throw new System.ArgumentNullException("item");
        }

        Items[ItemCount] = item.Copy();
        ItemCount = ItemCount + 1;
        return Items[ItemCount - 1];
    }

    /// <summary>
    /// Returns the item at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting hierarchy arc item.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartHierarchyArcItem ItemAt(int index)
    {
        if (index < 0 || index >= ItemCount)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return Items[index];
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

    /// <summary>Returns the current items.</summary>
    /// <returns>The current field value.</returns>
    public ChartHierarchyArcItem[] GetItems()
    {
        return Items;
    }

    /// <summary>Replaces the current items.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetItems(ChartHierarchyArcItem[] value)
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
