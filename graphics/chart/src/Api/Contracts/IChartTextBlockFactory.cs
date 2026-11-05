namespace chart;
/// <summary>Defines substitutable chart text block factory operations.</summary>
public interface IChartTextBlockFactory
{
    /// <summary>
    /// Creates a text block from the supplied text.
    /// </summary>
    /// <param name = "text">The text to measure, render, or encode.</param>
    /// <returns>A text block initialized with the supplied text and library defaults.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartTextBlock Create(string text);
}
