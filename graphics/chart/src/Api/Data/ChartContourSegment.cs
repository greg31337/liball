namespace chart;
/// <summary>
/// Represents a contour segment used by the chart model or rendering pipeline.
/// </summary>
public class ChartContourSegment
{
    /// <summary>
    /// The level used by this contour segment.
    /// </summary>
    public double Level;
    /// <summary>
    /// The start used by this contour segment.
    /// </summary>
    public ChartPoint Start;
    /// <summary>
    /// The end used by this contour segment.
    /// </summary>
    public ChartPoint End;
    /// <summary>
    /// Initializes a contour segment from the supplied level, start, and end.
    /// </summary>
    /// <param name = "level">The level.</param>
    /// <param name = "start">The start.</param>
    /// <param name = "end">The end.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartContourSegment(double level, ChartPoint start, ChartPoint end)
    {
        ChartLinearScale.ValidateFinite(level, "level");
        if (start == null)
        {
            throw new System.ArgumentNullException("start");
        }

        if (end == null)
        {
            throw new System.ArgumentNullException("end");
        }

        Level = level;
        Start = start.Copy();
        End = end.Copy();
    }
}
