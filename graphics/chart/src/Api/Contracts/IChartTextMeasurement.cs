namespace chart;
/// <summary>Defines deterministic dimensions and line metrics returned by built-in text measurement.</summary>
public interface IChartTextMeasurement
{
    /// <summary>Returns the measured width in pixels.</summary>
    /// <returns>The measured width.</returns>
    int GetWidth();
    /// <summary>Returns the measured height in pixels.</summary>
    /// <returns>The measured height.</returns>
    int GetHeight();
    /// <summary>Returns the number of measured lines.</summary>
    /// <returns>The line count.</returns>
    int GetLineCount();
    /// <summary>Returns the glyph height for one line in pixels.</summary>
    /// <returns>The line height.</returns>
    int GetLineHeight();
    /// <summary>Returns the first-line baseline offset in pixels.</summary>
    /// <returns>The baseline offset.</returns>
    int GetBaseline();
}
