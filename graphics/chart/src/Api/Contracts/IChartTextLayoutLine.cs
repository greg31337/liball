namespace chart;
/// <summary>Defines one positioned line in a deterministic text layout.</summary>
public interface IChartTextLayoutLine
{
    /// <summary>Returns the prepared line text.</summary>
    /// <returns>The line text.</returns>
    string GetText();
    /// <summary>Returns the horizontal line origin.</summary>
    /// <returns>The horizontal origin.</returns>
    int GetX();
    /// <summary>Returns the vertical line origin.</summary>
    /// <returns>The vertical origin.</returns>
    int GetY();
    /// <summary>Returns the measured line width.</summary>
    /// <returns>The line width.</returns>
    int GetWidth();
    /// <summary>Returns the baseline coordinate.</summary>
    /// <returns>The baseline coordinate.</returns>
    int GetBaselineY();
    /// <summary>Creates an independent copy.</summary>
    /// <returns>A copied layout line.</returns>
    ChartTextLayoutLine Copy();
}
