namespace chart;
/// <summary>Defines a categorical marker-shape scale.</summary>
public interface IChartShapeScale
{
    /// <summary>Maps a category to its configured marker shape.</summary>
    /// <param name = "category">The category to map.</param>
    /// <returns>The configured marker shape.</returns>
    ChartMarkerShape Map(string category);
}
