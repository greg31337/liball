namespace chart;
/// <summary>
/// Represents a hierarchy rect layout used by the chart model or rendering pipeline.
/// </summary>
public class ChartHierarchyRectLayout : IChartHierarchyRectLayout
{
    internal ChartHierarchyRectItem[] Items;
    internal int ItemCount;
    /// <summary>
    /// Initializes a hierarchy rect layout from the supplied capacity.
    /// </summary>
    /// <param name = "capacity">The capacity.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartHierarchyRectLayout(int capacity)
    {
        if (capacity <= 0)
        {
            throw new System.ArgumentOutOfRangeException("capacity");
        }

        Items = new ChartHierarchyRectItem[capacity];
        ItemCount = 0;
    }

    /// <summary>
    /// Adds the supplied item to this object.
    /// </summary>
    /// <param name = "item">The item.</param>
    /// <returns>The resulting hierarchy rect item.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartHierarchyRectItem AddItem(ChartHierarchyRectItem item)
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
    /// <returns>The resulting hierarchy rect item.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartHierarchyRectItem ItemAt(int index)
    {
        if (index < 0 || index >= ItemCount)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return Items[index];
    }

    /// <summary>Returns the current items.</summary>
    /// <returns>The current field value.</returns>
    public ChartHierarchyRectItem[] GetItems()
    {
        return Items;
    }

    /// <summary>Replaces the current items.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetItems(ChartHierarchyRectItem[] value)
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
