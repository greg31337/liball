namespace chart;
/// <summary>
/// Represents a palette used by the chart model or rendering pipeline.
/// </summary>
public class ChartPalette : IChartPalette
{
    internal string Name;
    internal ChartColor[] Colors;
    /// <summary>
    /// Initializes a palette from the supplied name and colors.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    /// <param name = "colors">The colors.</param>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartPalette(string name, ChartColor[] colors)
    {
        if (name == null)
        {
            throw new System.ArgumentNullException("name");
        }

        if (colors == null)
        {
            throw new System.ArgumentNullException("colors");
        }

        if (colors.Length == 0)
        {
            throw new System.ArgumentException("A palette requires at least one color.");
        }

        Name = name;
        Colors = CopyColorArray(colors);
    }

    /// <summary>
    /// Returns the number of logical items in this value.
    /// </summary>
    /// <returns>The resulting int.</returns>
    public int Count()
    {
        return Colors.Length;
    }

    /// <summary>
    /// Returns the requested color.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting color.</returns>
    public ChartColor GetColor(int index)
    {
        int colorIndex = index % Colors.Length;
        if (colorIndex < 0)
        {
            colorIndex = colorIndex + Colors.Length;
        }

        return Colors[colorIndex].Copy();
    }

    /// <summary>
    /// Creates an independent copy of the colors.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartColor[] CopyColors()
    {
        return CopyColorArray(Colors);
    }

    internal static ChartColor[] CopyColorArray(ChartColor[] colors)
    {
        ChartColor[] copy = new ChartColor[colors.Length];
        int index = 0;
        while (index < colors.Length)
        {
            if (colors[index] == null)
            {
                throw new System.ArgumentException("Palette colors cannot contain null values.");
            }

            copy[index] = colors[index].Copy();
            index = index + 1;
        }

        return copy;
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

    /// <summary>Returns the current colors.</summary>
    /// <returns>The current field value.</returns>
    public ChartColor[] GetColors()
    {
        return Colors;
    }

    /// <summary>Replaces the current colors.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetColors(ChartColor[] value)
    {
        Colors = value;
    }
}
