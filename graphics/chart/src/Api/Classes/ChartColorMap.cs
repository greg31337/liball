namespace chart;
/// <summary>
/// Represents a color map used by the chart model or rendering pipeline.
/// </summary>
public class ChartColorMap : IChartColorMap
{
    internal string Name;
    internal ChartColor[] Stops;
    /// <summary>
    /// Initializes a color map from the supplied name and stops.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    /// <param name = "stops">The stops.</param>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartColorMap(string name, ChartColor[] stops)
    {
        if (name == null)
        {
            throw new System.ArgumentNullException("name");
        }

        if (stops == null)
        {
            throw new System.ArgumentNullException("stops");
        }

        if (stops.Length == 0)
        {
            throw new System.ArgumentException("A color map requires at least one stop.");
        }

        Name = name;
        Stops = ChartPalette.CopyColorArray(stops);
    }

    /// <summary>
    /// Returns the number of logical items in this value.
    /// </summary>
    /// <returns>The resulting int.</returns>
    public int Count()
    {
        return Stops.Length;
    }

    /// <summary>
    /// Returns the requested stop.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting color.</returns>
    public ChartColor GetStop(int index)
    {
        return Stops[index].Copy();
    }

    /// <summary>
    /// Returns the requested color.
    /// </summary>
    /// <param name = "amount">The amount.</param>
    /// <returns>The resulting color.</returns>
    public ChartColor GetColor(double amount)
    {
        double clamped = ChartColor.ClampDouble(amount, 0.0, 1.0);
        if (Stops.Length == 1)
        {
            return Stops[0].Copy();
        }

        if (clamped == 0.0)
        {
            return Stops[0].Copy();
        }

        if (clamped == 1.0)
        {
            return Stops[Stops.Length - 1].Copy();
        }

        double scaled = clamped * (Stops.Length - 1);
        int index = (int)System.Math.Floor(scaled);
        double localAmount = scaled - index;
        return ChartColor.Lerp(Stops[index], Stops[index + 1], localAmount);
    }

    /// <summary>
    /// Creates a reversed copy of the the configured value.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    /// <returns>The resulting color map.</returns>
    public ChartColorMap Reverse(string name)
    {
        ChartColor[] reversed = new ChartColor[Stops.Length];
        int index = 0;
        while (index < Stops.Length)
        {
            reversed[index] = Stops[Stops.Length - index - 1].Copy();
            index = index + 1;
        }

        return new ChartColorMap(name, reversed);
    }

    /// <summary>Returns the current name.</summary>
    /// <returns>The current field value.</returns>
    public string GetName()
    {
        return Name;
    }

    /// <summary>Replaces the current name.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetName(string value)
    {
        Name = value;
    }

    /// <summary>Returns the current stops.</summary>
    /// <returns>The current field value.</returns>
    public ChartColor[] GetStops()
    {
        return Stops;
    }

    /// <summary>Replaces the current stops.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetStops(ChartColor[] value)
    {
        Stops = value;
    }
}
