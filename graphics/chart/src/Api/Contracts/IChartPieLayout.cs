namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartPieLayout"/>.</summary>
public interface IChartPieLayout
{
    /// <summary>
    /// Returns the requested slice.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting pie slice.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    ChartPieSlice SliceAt(int index);
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
    /// <summary>Returns the current slices.</summary>
    /// <returns>The current field value.</returns>
    ChartPieSlice[] GetSlices();
    /// <summary>Replaces the current slices.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetSlices(ChartPieSlice[] value);
    /// <summary>Returns the current slice count.</summary>
    /// <returns>The current field value.</returns>
    int GetSliceCount();
    /// <summary>Replaces the current slice count.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetSliceCount(int value);
}
