namespace chart;
/// <summary>
/// Represents a histogram bin used by the chart model or rendering pipeline.
/// </summary>
public class ChartHistogramBin : IChartHistogramBin
{
    internal double Minimum;
    internal double Maximum;
    internal int Count;
    /// <summary>
    /// Initializes a histogram bin from the supplied minimum and maximum.
    /// </summary>
    /// <param name = "minimum">The inclusive lower end of the numeric domain.</param>
    /// <param name = "maximum">The inclusive upper end of the numeric domain.</param>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    public ChartHistogramBin(double minimum, double maximum)
    {
        if (double.IsNaN(minimum) || double.IsInfinity(minimum))
        {
            throw new System.ArgumentException("Bin minimum must be finite.");
        }

        if (double.IsNaN(maximum) || double.IsInfinity(maximum) || maximum <= minimum)
        {
            throw new System.ArgumentException("Bin maximum must be finite and greater than minimum.");
        }

        Minimum = minimum;
        Maximum = maximum;
        Count = 0;
    }

    /// <summary>
    /// Applies the width operation to the supplied data and returns the computed result.
    /// </summary>
    /// <returns>The resulting double.</returns>
    public double Width()
    {
        return Maximum - Minimum;
    }

    /// <summary>Returns the current minimum.</summary>
    /// <returns>The current field value.</returns>
    public double GetMinimum()
    {
        return Minimum;
    }

    /// <summary>Replaces the current minimum.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetMinimum(double value)
    {
        Minimum = value;
    }

    /// <summary>Returns the current maximum.</summary>
    /// <returns>The current field value.</returns>
    public double GetMaximum()
    {
        return Maximum;
    }

    /// <summary>Replaces the current maximum.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetMaximum(double value)
    {
        Maximum = value;
    }

    /// <summary>Returns the current count.</summary>
    /// <returns>The current field value.</returns>
    public int GetCount()
    {
        return Count;
    }

    /// <summary>Replaces the current count.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetCount(int value)
    {
        Count = value;
    }
}
