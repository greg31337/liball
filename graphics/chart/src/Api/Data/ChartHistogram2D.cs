namespace chart;
/// <summary>
/// Represents a histogram2 d used by the chart model or rendering pipeline.
/// </summary>
public class ChartHistogram2D
{
    /// <summary>
    /// The counts used by this histogram2 d.
    /// </summary>
    public ChartDataMatrix Counts;
    /// <summary>
    /// The x minimum used by this histogram2 d.
    /// </summary>
    public double XMinimum;
    /// <summary>
    /// The x maximum used by this histogram2 d.
    /// </summary>
    public double XMaximum;
    /// <summary>
    /// The y minimum used by this histogram2 d.
    /// </summary>
    public double YMinimum;
    /// <summary>
    /// The y maximum used by this histogram2 d.
    /// </summary>
    public double YMaximum;
    /// <summary>
    /// The x bin width used by this histogram2 d.
    /// </summary>
    public double XBinWidth;
    /// <summary>
    /// The y bin width used by this histogram2 d.
    /// </summary>
    public double YBinWidth;
    /// <summary>
    /// Initializes a histogram2 d from the supplied counts, x minimum, x maximum, y minimum, and y maximum.
    /// </summary>
    /// <param name = "counts">The counts.</param>
    /// <param name = "xMinimum">The x minimum.</param>
    /// <param name = "xMaximum">The x maximum.</param>
    /// <param name = "yMinimum">The y minimum.</param>
    /// <param name = "yMaximum">The y maximum.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartHistogram2D(ChartDataMatrix counts, double xMinimum, double xMaximum, double yMinimum, double yMaximum)
    {
        if (counts == null)
        {
            throw new System.ArgumentNullException("counts");
        }

        ChartLinearScale.ValidateDomain(xMinimum, xMaximum);
        ChartLinearScale.ValidateDomain(yMinimum, yMaximum);
        Counts = counts.Copy();
        XMinimum = xMinimum;
        XMaximum = xMaximum;
        YMinimum = yMinimum;
        YMaximum = yMaximum;
        XBinWidth = (xMaximum - xMinimum) / counts.Columns;
        YBinWidth = (yMaximum - yMinimum) / counts.Rows;
    }
}
