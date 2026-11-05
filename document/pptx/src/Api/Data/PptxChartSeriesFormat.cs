namespace pptx;

/// <summary>
/// Represents editable fill, line, marker, smoothing, and label formatting for one chart series.
/// </summary>
public class PptxChartSeriesFormat
{
    /// <summary>
    /// Indicates whether the fill value should be applied.
    /// </summary>
    public bool HasFill;
    /// <summary>
    /// Stores the series fill.
    /// </summary>
    public PptxFill Fill;
    /// <summary>
    /// Indicates whether the line value should be applied.
    /// </summary>
    public bool HasLine;
    /// <summary>
    /// Stores the series line.
    /// </summary>
    public PptxLine Line;
    /// <summary>
    /// Stores the marker symbol, or an empty string when no marker is requested.
    /// </summary>
    public string MarkerSymbol;
    /// <summary>
    /// Stores the marker size in points.
    /// </summary>
    public int MarkerSize;
    /// <summary>
    /// Indicates whether line smoothing is enabled.
    /// </summary>
    public bool Smooth;
    /// <summary>
    /// Stores the DrawingML data-label position.
    /// </summary>
    public string LabelPosition;
    /// <summary>
    /// Stores the data-label number format.
    /// </summary>
    public string NumberFormat;

    /// <summary>
    /// Initializes empty series formatting.
    /// </summary>
    public PptxChartSeriesFormat()
    {
        HasFill = false;
        Fill = new PptxFill();
        HasLine = false;
        Line = new PptxLine();
        MarkerSymbol = "";
        MarkerSize = 0;
        Smooth = false;
        LabelPosition = "";
        NumberFormat = "";
    }
}
