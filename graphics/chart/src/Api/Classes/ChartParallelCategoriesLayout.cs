namespace chart;
/// <summary>
/// Represents a parallel categories layout used by the chart model or rendering pipeline.
/// </summary>
public class ChartParallelCategoriesLayout : IChartParallelCategoriesLayout
{
    internal ChartParallelCategoryBand[] Bands;
    internal ChartParallelCategoryFlow[] Flows;
    internal int BandCount;
    internal int FlowCount;
    internal int AxisCount;
    internal int RowCount;
    /// <summary>
    /// Initializes a parallel categories layout from the supplied band capacity, flow capacity, axis count, and row count.
    /// </summary>
    /// <param name = "bandCapacity">The band capacity.</param>
    /// <param name = "flowCapacity">The flow capacity.</param>
    /// <param name = "axisCount">The requested number of axis entries.</param>
    /// <param name = "rowCount">The number of logical rows represented by the input.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartParallelCategoriesLayout(int bandCapacity, int flowCapacity, int axisCount, int rowCount)
    {
        if (bandCapacity <= 0)
        {
            throw new System.ArgumentOutOfRangeException("bandCapacity");
        }

        if (flowCapacity < 0)
        {
            throw new System.ArgumentOutOfRangeException("flowCapacity");
        }

        Bands = new ChartParallelCategoryBand[bandCapacity];
        Flows = new ChartParallelCategoryFlow[flowCapacity];
        BandCount = 0;
        FlowCount = 0;
        AxisCount = axisCount;
        RowCount = rowCount;
    }

    /// <summary>
    /// Adds the supplied band to this object.
    /// </summary>
    /// <param name = "band">The band.</param>
    /// <returns>The resulting parallel category band.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartParallelCategoryBand AddBand(ChartParallelCategoryBand band)
    {
        if (band == null)
        {
            throw new System.ArgumentNullException("band");
        }

        Bands[BandCount] = band.Copy();
        BandCount = BandCount + 1;
        return Bands[BandCount - 1];
    }

    /// <summary>
    /// Adds the supplied flow to this object.
    /// </summary>
    /// <param name = "flow">The flow.</param>
    /// <returns>The resulting parallel category flow.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartParallelCategoryFlow AddFlow(ChartParallelCategoryFlow flow)
    {
        if (flow == null)
        {
            throw new System.ArgumentNullException("flow");
        }

        Flows[FlowCount] = flow.Copy();
        FlowCount = FlowCount + 1;
        return Flows[FlowCount - 1];
    }

    /// <summary>
    /// Returns the requested band.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting parallel category band.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartParallelCategoryBand BandAt(int index)
    {
        if (index < 0 || index >= BandCount)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return Bands[index];
    }

    /// <summary>
    /// Returns the requested flow.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting parallel category flow.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartParallelCategoryFlow FlowAt(int index)
    {
        if (index < 0 || index >= FlowCount)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return Flows[index];
    }

    /// <summary>Returns the current bands.</summary>
    /// <returns>The current field value.</returns>
    public ChartParallelCategoryBand[] GetBands()
    {
        return Bands;
    }

    /// <summary>Replaces the current bands.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetBands(ChartParallelCategoryBand[] value)
    {
        Bands = value;
    }

    /// <summary>Returns the current flows.</summary>
    /// <returns>The current field value.</returns>
    public ChartParallelCategoryFlow[] GetFlows()
    {
        return Flows;
    }

    /// <summary>Replaces the current flows.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetFlows(ChartParallelCategoryFlow[] value)
    {
        Flows = value;
    }

    /// <summary>Returns the current band count.</summary>
    /// <returns>The current field value.</returns>
    public int GetBandCount()
    {
        return BandCount;
    }

    /// <summary>Replaces the current band count.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetBandCount(int value)
    {
        BandCount = value;
    }

    /// <summary>Returns the current flow count.</summary>
    /// <returns>The current field value.</returns>
    public int GetFlowCount()
    {
        return FlowCount;
    }

    /// <summary>Replaces the current flow count.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetFlowCount(int value)
    {
        FlowCount = value;
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

    /// <summary>Returns the current row count.</summary>
    /// <returns>The current field value.</returns>
    public int GetRowCount()
    {
        return RowCount;
    }

    /// <summary>Replaces the current row count.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetRowCount(int value)
    {
        RowCount = value;
    }
}
