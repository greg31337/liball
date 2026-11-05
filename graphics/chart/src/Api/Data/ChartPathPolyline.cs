namespace chart;
/// <summary>
/// Represents a path polyline used by the chart model or rendering pipeline.
/// </summary>
public class ChartPathPolyline
{
    /// <summary>
    /// The points used by this path polyline.
    /// </summary>
    public ChartPoint[] Points;
    /// <summary>
    /// Indicates whether closed.
    /// </summary>
    public bool Closed;
    /// <summary>
    /// Initializes a path polyline from the supplied points and closed.
    /// </summary>
    /// <param name = "points">The points.</param>
    /// <param name = "closed">The closed.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartPathPolyline(ChartPoint[] points, bool closed)
    {
        if (points == null)
        {
            throw new System.ArgumentNullException("points");
        }

        Points = CopyPoints(points);
        Closed = closed;
    }

    internal static ChartPoint[] CopyPoints(ChartPoint[] points)
    {
        ChartPoint[] copy = new ChartPoint[points.Length];
        int index = 0;
        while (index < points.Length)
        {
            if (points[index] == null)
            {
                throw new System.ArgumentException("Path points cannot contain null values.");
            }

            copy[index] = points[index].Copy();
            index = index + 1;
        }

        return copy;
    }
}
