namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartParallelCategoriesLayout"/>.</summary>
public interface IChartParallelCategoriesLayout
{
    /// <summary>
    /// Adds the supplied band to this object.
    /// </summary>
    /// <param name = "band">The band.</param>
    /// <returns>The resulting parallel category band.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartParallelCategoryBand AddBand(ChartParallelCategoryBand band);
    /// <summary>
    /// Adds the supplied flow to this object.
    /// </summary>
    /// <param name = "flow">The flow.</param>
    /// <returns>The resulting parallel category flow.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartParallelCategoryFlow AddFlow(ChartParallelCategoryFlow flow);
    /// <summary>
    /// Returns the requested band.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting parallel category band.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    ChartParallelCategoryBand BandAt(int index);
    /// <summary>
    /// Returns the requested flow.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting parallel category flow.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    ChartParallelCategoryFlow FlowAt(int index);
    /// <summary>Returns the current bands.</summary>
    /// <returns>The current field value.</returns>
    ChartParallelCategoryBand[] GetBands();
    /// <summary>Replaces the current bands.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetBands(ChartParallelCategoryBand[] value);
    /// <summary>Returns the current flows.</summary>
    /// <returns>The current field value.</returns>
    ChartParallelCategoryFlow[] GetFlows();
    /// <summary>Replaces the current flows.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetFlows(ChartParallelCategoryFlow[] value);
    /// <summary>Returns the current band count.</summary>
    /// <returns>The current field value.</returns>
    int GetBandCount();
    /// <summary>Replaces the current band count.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetBandCount(int value);
    /// <summary>Returns the current flow count.</summary>
    /// <returns>The current field value.</returns>
    int GetFlowCount();
    /// <summary>Replaces the current flow count.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetFlowCount(int value);
    /// <summary>Returns the current axis count.</summary>
    /// <returns>The current field value.</returns>
    int GetAxisCount();
    /// <summary>Replaces the current axis count.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetAxisCount(int value);
    /// <summary>Returns the current row count.</summary>
    /// <returns>The current field value.</returns>
    int GetRowCount();
    /// <summary>Replaces the current row count.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetRowCount(int value);
}
