namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartHexbinResult"/>.</summary>
public interface IChartHexbinResult
{
    /// <summary>
    /// Adds the supplied or increment to this object.
    /// </summary>
    /// <param name = "centerX">The center x.</param>
    /// <param name = "centerY">The center y.</param>
    /// <returns>The resulting hexbin cell.</returns>
    ChartHexbinCell AddOrIncrement(double centerX, double centerY);
    /// <summary>
    /// Returns the requested cell.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting hexbin cell.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    ChartHexbinCell CellAt(int index);
    /// <summary>Returns the current cells.</summary>
    /// <returns>The current field value.</returns>
    ChartHexbinCell[] GetCells();
    /// <summary>Replaces the current cells.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetCells(ChartHexbinCell[] value);
    /// <summary>Returns the current cell count.</summary>
    /// <returns>The current field value.</returns>
    int GetCellCount();
    /// <summary>Replaces the current cell count.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetCellCount(int value);
    /// <summary>Returns the current radius.</summary>
    /// <returns>The current field value.</returns>
    double GetRadius();
    /// <summary>Replaces the current radius.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetRadius(double value);
}
