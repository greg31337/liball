namespace chart;
/// <summary>Represents deterministic dimensions and line metrics returned by built-in text measurement.</summary>
public class ChartTextMeasurement : IChartTextMeasurement
{
    internal int Width;
    internal int Height;
    internal int LineCount;
    internal int LineHeight;
    internal int Baseline;

    /// <summary>Initializes a text measurement from pixel dimensions and line metrics.</summary>
    /// <param name = "width">The non-negative measured width.</param>
    /// <param name = "height">The non-negative measured height.</param>
    /// <param name = "lineCount">The positive number of lines.</param>
    /// <param name = "lineHeight">The positive glyph height for one line.</param>
    /// <param name = "baseline">The baseline offset from the top of the first line.</param>
    public ChartTextMeasurement(int width, int height, int lineCount, int lineHeight, int baseline)
    {
        if (width < 0 || height < 0 || lineCount <= 0 || lineHeight <= 0 || baseline < 0 || baseline > lineHeight)
        {
            throw new System.ArgumentOutOfRangeException("width");
        }

        Width = width;
        Height = height;
        LineCount = lineCount;
        LineHeight = lineHeight;
        Baseline = baseline;
    }

    /// <summary>Returns the measured width in pixels.</summary>
    /// <returns>The measured width.</returns>
    public int GetWidth()
    {
        return Width;
    }

    /// <summary>Returns the measured height in pixels.</summary>
    /// <returns>The measured height.</returns>
    public int GetHeight()
    {
        return Height;
    }

    /// <summary>Returns the number of measured lines.</summary>
    /// <returns>The line count.</returns>
    public int GetLineCount()
    {
        return LineCount;
    }

    /// <summary>Returns the glyph height for one line in pixels.</summary>
    /// <returns>The line height.</returns>
    public int GetLineHeight()
    {
        return LineHeight;
    }

    /// <summary>Returns the first-line baseline offset in pixels.</summary>
    /// <returns>The baseline offset.</returns>
    public int GetBaseline()
    {
        return Baseline;
    }
}
