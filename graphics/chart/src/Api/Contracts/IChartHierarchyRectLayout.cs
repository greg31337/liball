namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartHierarchyRectLayout"/>.</summary>
public interface IChartHierarchyRectLayout
{
    /// <summary>
    /// Adds the supplied item to this object.
    /// </summary>
    /// <param name = "item">The item.</param>
    /// <returns>The resulting hierarchy rect item.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartHierarchyRectItem AddItem(ChartHierarchyRectItem item);
    /// <summary>
    /// Returns the item at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting hierarchy rect item.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    ChartHierarchyRectItem ItemAt(int index);
    /// <summary>Returns the current items.</summary>
    /// <returns>The current field value.</returns>
    ChartHierarchyRectItem[] GetItems();
    /// <summary>Replaces the current items.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetItems(ChartHierarchyRectItem[] value);
    /// <summary>Returns the current item count.</summary>
    /// <returns>The current field value.</returns>
    int GetItemCount();
    /// <summary>Replaces the current item count.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetItemCount(int value);
}
