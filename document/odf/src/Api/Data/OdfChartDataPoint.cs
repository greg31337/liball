namespace odf;

/// <summary>Describes a compact run of chart data points.</summary>
public class OdfChartDataPoint
{
    /// <summary>The preserved positive repetition count.</summary>
    public string Repeated = "";
    /// <summary>The parsed repetition count, or one when absent.</summary>
    public long RepeatCount = 1;
    /// <summary>Whether the repetition count is absent or valid.</summary>
    public bool IsRepeatCountValid = true;
    /// <summary>The referenced chart style name.</summary>
    public string StyleName = "";
    /// <summary>The effective chart-style metadata.</summary>
    public OdfChartStyleMetadata StyleMetadata = new OdfChartStyleMetadata();
    /// <summary>The point data label, when present.</summary>
    public OdfChartDataLabel? Label;
    /// <summary>The underlying data-point element.</summary>
    public OdfNode? Node;
    internal string OriginalRepeated = "";
    internal string OriginalStyleName = "";
}
