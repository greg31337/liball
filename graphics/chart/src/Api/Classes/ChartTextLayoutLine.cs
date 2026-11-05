namespace chart;
/// <summary>Represents one positioned line in a deterministic text layout.</summary>
public class ChartTextLayoutLine : IChartTextLayoutLine
{
    internal string Text;
    internal int X;
    internal int Y;
    internal int Width;
    internal int BaselineY;

    /// <summary>Initializes a positioned text line.</summary>
    /// <param name = "text">The prepared line text.</param>
    /// <param name = "x">The horizontal line origin.</param>
    /// <param name = "y">The vertical line origin.</param>
    /// <param name = "width">The nonnegative measured width.</param>
    /// <param name = "baselineY">The baseline coordinate.</param>
    public ChartTextLayoutLine(string text, int x, int y, int width, int baselineY)
    {
        ChartTextBlock.ValidateText(text, "text");
        if (width < 0)
        {
            throw new System.ArgumentOutOfRangeException("width");
        }

        Text = text;
        X = x;
        Y = y;
        Width = width;
        BaselineY = baselineY;
    }

    /// <summary>Returns the prepared line text.</summary>
    /// <returns>The line text.</returns>
    public string GetText()
    {
        return Text;
    }

    /// <summary>Returns the horizontal line origin.</summary>
    /// <returns>The horizontal origin.</returns>
    public int GetX()
    {
        return X;
    }

    /// <summary>Returns the vertical line origin.</summary>
    /// <returns>The vertical origin.</returns>
    public int GetY()
    {
        return Y;
    }

    /// <summary>Returns the measured line width.</summary>
    /// <returns>The line width.</returns>
    public int GetWidth()
    {
        return Width;
    }

    /// <summary>Returns the baseline coordinate.</summary>
    /// <returns>The baseline coordinate.</returns>
    public int GetBaselineY()
    {
        return BaselineY;
    }

    /// <summary>Creates an independent copy.</summary>
    /// <returns>A copied layout line.</returns>
    public ChartTextLayoutLine Copy()
    {
        return new ChartTextLayoutLine(Text, X, Y, Width, BaselineY);
    }
}
