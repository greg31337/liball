namespace chart;
/// <summary>
/// Represents a parallel coordinates layout used by the chart model or rendering pipeline.
/// </summary>
public class ChartParallelCoordinatesLayout : IChartParallelCoordinatesLayout
{
    internal ChartParallelAxisLayout[] Axes;
    internal ChartParallelRecordLayout[] Records;
    internal int AxisCount;
    internal int RecordCount;
    /// <summary>
    /// Initializes a parallel coordinates layout from the supplied axis capacity and record capacity.
    /// </summary>
    /// <param name = "axisCapacity">The axis capacity.</param>
    /// <param name = "recordCapacity">The record capacity.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartParallelCoordinatesLayout(int axisCapacity, int recordCapacity)
    {
        if (axisCapacity <= 0)
        {
            throw new System.ArgumentOutOfRangeException("axisCapacity");
        }

        if (recordCapacity <= 0)
        {
            throw new System.ArgumentOutOfRangeException("recordCapacity");
        }

        Axes = new ChartParallelAxisLayout[axisCapacity];
        Records = new ChartParallelRecordLayout[recordCapacity];
        AxisCount = 0;
        RecordCount = 0;
    }

    /// <summary>
    /// Adds the supplied axis to this object.
    /// </summary>
    /// <param name = "axis">The axis.</param>
    /// <returns>The resulting parallel axis layout.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartParallelAxisLayout AddAxis(ChartParallelAxisLayout axis)
    {
        if (axis == null)
        {
            throw new System.ArgumentNullException("axis");
        }

        Axes[AxisCount] = axis.Copy();
        AxisCount = AxisCount + 1;
        return Axes[AxisCount - 1];
    }

    /// <summary>
    /// Adds the supplied record to this object.
    /// </summary>
    /// <param name = "record">The record.</param>
    /// <returns>The resulting parallel record layout.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartParallelRecordLayout AddRecord(ChartParallelRecordLayout record)
    {
        if (record == null)
        {
            throw new System.ArgumentNullException("record");
        }

        Records[RecordCount] = record.Copy();
        RecordCount = RecordCount + 1;
        return Records[RecordCount - 1];
    }

    /// <summary>
    /// Returns the requested axis.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting parallel axis layout.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartParallelAxisLayout AxisAt(int index)
    {
        if (index < 0 || index >= AxisCount)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return Axes[index];
    }

    /// <summary>
    /// Returns the requested record.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting parallel record layout.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartParallelRecordLayout RecordAt(int index)
    {
        if (index < 0 || index >= RecordCount)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return Records[index];
    }

    /// <summary>Returns the current axes.</summary>
    /// <returns>The current field value.</returns>
    public ChartParallelAxisLayout[] GetAxes()
    {
        return Axes;
    }

    /// <summary>Replaces the current axes.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetAxes(ChartParallelAxisLayout[] value)
    {
        Axes = value;
    }

    /// <summary>Returns the current records.</summary>
    /// <returns>The current field value.</returns>
    public ChartParallelRecordLayout[] GetRecords()
    {
        return Records;
    }

    /// <summary>Replaces the current records.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetRecords(ChartParallelRecordLayout[] value)
    {
        Records = value;
    }

    /// <summary>Returns the current axis count.</summary>
    /// <returns>The current field value.</returns>
    public int GetAxisCount()
    {
        return AxisCount;
    }

    /// <summary>Replaces the current axis count.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetAxisCount(int value)
    {
        AxisCount = value;
    }

    /// <summary>Returns the current record count.</summary>
    /// <returns>The current field value.</returns>
    public int GetRecordCount()
    {
        return RecordCount;
    }

    /// <summary>Replaces the current record count.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetRecordCount(int value)
    {
        RecordCount = value;
    }
}
