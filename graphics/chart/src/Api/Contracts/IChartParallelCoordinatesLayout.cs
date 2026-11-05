namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartParallelCoordinatesLayout"/>.</summary>
public interface IChartParallelCoordinatesLayout
{
    /// <summary>
    /// Adds the supplied axis to this object.
    /// </summary>
    /// <param name = "axis">The axis.</param>
    /// <returns>The resulting parallel axis layout.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartParallelAxisLayout AddAxis(ChartParallelAxisLayout axis);
    /// <summary>
    /// Adds the supplied record to this object.
    /// </summary>
    /// <param name = "record">The record.</param>
    /// <returns>The resulting parallel record layout.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartParallelRecordLayout AddRecord(ChartParallelRecordLayout record);
    /// <summary>
    /// Returns the requested axis.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting parallel axis layout.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    ChartParallelAxisLayout AxisAt(int index);
    /// <summary>
    /// Returns the requested record.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting parallel record layout.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    ChartParallelRecordLayout RecordAt(int index);
    /// <summary>Returns the current axes.</summary>
    /// <returns>The current field value.</returns>
    ChartParallelAxisLayout[] GetAxes();
    /// <summary>Replaces the current axes.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetAxes(ChartParallelAxisLayout[] value);
    /// <summary>Returns the current records.</summary>
    /// <returns>The current field value.</returns>
    ChartParallelRecordLayout[] GetRecords();
    /// <summary>Replaces the current records.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetRecords(ChartParallelRecordLayout[] value);
    /// <summary>Returns the current axis count.</summary>
    /// <returns>The current field value.</returns>
    int GetAxisCount();
    /// <summary>Replaces the current axis count.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetAxisCount(int value);
    /// <summary>Returns the current record count.</summary>
    /// <returns>The current field value.</returns>
    int GetRecordCount();
    /// <summary>Replaces the current record count.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetRecordCount(int value);
}
