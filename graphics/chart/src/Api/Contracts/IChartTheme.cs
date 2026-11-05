namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartTheme"/>.</summary>
public interface IChartTheme
{
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartTheme Copy();
    /// <summary>
    /// Applies the series color operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "seriesIndex">The zero-based series index.</param>
    /// <returns>The resulting color.</returns>
    ChartColor SeriesColor(int seriesIndex);
    /// <summary>
    /// Sets the palette configuration.
    /// </summary>
    /// <param name = "palette">The categorical colors to apply in stable index order.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    void SetPalette(ChartPalette palette);
    /// <summary>
    /// Selects colorblind palette using the supplied data and configuration.
    /// </summary>
    void UseColorblindPalette();
    /// <summary>Returns the current name.</summary>
    /// <returns>The current field value.</returns>
    string GetName();
    /// <summary>Replaces the current name.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetName(string value);
    /// <summary>Returns the current style.</summary>
    /// <returns>The current field value.</returns>
    ChartStyle GetStyle();
    /// <summary>Replaces the current style.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetStyle(ChartStyle value);
    /// <summary>Returns the current palette.</summary>
    /// <returns>The current field value.</returns>
    ChartPalette GetPalette();
}
