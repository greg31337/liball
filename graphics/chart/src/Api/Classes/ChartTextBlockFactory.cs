namespace chart;
/// <summary>Creates and converts chart text block values.</summary>
public class ChartTextBlockFactory : IChartTextBlockFactory
{
    /// <summary>Initializes a stateless chart text block factory facade.</summary>
    public ChartTextBlockFactory()
    {
    }

    /// <summary>
    /// Creates a text block from the supplied text.
    /// </summary>
    /// <param name = "text">The text to measure, render, or encode.</param>
    /// <returns>A text block initialized with the supplied text and library defaults.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartTextBlock Create(string text)
    {
        return ChartTextBlock.Create(text);
    }
}
