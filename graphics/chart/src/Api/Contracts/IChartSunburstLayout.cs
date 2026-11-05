namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartSunburstLayout"/>.</summary>
public interface IChartSunburstLayout
{
    /// <summary>
    /// Adds the supplied item to this object.
    /// </summary>
    /// <param name = "item">The item.</param>
    /// <returns>The resulting hierarchy arc item.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartHierarchyArcItem AddItem(ChartHierarchyArcItem item);
    /// <summary>
    /// Returns the item at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting hierarchy arc item.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    ChartHierarchyArcItem ItemAt(int index);
    /// <summary>Returns the current center x.</summary>
    /// <returns>The current field value.</returns>
    double GetCenterX();
    /// <summary>Replaces the current center x.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetCenterX(double value);
    /// <summary>Returns the current center y.</summary>
    /// <returns>The current field value.</returns>
    double GetCenterY();
    /// <summary>Replaces the current center y.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetCenterY(double value);
    /// <summary>Returns the current items.</summary>
    /// <returns>The current field value.</returns>
    ChartHierarchyArcItem[] GetItems();
    /// <summary>Replaces the current items.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetItems(ChartHierarchyArcItem[] value);
    /// <summary>Returns the current item count.</summary>
    /// <returns>The current field value.</returns>
    int GetItemCount();
    /// <summary>Replaces the current item count.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetItemCount(int value);
}
